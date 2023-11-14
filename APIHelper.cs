using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Helper;

namespace ts3ncm
{
    public static class APIHelper
    {
        private static long GetTimestamp() => DateTimeOffset.Now.ToUnixTimeSeconds();

        #region 登入API
        /// <summary>
        /// 二维码 key 生成接口
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static async Task<string> GetLoginQRKeyAsync(string server)
        {
            var obj =  await WebWrapper.Request($"{server}/login/qr/key?timestamp={GetTimestamp()}").AsJson<RespQRKey>();
            if (obj == null || obj.code != 200)
                return "";

            return obj.data.unikey;
        }
        /// <summary>
        /// 二维码生成接口
        /// </summary>
        /// <param name="server"></param>
        /// <param name="unikey">由第一个接口生成</param>
        /// <returns>二维码图片Stream</returns>
        public static async Task<Stream> GetLoginQRImageAsync(string server, string unikey)
        {
            var obj = await WebWrapper.Request($"{server}/login/qr/create?key={unikey}&qrimg=true&timestamp={GetTimestamp()}").AsJson<RespQRCreate>();
            if (obj == null || obj.code != 200 || obj.data.qrimg.Length == 0)
                return null;

            byte[] imgBytes = Convert.FromBase64String(obj.data.qrimg.Split(',')[1]);
            return new MemoryStream(imgBytes);
        }
        /// <summary>
        /// 二维码检测扫码状态接口
        /// </summary>
        /// <param name="server"></param>
        /// <param name="unikey"></param>
        /// <returns>800 为二维码过期.801 为等待扫码,802 为待确认,803 为授权登录成功(803 状态码下会返回 cookies)</returns>
        public static async Task<RespQRCheck> GetLoginQRStatusAsync(string server, string unikey)
        {
            var obj = await WebWrapper.Request($"{server}/login/qr/check?key={unikey}&timestamp={GetTimestamp()}").AsJson<RespQRCheck>();
            return obj;
        }
        /// <summary>
        /// 登录状态
        /// </summary>
        /// <param name="server"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public static async Task<RespStatus> GetLoginStatusAasync(string server, string cookie)
        {
            return await WebWrapper.Request($"{server}/login/status?timestamp={GetTimestamp()}")
                .WithHeader("Cookie", cookie)
                .AsJson<RespStatus>();
        }
        #endregion

        #region 歌曲信息
        private static IEnumerable<SongDetail> ParseToSongDetails(IEnumerable<JsonSongDetailSong> songs)
        {
            List<SongDetail> details = new List<SongDetail>();
            foreach (var song in songs)
            {
                if (song.noCopyrightRcmd != null)
                    continue;
                var detail = new SongDetail()
                {
                    id = $"{song.id}",
                    title = song.name,
                    author = "",
                    program_id = "",
                };
                if (song.al != null)
                    detail.picUrl = song.al.picUrl;
                if (song.ar != null && song.ar.Length > 0)
                    detail.author = string.Join('/', song.ar.Select(ar => ar.name));

                details.Add(detail);
            }

            return details;
        }

        public static async Task<IEnumerable<SongDetail>> GetAlbumDetailAsync(string server, string cookie, string albumId)
        {
            JsonSongDetail result;
            try
            {
                var request = WebWrapper.Request($"{server}/album?id={albumId}&timestamp={GetTimestamp()}");
                if(!string.IsNullOrEmpty(cookie))
                    request.Headers.Add("Cookie", cookie);
                result = await request.AsJson<JsonSongDetail>();
            }
            catch (Exception ex)
            {
                throw Error.Str($"获取专辑信息失败: {ex}");
            }
            if (result == null || result.code != 200)
                throw Error.Str($"专辑接口查询失败");
            if (result.songs.Length == 0)
                throw Error.Str($"专辑是空的或不存在");

            return ParseToSongDetails(result.songs);
        }
        public static async Task<IEnumerable<SongDetail>> GetPlaylistDetailAsync(string server, string cookie, string listId)
        {
            JsonSongDetail result;
            try
            {
                var request = WebWrapper.Request($"{server}/playlist/track/all?id={listId}&limit=100&timestamp={GetTimestamp()}");
                if (!string.IsNullOrEmpty(cookie))
                    request.Headers.Add("Cookie", cookie);
                result = await request.AsJson<JsonSongDetail>();
            }
            catch (Exception ex)
            {
                throw Error.Str($"获取专辑信息失败: {ex}");
            }
            if (result == null || result.code != 200)
                throw Error.Str($"歌单接口查询失败");
            if (result.songs.Length == 0)
                throw Error.Str($"歌单是空的或不存在");

            return ParseToSongDetails(result.songs);
        }
        public static async Task<IEnumerable<SongDetail>> GetSongDetailAsync(string server, string cookie, string songId)
        {
            JsonSongDetail result;
            try
            {
                var request = WebWrapper.Request($"{server}/song/detail?ids={songId}&timestamp={GetTimestamp()}");
                if (!string.IsNullOrEmpty(cookie))
                    request.Headers.Add("Cookie", cookie);
                result = await request.AsJson<JsonSongDetail>();
            }
            catch (Exception ex)
            {
                throw Error.Str($"获取歌曲信息失败: {ex}");
            }
            if (result == null || result.code != 200)
                throw Error.Str($"歌曲接口查询失败");
            if (result.songs.Length == 0)
                throw Error.Str($"找不到这个歌曲");

            return ParseToSongDetails(result.songs);
        }
        public static async Task<IEnumerable<SongDetail>> GetSearchDetailAsync(string server, string cookie, string keyword)
        {
            JsonSearch result;
            try
            {
                var request = WebWrapper.Request($"{server}/cloudsearch?keywords={Uri.EscapeDataString(keyword)}&timestamp={GetTimestamp()}");
                if (!string.IsNullOrEmpty(cookie))
                    request.Headers.Add("Cookie", cookie);
                result = await request.AsJson<JsonSearch>();
            }
            catch (Exception ex)
            {
                throw Error.Str($"搜索失败: {ex}");
            }
            if (result == null || result.code != 200)
                throw Error.Str($"搜索接口查询失败");
            if (result.result == null || result.result.songs == null || result.result.songs.Length == 0)
                throw Error.Str($"什么都没有搜索到");

            return ParseToSongDetails(result.result.songs);
        }
        
