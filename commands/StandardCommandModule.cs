using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace aice_stable.commands
{
    public class StandardCommandModule : BaseCommandModule
    {
        
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
        /// 
        /// </summary>
        [Command("ping"), Description("Checks the connection to the server.")]
        public async Task PingCommand(CommandContext context)
        {
            await context.RespondAsync($"Ping: {context.Client.Ping}ms");
        }
    }
}