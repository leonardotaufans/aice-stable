using aice_stable.services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace aice_stable
{
    public sealed class AiceHelpFormatter : BaseHelpFormatter
    {
        private Aice aice { get; }
        private StringBuilder message { get; }
        private bool _hasCommand = false;

        public AiceHelpFormatter(CommandContext ctx, Aice bot) : base(ctx)
        {
            aice = bot;
            message = new StringBuilder();
            this.message.AppendLine("```")
                .AppendLine($"AICE bot v{bot.BotVersion}")
                .AppendLine();
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            this._hasCommand = true;
            this.message.AppendLine(command.QualifiedName)
                .AppendLine(command.Description);

            if (command.Aliases?.Any() == true)
                this.message.AppendLine($"Aliases: {string.Join(" ", command.Aliases)}");
            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            if (this._hasCommand)
                this.message.AppendLine()
                    .AppendLine("Available subcommands:");
            else
                this.message.AppendLine("Available commands:");

            var maxLen = subcommands.Max(x => x.Name.Length) + 2;
            foreach (var cmd in subcommands)
                this.message.AppendLine($"{cmd.Name.ToFixedWidth(maxLen)}    {cmd.Description}");

            return this;
        }

        public override CommandHelpMessage Build()
        {
            this.message.Append("```");
            return new CommandHelpMessage(this.message.ToString());
        }

    }
}