﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqueakyBot.Modules
{
    public class LogManagerModule : BaseModule
    {
        static readonly MethodInfo memberwiseCloneMethod = typeof(DiscordMessage).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
        DiscordMessage[] discordMessageCache = new DiscordMessage[1000];
        int currentIndex = 0;

        protected override void Setup(DiscordClient client)
        {
            Client = client;
            client.MessageCreated += OnReceiveDiscordCreateLog;
            //client.MessageDeleted += OnReceiveDiscordDeleteLog;
            //client.MessageUpdated += OnReceiveDiscordModifyLog;
        }

        int FindIndexOfIdInCache(ulong id)
        {
            for (int i = currentIndex; i < discordMessageCache.Length; i++)
                if (discordMessageCache[i] != null && discordMessageCache[i].Id == id)
                    return i;

            for (int i = 0; i < currentIndex; i++)
                if (discordMessageCache[i] != null && discordMessageCache[i].Id == id)
                    return i;

            return -1;
        }
        DiscordMessage ShallowCopyOf(DiscordMessage message)
        {
            return (DiscordMessage)memberwiseCloneMethod.Invoke(message, new object[0]);
        }
        Task OnReceiveDiscordCreateLog(MessageCreateEventArgs e)
        {
            return PushMessage(e.Message);
        }

        async Task PushMessage(DiscordMessage message)
        {
            await Task.Run(() => discordMessageCache[currentIndex = (++currentIndex % discordMessageCache.Length)] = ShallowCopyOf(message)).ConfigureAwait(false);
        }

        async Task OnReceiveDiscordDeleteLog(MessageDeleteEventArgs e)
        {
            if (e.Message.Channel?.Name == "logs" || (e.Message.Author?.IsBot ?? true)) return;      
            if(e.Guild!=null)
            {
                int ind = -1;
                if ((ind = FindIndexOfIdInCache(e.Message.Id)) != -1)
                {
                    await NotifyDeleteAsync(discordMessageCache[ind], e.Guild);
                    discordMessageCache[ind] = null;
                }
                else
                {
                    await NotifyDeleteAsync(e.Message, e.Guild);
                }
            }
            
        }
        async Task OnReceiveDiscordModifyLog(MessageUpdateEventArgs e)
        {
            if (e.Message.Channel.Name == "logs" || e.Message == null || string.IsNullOrEmpty(e.Message.Content) || e.Message.Author.IsBot) return;
            if(e.Guild!=null)
            {
                int ind = -1;
                if ((ind = FindIndexOfIdInCache(e.Message.Id)) != -1)
                {
                    DiscordMessage before = discordMessageCache[ind];
                    await NotifyModifyAsync(before, e.Message, e.Guild);
                    discordMessageCache[ind] = (DiscordMessage)memberwiseCloneMethod.Invoke(e.Message, new object[0]);
                }
                else
                {
                    await NotifyModifyAsync(null, e.Message, e.Guild);
                    await PushMessage(e.Message);
                }
            }
            
        }
        async Task NotifyDeleteAsync(DiscordMessage message, DiscordGuild guild)
        {
            if (guild == null) return;
            string content;
            content = message.Content ?? "(메세지가 너무 오래되었습니다...)";
            if (content.Length == 0)
            {
                content = "(Empty)";
            }
            DiscordChannel channel = (await guild.GetChannelsAsync()).First(c => c.Name == "logs");
            if (channel != null)
            {
                Permissions permissions = channel.PermissionsFor(guild.CurrentMember);
                if ((permissions & Permissions.SendMessages) != 0)
                {
                    DiscordEmbedBuilder embedBuilder = MakeDeleteMessageEmbed(message, content);
                    await channel.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
                }
            }
        }
        async Task NotifyModifyAsync(DiscordMessage before, DiscordMessage after, DiscordGuild guild)
        {
            string content;
            content = before?.Content ?? "(메세지가 너무 오래되었습니다...)";
            if (content.Length == 0)
            {
                content = "(Empty)";
            }
            if (content == after.Content) return;
            DiscordChannel channel = (await guild.GetChannelsAsync()).First(c => c.Name == "logs");
            if (channel != null)
            {
                Permissions permissions = channel.PermissionsFor(guild.CurrentMember);
                if ((permissions & Permissions.SendMessages) != 0)
                {
                    DiscordEmbedBuilder embedBuilder = MakeModifyMessageEmbed(after, content);
                    await channel.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
                }
            }
        }

        static DiscordEmbedBuilder MakeDeleteMessageEmbed(DiscordMessage message, string content)
        {
            content = content.Replace("`", "'");
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.WithTitle("Message Deleted");
            embedBuilder.WithAuthor($"{message.Author.Username} #{message.Author.Discriminator}", null, message.Author.AvatarUrl);
            embedBuilder.WithColor(DiscordColor.Red);
            embedBuilder.WithDescription($"```\n{content}```");
            embedBuilder.AddField("ID", message.Id.ToString(), true);
            embedBuilder.AddField("Author ID", message.Author.Id.ToString(), true);
            embedBuilder.AddField("Channel", "#" + message.Channel.Name, true);
            embedBuilder.AddField("Timestamp (UTC)", message.Timestamp.ToUniversalTime().ToString(), true);
            IReadOnlyList<DiscordAttachment> attachments = message.Attachments;
            for (int i = 0; i < attachments.Count; i++)
            {
                embedBuilder.AddField($"Attachment {i + 1}", $"{attachments[i].FileName} ({attachments[i].FileSize}) {attachments[i].Url}", true);
            }
            return embedBuilder;
        }
        static DiscordEmbedBuilder MakeModifyMessageEmbed(DiscordMessage after, string content)
        {
            content = content.Replace("`", "'");
            string afterContent = after.Content?.Replace("`", "'");
            if (string.IsNullOrEmpty(afterContent))
            {
                afterContent = "(Empty)";
            }
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.WithTitle("Message Modified");
            embedBuilder.WithAuthor($"{after.Author.Username} #{after.Author.Discriminator}", null, after.Author.AvatarUrl);
            embedBuilder.WithColor(DiscordColor.Yellow);
            embedBuilder.WithDescription($"Before```\n{content}```After```\n{afterContent}```");
            embedBuilder.AddField("ID", after.Id.ToString(), true);
            embedBuilder.AddField("Author ID", after.Author.Id.ToString(), true);
            embedBuilder.AddField("Channel", "#" + after.Channel.Name, true);
            embedBuilder.AddField("Timestamp (UTC)", after.Timestamp.ToUniversalTime().ToString(), true);
            IReadOnlyList<DiscordAttachment> attachments = after.Attachments;
            for (int i = 0; i < attachments.Count; i++)
            {
                embedBuilder.AddField($"Attachment {i + 1}", $"{attachments[i].FileName} ({attachments[i].FileSize}) {attachments[i].Url}", true);
            }
            return embedBuilder;
        }
    }
}
