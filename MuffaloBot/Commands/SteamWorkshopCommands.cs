using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SqueakyBot.Commands
{
    public class SteanWorkshopCommands
    {
        const string query = "https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?key={0}&format=json&numperpage={1}&appid=294100&match_all_tags=1&search_text={2}&return_short_description=1&return_metadata=1&query_type={3}";
        JObject Query(string content, string key, byte resultsCap = 5)
        {
            if (resultsCap > 20) resultsCap = 20;
            string request = string.Format(query, key, resultsCap, content, 3.ToString());
            HttpWebRequest req = WebRequest.CreateHttp(request);
            StreamReader reader = new StreamReader(req.GetResponse().GetResponseStream());
            return JObject.Parse(reader.ReadToEnd());
        }
        [Command("창작마당"), Description("창작마당에서 컨텐츠를 검색합니다.")]
        public async Task Search(CommandContext ctx, [RemainingText, Description("찾을 단어.")] string 쿼리)
        {
            JObject result = Query(쿼리, AuthResources.SteamApiKey, 5);
            if (result["response"]["total"].Value<int>() == 0)
            {
                await ctx.RespondAsync("결과가 없네요.");
            }
            else
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
                embedBuilder.WithColor(DiscordColor.DarkBlue);
                embedBuilder.WithTitle($"Results for '{쿼리}'");
                embedBuilder.WithDescription("전체 결과: " + result["response"]["total"]);
                foreach (JToken item in result["response"]["publishedfiledetails"])
                {
                    embedBuilder.AddField(item["title"].ToString(),
                        $"**조회수**: {item["views"]}\n" +
                        $"**요약**: {item["subscriptions"]}\n" +
                        $"**즐겨찾기**: {item["favorited"]}\n**ID**: {item["publishedfileid"]}\n" +
                        $"[링크](http://steamcommunity.com/sharedfiles/filedetails/?id={item["publishedfileid"]})",
                        true);
                }
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }
    }
}
