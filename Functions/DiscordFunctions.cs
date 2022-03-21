using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SlickReship_Payments.Functions
{
    public static class DiscordFunctions
    {
        public static JObject GetConfig()
        {
            // Get the config file.
            using var configJson = new StreamReader(Directory.GetCurrentDirectory() + @"/config.json");
            return (JObject) JsonConvert.DeserializeObject(configJson.ReadToEnd());
        }
    }
}