        public static async Task<IEnumerable<SongDetail>> GetDjRadioDetailAsync(string server, string cookie, string radioId)
        {
            RespDjRadio result;
            try
            {
                var request = WebWrapper.Request($"{server}/dj/program?rid={radioId}&limit=100&timestamp={GetTimestamp()}");
                if (!string.IsNullOrEmpty(cookie))
                    request.Headers.Add("Cookie", cookie);

                result = await request.AsJson<RespDjRadio>();
            }
            catch (Exception ex)
            {
                throw Error.Str($"获取电台信息失败: {radioId} {ex}");
            }
            if (result == null || result.code != 200)
                throw Error.Str($"电台接口查询失败");
            if (result.programs.Length == 0)
                throw Error.Str($"电台是空的或不存在");

            List<SongDetail> details = new List<SongDetail>();
            foreach (var song in result.programs)
            {
                var detail = new SongDetail()
                {
                    id = $"{song.mainTrackId}",
                    program_id = $"{song.id}",
                    title = song.name,
                    author = "电台节目",
                    picUrl = song.coverUrl
                };

                details.Add(detail);
            }

            return details;
        }
        public static async Task<IEnumerable<SongDetail>> GetProgramDetailAsync(string server, string cookie, string programId)
        {
            RespProgramDetail result;
            try
            {
                var request = WebWrapper.Request($"{server}/dj/program/detail?id={programId}&timestamp={GetTimestamp()}");
                if (!string.IsNullOrEmpty(cookie))
                    request.Headers.Add("Cookie", cookie);

                result = await request.AsJson<RespProgramDetail>();
            }
            catch (Exception ex)
            {
                throw Error.Str($"获取节目信息失败: {programId} {ex}");
            }
            if (result == null || result.code != 200)
                throw Error.Str($"节目接口查询失败");

            List<SongDetail> details = new List<SongDetail>
            {
                new SongDetail()
                {
                    id = $"{result.program.mainTrackId}",
                    program_id = $"{result.program.id}",
                    title = result.program.name,
                    author = "电台节目",
                    picUrl = result.program.coverUrl
                }
            };

            return details;
        }

        public static async Task<bool> CheckSongCanPlay(string server, string cookie, string songId)
        {
            var request = WebWrapper.Request($"{server}/check/music?id={songId}");
            if (!string.IsNullOrEmpty(cookie))
                request.Headers.Add("Cookie", cookie);
            var result = await request.AsJson<JsonCheckMusic>();

            return result.success;
        }
        public static async Task<string> GetAudioUrlAsync(string server, string cookie, string songId)
        {
            JsonSongUrlInfo result;
            try
            {
                var request = WebWrapper.Request($"{server}/song/url?id={songId}&br=320000&timestamp={GetTimestamp()}");
                if (!string.IsNullOrEmpty(cookie))
                    request.Headers.Add("Cookie", cookie);
                result = await request.AsJson<JsonSongUrlInfo>();
            }
            catch (Exception ex)
            {
                throw Error.Str($"获取播放链接失败: {ex}");
            }
            if (result == null || result.code != 200)
                throw Error.Str($"播放链接接口查询失败");

            if (result.data == null || result.data.Length == 0 || result.data[0].url == null)
            {
                throw Error.Str($"没有播放路径: {songId}");
            }

            return result.data[0].url;
        }
        #endregion
    }
}
