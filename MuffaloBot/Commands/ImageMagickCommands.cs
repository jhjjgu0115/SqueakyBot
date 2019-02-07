using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Http;
using SqueakyBot.Attributes;

namespace SqueakyBot.Commands
{
    [Cooldown(1, 60, CooldownBucketType.User), RequireChannelInGuild("RimWorld", "bot-commands")]
    public class ImageMagickCommands
    {
        enum ImageEditMode
        {
            Swirl,
            Rescale,
            Wave,
            Implode,
            JPEG,
            MoreJPEG,
            MostJPEG
        }
        [Command("소용돌이"), Description("소용돌이 이펙트를 적용합니다. 링크와 함께 제공됩니다.")]
        public async Task ImageMagickDistort(CommandContext ctx, [Description("Optional. The link to the image you want to apply the effect to.")] string link = null)
        {
            await DoImageMagickCommand(ctx, ImageEditMode.Swirl, link).ConfigureAwait(false);
        }
        [Command("갸우뚱"), Description("살짝 기울어진 이펙트를 적용합니다. 링크와 함께 제공됩니다.")]
        public async Task ImageMagickWonky(CommandContext ctx, [Description("Optional. The link to the image you want to apply the effect to.")] string link = null)
        {
            await DoImageMagickCommand(ctx, ImageEditMode.Rescale, link).ConfigureAwait(false);
        }
        [Command("출렁"), Description("물결 이펙트를 적용합니다. 링크와 함께 제공됩니다.")]
        public async Task ImageMagickWave(CommandContext ctx, [Description("Optional. The link to the image you want to apply the effect to.")] string link = null)
        {
            await DoImageMagickCommand(ctx, ImageEditMode.Wave, link).ConfigureAwait(false);
        }
        [Command("터짐"), Description("폭8이펙트가 적용합니다. 링크와 함께 제공됩니다.")]
        public async Task ImageMagickImplode(CommandContext ctx, [Description("Optional. The link to the image you want to apply the effect to.")] string link = null)
        {
            await DoImageMagickCommand(ctx, ImageEditMode.Implode, link).ConfigureAwait(false);
        }
        [Command("jpeg"), Description("많이 압축시킨 jpeg입니다. 링크와 함께 제공됩니다.")]
        public async Task ImageMagickJPEG(CommandContext ctx, [Description("Optional. The link to the image you want to apply the effect to.")] string link = null)
        {
            await DoImageMagickCommand(ctx, ImageEditMode.JPEG, link).ConfigureAwait(false);
        }
        [Command("moarjpeg"), Description("`!jpeg`보다 더 압축시킨 jpeg입니다. 링크와 함께 제공됩니다.")]
        public async Task ImageMagickMoreJPEG(CommandContext ctx, [Description("Optional. The link to the image you want to apply the effect to.")] string link = null)
        {
            await DoImageMagickCommand(ctx, ImageEditMode.MoreJPEG, link).ConfigureAwait(false);
        }
        [Command("mostjpeg"), Description("압축된 이미지입니다. 너무 많이 압축한 나머지 트럼보 발에 밟힌 랫킨과도 같습니다. 링크와 함께 제공됩니다.")]
        public async Task ImageMagickMostJPEG(CommandContext ctx, [Description("Optional. The link to the image you want to apply the effect to.")] string link = null)
        {
            await DoImageMagickCommand(ctx, ImageEditMode.MostJPEG, link).ConfigureAwait(false);
        }
        async Task DoImageMagickCommand(CommandContext ctx, ImageEditMode mode, string link)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);
            string attachmentUrl = null;
            if (!string.IsNullOrWhiteSpace(ctx.RawArgumentString) && link != null && Uri.TryCreate(link, UriKind.Absolute, out Uri uri))
            {
                attachmentUrl = link;
            }
            else
            {
                IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync(10);
                for (int i = 0; i < messages.Count; i++)
                {
                    if (messages[i].Attachments.Count != 0)
                    {
                        attachmentUrl = messages[i].Attachments[0].Url;
                        break;
                    }
                }
            }
            if (!string.IsNullOrEmpty(attachmentUrl))
            {
                HttpClient client = new HttpClient();
                byte[] buffer;
                try
                {
                    buffer = await client.GetByteArrayAsync(attachmentUrl);
                }
                catch (HttpRequestException)
                {
                    await ctx.RespondAsync("이미지링크에 접속할수 없습니다.");
                    return;
                }
                if (attachmentUrl.EndsWith(".gif"))
                {
                    await DoImageMagickCommandForGif(ctx, buffer, mode);
                }
                else
                {
                    await DoImageMagickCommandForStillImage(ctx, buffer, mode);
                }
            }
            else
            {
                await ctx.RespondAsync("이미지를 찾을 수 없습니다.");
            }
        }
        async Task DoImageMagickCommandForGif(CommandContext ctx, byte[] buffer, ImageEditMode mode)
        {
            if (mode == ImageEditMode.Rescale)
            {
                await ctx.RespondAsync("이 모드는 속도가 느리고 이미지사이즈에 기반해서 느려지기 때문에 gif는 지원하지 않습니다.");
                return;
            }
            MagickImageCollection image;
            try
            {
                image = new MagickImageCollection(buffer);
            }
            catch (MagickMissingDelegateErrorException)
            {
                await ctx.RespondAsync("이미지 파일확장자를 알아볼 수 없다.");
                return;
            }
            int originalWidth = image[0].Width, originalHeight = image[0].Height;
            if (originalHeight * originalWidth > 1000000)
            {
                await ctx.RespondAsync($"Gif exceeds maximum size of 1000000 pixels (Actual size: {originalHeight * originalWidth})");
                return;
            }
            if (image.Count > 100)
            {
                await ctx.RespondAsync($"Gif exceeds maximum frame count of 100 pixels (Actual count: {image.Count})");
                return;
            }
            image.Coalesce();
            long rawLength;
            using (MemoryStream stream = new MemoryStream())
            {
                image.Write(stream);
                rawLength = stream.Length;
            }
            double exceed = rawLength / 4194304d;
            double rescale = 1f;
            if (exceed > 1.0)
            {
                rescale = Math.Sqrt(exceed);
            }
            await ctx.TriggerTypingAsync();
            for (int i = 0; i < image.Count; i++)
            {
                IMagickImage frame = image[i];
                if (rescale > 1f)
                {
                    if (rescale > 2f)
                    {
                        frame.AdaptiveResize((int)(frame.Width / rescale), (int)(frame.Height / rescale));
                    }
                    else
                    {
                        frame.Resize((int)(frame.Width / rescale), (int)(frame.Height / rescale));
                    }
                }
                DoMagic(mode, frame, originalWidth, originalHeight);
            }
            await ctx.TriggerTypingAsync();
            image.OptimizeTransparency();
            using (Stream stream = new MemoryStream())
            {
                image.Write(stream);
                stream.Seek(0, SeekOrigin.Begin);
                await ctx.RespondWithFileAsync(stream, "magic.gif");
            }
        }
        async Task DoImageMagickCommandForStillImage(CommandContext ctx, byte[] buffer, ImageEditMode mode)
        {
            MagickImage image;
            try
            {
                image = new MagickImage(buffer);
            }
            catch (MagickMissingDelegateErrorException)
            {
                await ctx.RespondAsync("Image format not recognised.");
                return;
            }
            int originalWidth = image.Width, originalHeight = image.Height;
            if (originalHeight * originalWidth > 2250000)
            {
                await ctx.RespondAsync($"Image exceeds maximum size of 2250000 pixels (Actual size: {originalHeight * originalWidth})");
                return;
            }
            // Do magic
            double exceed = buffer.Length / 8388608d;
            double rescale = 1f;
            if (exceed > 1.0)
            {
                rescale = 1.0 / Math.Sqrt(exceed);
            }
            if (rescale < 1f)
            {
                if (rescale < 0.5f)
                {
                    image.AdaptiveResize((int)(image.Width * rescale), (int)(image.Height * rescale));
                }
                else
                {
                    image.Resize((int)(image.Width * rescale), (int)(image.Height * rescale));
                }
            }
            DoMagic(mode, image, originalWidth, originalHeight);
            using (Stream stream = new MemoryStream())
            {
                if (mode == ImageEditMode.JPEG || mode == ImageEditMode.MoreJPEG || mode == ImageEditMode.MostJPEG)
                {
                    image.Write(stream, MagickFormat.Jpeg);
                }
                else
                {
                    image.Write(stream);
                }
                stream.Seek(0, SeekOrigin.Begin);
                if (mode == ImageEditMode.JPEG || mode == ImageEditMode.MoreJPEG || mode == ImageEditMode.MostJPEG)
                {
                    await ctx.RespondWithFileAsync(stream, "magic.jpeg");
                }
                else
                {
                    await ctx.RespondWithFileAsync(stream, "magic.png");
                }
            }
        }

        void DoMagic(ImageEditMode mode, IMagickImage image, int originalWidth, int originalHeight)
        {
            switch (mode)
            {
                case ImageEditMode.Swirl:
                    image.Swirl(360);
                    break;
                case ImageEditMode.Rescale:
                    image.LiquidRescale(image.Width / 2, image.Height / 2);
                    image.LiquidRescale((image.Width * 3) / 2, (image.Height * 3) / 2);
                    image.Resize(originalWidth, originalHeight);
                    break;
                case ImageEditMode.Wave:
                    image.BackgroundColor = MagickColor.FromRgb(0, 0, 0);
                    image.Wave(image.Interpolate, 10.0, 150.0);
                    break;
                case ImageEditMode.Implode:
                    image.Implode(0.5d, PixelInterpolateMethod.Average);
                    break;
                case ImageEditMode.JPEG:
                    image.Quality = 10;
                    break;
                case ImageEditMode.MoreJPEG:
                    image.Quality = 5;
                    break;
                case ImageEditMode.MostJPEG:
                    image.Quality = 1;
                    break;
                default:
                    break;
            }
        }
    }
}
