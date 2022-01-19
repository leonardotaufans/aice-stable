using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace aice_stable.commands
{
    public class StandardCommandModule : BaseCommandModule
    {
        private Aice aice { get; }
        /// <summary>
        /// Returns the version of the bot
        /// </summary>
        [Command("version"), Description("Version information")]
        public async Task DebugVersion(CommandContext context)
        {
            var bot = aice;
            await context.RespondAsync($"AICE bot {bot.BotVersion}");
        }
        /// <summary>
        /// These are just for debugging purposes.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("greet")]
        public async Task GreetCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("Greetings, human.");
        }
        [Command("greet")]
        public async Task GreetCommand(CommandContext ctx, [Description("Tag the user.")] DiscordUser user)
        {
            await ctx.RespondAsync($"Greetings, {user.Mention}.");
        }

        /// <summary>
        /// For checking the bot's ping
        /// </summary>
        [Command("ping"), Description("Checks the connection to the server.")]
        public async Task PingCommand(CommandContext context)
        {
            await context.RespondAsync($"Ping: {context.Client.Ping}ms");
        }

        /// <summary>
        /// Displays the avatar of a user.
        /// </summary>
        /// <param name="user">DiscordUser that is tagged by the summoner.</param>

        [Command("avatar"), Description("Displays the avatar of the user")]
        public async Task ShowAvatarCommand(CommandContext context, DiscordUser user)
        {
            DiscordUser userTarget;
            if (user == null)    ///Checks if var user is empty, which means no one was tagged.
                userTarget = context.Member; /// CommandContext.Member is the one calling the command
            else
                userTarget = user;
            await context.Channel.SendMessageAsync($"{userTarget.AvatarUrl}");
        }
    }
}