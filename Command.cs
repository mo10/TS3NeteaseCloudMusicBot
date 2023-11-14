using TS3AudioBot.Audio;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.ResourceFactories;
using System.Threading.Tasks;
using TS3AudioBot.CommandSystem;
using System.Threading;
using System.Linq;
using TSLib.Scheduler;
using System.Collections.Generic;
using System;
using TS3AudioBot.Helper;
using System.IO;
using System.Reflection;

namespace ts3ncm
{
    public class NCMCommand : IBotPlugin
    {
        private PlayManager playManager;
        private Ts3Client ts3Client;
        private ResourceResolver resourceResolver;
        private DedicatedTaskScheduler taskScheduler;

        private TickWorker worker;
        private InvokerData invokerData;
        private Queue<AudioResource> songQueue;

        public string Cookie;

        /// <summary>
        /// 登入操作正在进行中?
        /// </summary>
        private bool isLogging = false;
        /// <summary>
        /// 1 未登入, 2 登入有效
        /// </summary>
        public int loginStatus = 0;

        public NCMCommand(PlayManager playManager, Ts3Client ts3Client, ResourceResolver resourceResolver, DedicatedTaskScheduler taskScheduler)
        {
            this.playManager = playManager;
            this.ts3Client = ts3Client;
            this.resourceResolver = resourceResolver;
            this.taskScheduler = taskScheduler;

            songQueue = new Queue<AudioResource>();
        }
        public async void Dispose()
        {
            worker.Disable();
            playManager.AfterResourceStarted -= PlayManager_AfterResourceStarted;
            playManager.PlaybackStopped -= PlayManager_PlaybackStopped;
            await this.ts3Client.SendChannelMessage($"{Env.Name} 卸载");

        }

        public async Task CheckLoginStatus()
        {
            int login_status = 1;
            if (!string.IsNullOrEmpty(this.Cookie))
            {
                var status = await APIHelper.GetLoginStatusAasync(Env.Backend, Cookie);
                if (status.data.code == 200 && status.data.account.status == 0)
                    login_status = 2;
            }

            if (this.loginStatus != login_status)
            {
                this.loginStatus = login_status;

                if (login_status == 1)
                    await this.ts3Client.ChangeName($"{Env.Name} - 未登入");
                if (login_status == 2)
                    await this.ts3Client.ChangeName($"{Env.Name}");
            }
        }

        public async void Initialize()
        {
            playManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
            playManager.PlaybackStopped += PlayManager_PlaybackStopped;

            // 30秒检查一次登入状态
            worker = taskScheduler.CreateTimer(async() => await CheckLoginStatus(), new TimeSpan(0, 0, 30), true);
             
            await CheckLoginStatus();
            await this.ts3Client.SendChannelMessage($"{Env.Name} 就绪");
        }

        private Task PlayManager_AfterResourceStarted(object sender, PlayInfoEventArgs value)
        {
            this.invokerData = value.Invoker;
            return Task.CompletedTask;
        }

        private async Task PlayManager_PlaybackStopped(object sender, EventArgs e)
        {
            PlayManager playManager = (PlayManager)sender;
            await TryPlayNextAsync(playManager, ts3Client, invokerData);
        }

        private bool IsSongInQueue(string id)
        {
            return songQueue.Any(ar => ar.ResourceId.Equals(id));
        }
        public async Task<string> TryPlayNextAsync(PlayManager playManager, Ts3Client ts3Client, InvokerData invoker)
        {
            AudioResource ar;

            if (!songQueue.TryDequeue(out ar))
            {
                await ts3Client.SendChannelMessage($"⏹ 播放结束");
                return null;
            }

            var url = await APIHelper.GetAudioUrlAsync(Env.Backend, Cookie, ar.ResourceId);
            try
            {
                await playManager.Play(invoker, new PlayResource(url, ar));
                await ts3Client.SendChannelMessage($"► 正在播放: [URL={ar.Get("songUrl")}]{ar.ResourceTitle}[/URL] - {ar.Get("artist")}");
                await MainCommands.CommandBotAvatarSet(ts3Client, ar.Get("coverUrl"));
            }
            catch (Exception ex)
            {
                throw Error.Str($"‼️ 播放失败: {ex}"); ;
            }

            return null;
        }

