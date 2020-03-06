﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SqueakyBot.Commands
{
    public class WikiCommands
    {
        const string queryAddress = "http://rimworldwiki.com/api.php?action=query&list=search&format=json&srlimit=5&srprop=size|wordcount|timestamp&srsearch={0}";


        [Command("위키검색"), Description("림월드 위키에서 검색합니다.")]
        public async Task Search(CommandContext ctx, [Description("The search query.")] string query)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);
            WebClient webClient = new WebClient();
            string result = await webClient.DownloadStringTaskAsync(string.Format(queryAddress, query)).ConfigureAwait(false);
            JObject jObject = JObject.Parse(result);
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.WithTitle($"Results for '{query}'");
            builder.WithColor(DiscordColor.Azure);
            foreach (JToken token in jObject["query"]["search"])
            {
                builder.AddField(token["title"].ToString(),
                    $"**정보**\n{token["size"]} bytes\n" +
                    $"{token["wordcount"]} words\n" +
                    $"마지막 수정 UTC {DateTime.Parse(token["timestamp"].ToString())}\n" +
                    $"[링크](http://rimworldwiki.com/wiki/{token["title"].ToString().Replace(" ", "%20")})", true);
            }
            await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }
    }
}
