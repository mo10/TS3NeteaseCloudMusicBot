using System;
using System.Collections.Generic;
using System.Text;

namespace ts3ncm
{
#pragma warning disable CS8632

    #region 登入响应Json
    public class RespQRKey
    {
        public class Data
        {
            public int code { get; set; }
            public string unikey { get; set; }
        }
        public Data data { get; set; }
        public int code { get; set; }
    }
    public class RespQRCreate
    {
        public class Data
        {
            public string qrurl { get; set; }
            public string qrimg { get; set; }
        }
        public Data data { get; set; }
        public int code { get; set; }
    }
    public class RespQRCheck
    {
        public int code { get; set; }
        public string cookie { get; set; }
        public string message { get; set; }
        public string nickname { get; set; }
        public string avatarUrl { get; set; }
    }
    public class RespStatus
    {
        public class Account
        {
            public bool anonimousUser { get; set; }
            public int ban { get; set; }
            public int baoyueVersion { get; set; }
            public long createTime { get; set; }
            public int donateVersion { get; set; }
            public long id { get; set; }
            public bool paidFee { get; set; }
            public int status { get; set; }
            public int tokenVersion { get; set; }
            public int type { get; set; }
            public string userName { get; set; }
            public int vipType { get; set; }
            public int whitelistAuthority { get; set; }
        }

        public class Profile
        {
            public int accountStatus { get; set; }
            public int accountType { get; set; }
            public string avatarUrl { get; set; }
            public string nickname { get; set; }
            public int userId { get; set; }
            public int userType { get; set; }

            public int vipType { get; set; }
            public long viptypeVersion { get; set; }
        }
        public class Data
        {
            public int code { get; set; }
            public Account? account { get; set; }
            public Profile? profile { get; set; }
        }
        public Data data { get; set; }
    }
    #endregion

    public class ProgramData
    {
        // public DjInfo? dj { get; set; }
        public string coverUrl { get; set; }
        public long id { get; set; }
        public long mainTrackId { get; set; }
        public string name { get; set; }
    }
    public class RespDjRadio
    {
        public int code { get; set; }
        public int count { get; set; }
        public bool more { get; set; }
        public ProgramData[] programs { get; set; }
    }
    public class RespProgramDetail
    {
        public int code { get; set; }
        public ProgramData? program { get; set; }
    }

    public class SongDetail
    {
        public string id { get; set; }
        public string program_id { get; set; }
        public string title { get; set; }
        public string author { get; set; }
        public string picUrl { get; set; }
    }
    public class JsonSongDetailSongAlbum
    {
        public string? name { get; set; }
        public string? picUrl { get; set; }
    }
    public class JsonSongDetailSongAuthor
    {
        public int? id { get; set; }
        public string? name { get; set; }
    }
    public class JsonSongDetailSong
    {
        public long id { get; set; }
        public string? name { get; set; }
        public object? noCopyrightRcmd { get; set; }
        public JsonSongDetailSongAlbum? al { get; set; }
        public JsonSongDetailSongAuthor[]? ar { get; set; }
    }
    public class JsonSongDetail
    {
        public int code { get; set; }
        public JsonSongDetailSong[]? songs { get; set; }
    }
    public class JsonSongUrlData
    {
        public int code { get; set; }
        public long id { get; set; }
        public string? type { get; set; }
        public string? url { get; set; }

    }
    public class JsonSongUrlInfo
    {
        public int code { get; set; }
        public JsonSongUrlData[]? data { get; set; }
    }
    public class JsonCheckMusic
    {
        public bool success { get; set; }
        public string? message { get; set; }
    }
    public class JsonSearchResult
    {
        public int songCount { get; set; }
        public JsonSongDetailSong[]? songs { get; set; }

    }
    public class JsonSearch
    {
        public int code { get; set; }
        public JsonSearchResult? result { get; set; }
    }
    public class JsonStatusCode
    {
        public int code { get; set; }
    }
}