        [Command("login")]
        public async Task CommandNcmLoginAsync(PlayManager playManager, Ts3Client ts3Client)
        {
            if (isLogging)
            {
                await ts3Client.SendChannelMessage("请使用APP扫描机器人头像二维码登入");
                return;
            }
            isLogging = true;

            // 1. 生成QR Key
            var key = await APIHelper.GetLoginQRKeyAsync(Env.Backend);
            if (string.IsNullOrEmpty(key))
            {
                await ts3Client.SendChannelMessage("生成二维码失败，请重试");
                isLogging = false;
                return;
            }
            // 2. 生成二维码
            using (var img = await APIHelper.GetLoginQRImageAsync(Env.Backend, key))
            {
                if (img == null)
                {
                    await ts3Client.SendChannelMessage("二维码加载失败，请重试");
                    isLogging = false;
                    return;
                }
                await this.ts3Client.UploadAvatar(img);
            }
            // 3. 等待登入
            await ts3Client.SendChannelMessage("请使用APP扫描机器人头像二维码登入");
            bool waiting = true;
            int lastCode = 0;
            while (waiting)
            {
                await Task.Delay(3000);
                var resp = await APIHelper.GetLoginQRStatusAsync(Env.Backend, key);
                switch (resp.code)
                {
                    case 800:
                        if (lastCode != resp.code)
                        {
                            await ts3Client.SendChannelMessage("二维码已过期，请重新登入");
                        }
                        lastCode = resp.code;
                        waiting = false;
                        break;
                    case 801:
                        if (lastCode != resp.code)
                        {
                            await ts3Client.ChangeDescription("等待扫码");
                        }
                        lastCode = resp.code;
                        break;
                    case 802:
                        if (lastCode != resp.code)
                        {
                            await ts3Client.ChangeDescription($"{resp.nickname} 等待客户端确认");
                            await MainCommands.CommandBotAvatarSet(ts3Client, resp.avatarUrl);
                        }
                        lastCode = resp.code;
                        break;
                    case 803:
                        if (lastCode != resp.code)
                        {
                            this.Cookie = processCookie(resp.cookie);
                            await ts3Client.SendChannelMessage($"{resp.nickname} 登入成功");
                            await CheckLoginStatus();
                        }
                        lastCode = resp.code;
                        waiting = false;
                        break;
                    default:
                        await ts3Client.SendChannelMessage($"未知状态码 {resp.code}:{resp.message}");
                        waiting = false;
                        break;
                }
            }

            if (playManager.IsPlaying)
            {
                // 恢复正在播放的描述和封面图
                await this.ts3Client.ChangeDescription(playManager.CurrentPlayData.PlayResource.AudioResource.ResourceTitle);
                await MainCommands.CommandBotAvatarSet(ts3Client, playManager.CurrentPlayData.PlayResource.AudioResource.Get("coverUrl"));
            }
            else
            {
                await this.ts3Client.DeleteAvatar();
                await this.ts3Client.ChangeDescription("等待点歌...");
            }

            isLogging = false;
        }

        [Command("logout")]
        public async Task CommandNcmLogoutAsync(PlayManager playManager, Ts3Client ts3Client)
        {
            if (isLogging)
            {
                await ts3Client.SendChannelMessage("请使用APP扫描机器人头像二维码登入");
                return;
            }
            this.Cookie = "";
            await ts3Client.SendChannelMessage("已登出");
        }

        [Command("ncm status")]
        public async Task<string> CommandNcmStatusAsync()
        {
            string result = $"\n后端服务器: {Env.Backend}\n当前用户: ";

            if (string.IsNullOrEmpty(this.Cookie))
            {
                result += $"未登入";
                return result;
            }

            var status = await APIHelper.GetLoginStatusAasync(Env.Backend, Cookie);
            if (status.data.code == 200 && status.data.account.status == 0)
            {
                result += $"[URL=https://music.163.com/#/user/home?id={status.data.profile.userId}]{status.data.profile.nickname}[/URL]\n";
                result += $"饼干: {this.Cookie}";
            }
            else
            {
                result += $"未登入";
            }

            return result;
        }

        
        [Command("ncm setcookie")]
        public async Task<string> CommandNcmSetCookieAsync(string cookie)
        {
            var status = await APIHelper.GetLoginStatusAasync(Env.Backend, cookie);
            if (status.data.code == 200 && status.data.account.status == 0)
            {
                this.Cookie = cookie;
                await CheckLoginStatus();
                return $"登入成功";
            }
            return $"饼干无效";
        }
        

