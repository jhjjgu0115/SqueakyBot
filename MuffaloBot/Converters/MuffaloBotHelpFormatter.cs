using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqueakyBot.Converters
{
    /// <summary>
    /// Help formatter based off of the default help formatter
    /// </summary>
    public class SqueakyBotHelpFormatter : IHelpFormatter
    {
        /// <summary>
        /// Creates a new default help formatter.
        /// </summary>
        public SqueakyBotHelpFormatter()
        {
            _embed = new DiscordEmbedBuilder();
            _name = null;
            _desc = null;
            _gexec = false;
        }

        /// <summary>
        /// Sets the name of the current command.
        /// </summary>
        /// <param name="name">Name of the command for which the help is displayed.</param>
        /// <returns>Current formatter.</returns>
        public IHelpFormatter WithCommandName(string name)
        {
            _name = name;
            return this;
        }

        /// <summary>
        /// Sets the description of the current command.
        /// </summary>
        /// <param name="description">Description of the command for which help is displayed.</param>
        /// <returns>Current formatter.</returns>
        public IHelpFormatter WithDescription(string description)
        {
            _desc = description;
            return this;
        }

        /// <summary>
        /// Sets aliases for the current command.
        /// </summary>
        /// <param name="aliases">Aliases of the command for which help is displayed.</param>
        /// <returns>Current formatter.</returns>
        public IHelpFormatter WithAliases(IEnumerable<string> aliases)
        {
            if (aliases.Any())
            {
                _embed.AddField("Aliases", string.Join(", ", aliases.Select(new Func<string, string>(Formatter.InlineCode))), false);
            }
            return this;
        }

        /// <summary>
        /// Sets the arguments the current command takes.
        /// </summary>
        /// <param name="arguments">Arguments that the command for which help is displayed takes.</param>
        /// <returns>Current formatter.</returns>
        public IHelpFormatter WithArguments(IEnumerable<CommandArgument> arguments)
        {
            if (arguments.Any<CommandArgument>())
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (CommandArgument commandArgument in arguments)
                {
                    if (commandArgument.IsOptional || commandArgument.IsCatchAll)
                    {
                        stringBuilder.Append("`[");
                    }
                    else
                    {
                        stringBuilder.Append("`<");
                    }
                    stringBuilder.Append(commandArgument.Name);
                    if (commandArgument.IsCatchAll)
                    {
                        stringBuilder.Append("...");
                    }
                    if (commandArgument.IsOptional || commandArgument.IsCatchAll)
                    {
                        stringBuilder.Append("]: ");
                    }
                    else
                    {
                        stringBuilder.Append(">: ");
                    }
                    stringBuilder.Append(commandArgument.Type.ToUserFriendlyName()).Append("`: ");
                    stringBuilder.Append(string.IsNullOrWhiteSpace(commandArgument.Description) ? "No description provided." : commandArgument.Description);
                    if (commandArgument.IsOptional)
                    {
                        stringBuilder.Append(" Default value: ").Append(commandArgument.DefaultValue);
                    }
                    stringBuilder.AppendLine();
                }
                _embed.AddField("명령인자", stringBuilder.ToString(), false);
            }
            return this;
        }

        /// <summary>
        /// When the current command is a group, this sets it as executable.
        /// </summary>
        /// <returns>Current formatter.</returns>
        public IHelpFormatter WithGroupExecutable()
        {
            _gexec = true;
            return this;
        }

        /// <summary>
        /// Sets subcommands of the current command. This is also invoked for top-level command listing.
        /// </summary>
        /// <param name="subcommands">Subcommands of the command for which help is displayed.</param>
        /// <returns>Current formatter.</returns>
        public IHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            if (subcommands.Any())
            {
                _embed.AddField((_name != null) ? "추가명령어" : "명령어", string.Join(", ", from xc in subcommands
                                                                                                          select Formatter.InlineCode(xc.QualifiedName)), false);
            }
            return this;
        }

        /// <summary>
        /// Construct the help message.
        /// </summary>
        /// <returns>Data for the help message.</returns>
        public CommandHelpMessage Build()
        {
            _embed.Title = "찍찍이 명령어";
            _embed.Color = DiscordColor.Green;
            string description = "모든 찍찍이 명령어를 출력합니다. `!도움 <명령어>` 를 통해 더 자세히 알아볼 수 있습니다. `!대사집`이라고 하면 기타 명령어를 보여줍니다.";
            if (_name != null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(Formatter.InlineCode(_name)).Append(": ").Append(string.IsNullOrWhiteSpace(_desc) ? "설명이 제공되지 않았습니다." : _desc);
                if (_gexec)
                {
                    stringBuilder.AppendLine().AppendLine().Append("이 명령어는 독립적으로 실행 가능한 명령어 입니다.");
                }
                description = stringBuilder.ToString();
            }
            _embed.Description = description;
            return new CommandHelpMessage(null, _embed);
        }

        private DiscordEmbedBuilder _embed;

        private string _name;

        private string _desc;

        private bool _gexec;
    }
}
