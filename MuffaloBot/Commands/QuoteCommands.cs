using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SqueakyBot.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SqueakyBot.Commands
{
    public class QuoteCommands
    {
        [Command("대사집"), Aliases("quote", "listquotes"), Description("사용 가능한 모든 대사를 보여줍니다.")]
        public async Task ListQuotesAsync(CommandContext ctx)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Listing all quotes:");
            JObject data = ctx.Client.GetModule<JsonDataModule>().data;
            foreach (var item in data["quotes"])
            {
                JProperty pair = (JProperty)item;
                stringBuilder.Append($"`{pair.Name}` ");
            }
            await ctx.RespondAsync(stringBuilder.ToString());
        }
    }
}