        [Command("p")]
        public async Task<string> CommandNcmPlayAsync(PlayManager playManager, Ts3Client ts3Client, InvokerData invoker, string url_or_keyword)
        {
            var url_or_name = TextUtil.ExtractUrlFromBb(url_or_keyword);
            var count = 0;

            IEnumerable<SongDetail> details = null;
            string urlPrefix = "https://music.163.com/#/song?id=";
            switch (UrlMatcher.MatchUrlType(url_or_name))
            {
                case NcmUrlType.Song:
                    var songId = UrlMatcher.ExtractID(url_or_name);
                    details = await APIHelper.GetSongDetailAsync(Env.Backend, Cookie, songId);
                    break;
                case NcmUrlType.Album:
                    var albumId = UrlMatcher.ExtractID(url_or_name);
                    details = await APIHelper.GetAlbumDetailAsync(Env.Backend, Cookie, albumId);
                    break;
                case NcmUrlType.Playlist:
                    var listId = UrlMatcher.ExtractID(url_or_name);
                    details = await APIHelper.GetPlaylistDetailAsync(Env.Backend, Cookie, listId);
                    break;
                case NcmUrlType.Radio:
                    var readioId = UrlMatcher.ExtractID(url_or_name);
                    details = await APIHelper.GetDjRadioDetailAsync(Env.Backend, Cookie, readioId);
                    urlPrefix = "https://music.163.com/#/program?id=";
                    break;
                case NcmUrlType.Program:
                    var programId = UrlMatcher.ExtractID(url_or_name);
                    details = await APIHelper.GetProgramDetailAsync(Env.Backend, Cookie, programId);
                    urlPrefix = "https://music.163.com/#/program?id=";
                    break;
                default:
                    // 搜索
                    var result = await APIHelper.GetSearchDetailAsync(Env.Backend, Cookie, url_or_keyword);
                    if (result.Count() >= 0)
                    {
                        var list = new List<SongDetail>();
                        list.Add(result.First());
                        details = list;
                    }
                    break;
            }

            if (details == null || details.Count() == 0)
                return $"无法播放，可能版权问题或未开通VIP";

            foreach (var detail in details)
            {
                if (IsSongInQueue(detail.id))
                    continue;
                var ar = new AudioResource(detail.id, detail.title, "ncm")
                    .Add("artist", detail.author)
                    .Add("coverUrl", detail.picUrl);

                if (!string.IsNullOrEmpty(detail.program_id))
                    ar.Add("songUrl", $"{urlPrefix}{detail.program_id}");
                else
                    ar.Add("songUrl", $"{urlPrefix}{detail.id}");

                songQueue.Enqueue(ar);
                count++;
            }

            // 没有歌曲正在播放，开始播放
            if (!playManager.IsPlaying)
            {
                await TryPlayNextAsync(playManager, ts3Client, invoker);
                count--;
            }

            if (count > 0)
            {
                return $"添加了 {count} 首歌到播放队列";
            }

            return null;
        }
        [Command("s")]
        public async Task<string> CommandNcmSkipAsync(PlayManager playManager)
        {
            await playManager.Stop();
            return null;
        }
        [Command("fs")]
        public async Task<string> CommandStopAsync(PlayManager playManager)
        {
            songQueue.Clear();
            await playManager.Stop();
            return null;
        }
        public string processCookie(string cookie)
        {
            string realCookie = string.Empty;
            foreach (string line in cookie.Split("Only;"))
            {
                if (line.Contains("Path=/;"))
                {
                    var kv = line.Split(';')[0];
                    if (!string.IsNullOrEmpty(kv.Split('=')[1]))
                    {
                        realCookie += kv + "; ";
                    }
                }
            }
            return realCookie;
        }

    }
}
