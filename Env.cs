using System;
using System.Collections.Generic;
using System.Text;

namespace ts3ncm
{
    public static class Env
    {
        public static string Name { get { return Environment.GetEnvironmentVariable("NCM_BOT_NAME") ?? "MusicBot"; } }
        public static string Backend { get { return Environment.GetEnvironmentVariable("NCM_API_SERVER") ?? "http://127.0.0.1:3000"; } }
    }
}
