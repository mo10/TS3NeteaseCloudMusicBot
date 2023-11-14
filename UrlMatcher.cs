using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ts3ncm
{
    public enum NcmUrlType
    {
        Unknown,
        Song,
        Album,
        Playlist,
        Radio,
        Program
    }
    public class UrlMatcher
    {
        public static readonly Regex SongMatch = new Regex(@"^(https?://)?music\.163\.com/(#/)?song\?id=(?<id>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ECMAScript);
        public static readonly Regex AlbumMatch = new Regex(@"^(https?://)?music\.163\.com/(#/)?album\?id=(?<id>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ECMAScript);
        public static readonly Regex PlaylistMatch = new Regex(@"^(https?://)?music\.163\.com/(#/)?playlist\?id=(?<id>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ECMAScript);
        public static readonly Regex RadioMatch = new Regex(@"^(https?://)?music\.163\.com/(#/)?djradio\?id=(?<id>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ECMAScript);
        public static readonly Regex ProgramMatch = new Regex(@"^(https?://)?music\.163\.com/(#/)?program\?id=(?<id>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ECMAScript);

        public static NcmUrlType MatchUrlType(string url)
        {
            if (SongMatch.IsMatch(url))
                return NcmUrlType.Song;
            if (AlbumMatch.IsMatch(url))
                return NcmUrlType.Album;
            if (PlaylistMatch.IsMatch(url))
                return NcmUrlType.Playlist;
            if (RadioMatch.IsMatch(url))
                return NcmUrlType.Radio;
            if (ProgramMatch.IsMatch(url))
                return NcmUrlType.Program;

            return NcmUrlType.Unknown;
        }
        public static string ExtractID(string url)
        {
            Regex matcher = null;

            if (SongMatch.IsMatch(url))
                matcher = SongMatch;
            else if (AlbumMatch.IsMatch(url))
                matcher = AlbumMatch;
            else if (PlaylistMatch.IsMatch(url))
                matcher = PlaylistMatch;
            else if (RadioMatch.IsMatch(url))
                matcher = RadioMatch;
            else if (ProgramMatch.IsMatch(url))
                matcher = ProgramMatch;

            return matcher?.Match(url).Groups["id"].Value ?? string.Empty;
        }

    }
}
