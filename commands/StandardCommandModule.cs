using System.Threading;
using System.Runtime.InteropServices.ComTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using aice_stable.models;
using aice_stable.services;

namespace aice_stable.commands
{
    public class StandardCommandModule : BaseCommandModule
    {
        private MusicPlayer MusicPlayer { get; set; }
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

        [Command("avatar"), Description("Displays the avatar of the user")]
        public async Task ShowAvatarCommand(CommandContext context, DiscordUser user)
        {
            DiscordUser userTarget;
            if (user == null)    ///Checks if var user is empty
                userTarget = context.Member;
            else
                userTarget = user;
            await context.Channel.SendMessageAsync($"{userTarget.AvatarUrl}");
        }

        [Command("nowplay_debug"), Description("Test button for a now_playing replacement")]
        public async Task ButtonTestCommand(CommandContext context)
        {
            /// <summary>
            /// This would be laughably inefficient, but...
            /// </summary>
            /// <value></value>
            var rewind = new DiscordButtonComponent(ButtonStyle.Primary, "rewind", DiscordEmoji.FromName(context.Client, ":track_previous:"));
            var play = new DiscordButtonComponent(ButtonStyle.Primary, "play", DiscordEmoji.FromName(context.Client, ":arrow_forward:"));
            var pause = new DiscordButtonComponent(ButtonStyle.Primary, "pause", DiscordEmoji.FromName(context.Client, ":pause_button:"));
            var skip = new DiscordButtonComponent(ButtonStyle.Primary, "skip", DiscordEmoji.FromName(context.Client, ":track_next:"));

            // Placeholder message
            String messageContent = "Now Playing: Never Gonna Give You Up - Rick Astley";
            
            var buttons = new DiscordButtonComponent[] {rewind, pause, play, skip};
            var builder = new DiscordMessageBuilder();
            builder.WithContent(messageContent)
                    .AddComponents(buttons)
                    .AddComponents(new DiscordComponent[] {new DiscordLinkButtonComponent("https://youtu.be/kWPQyCIKTtk", "YABE"),});
            var message = await builder.SendAsync(context.Channel);
            context.Client.ComponentInteractionCreated += async (s, e) =>
            {
                if (e.Id.Equals("play"))
                {
                    var playButtons = new DiscordButtonComponent[] {rewind, pause, skip};
                    var _playBuilder = new DiscordInteractionResponseBuilder()
                        .WithContent(messageContent)
                        .AddComponents(playButtons);
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, 
                        _playBuilder);
                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Play button pressed"));
                }
                else if(e.Id.Equals("pause"))
                {
                    var playButtons = new DiscordButtonComponent[] {rewind, play, skip};
                    var _playBuilder = new DiscordInteractionResponseBuilder()
                        .WithContent(messageContent)
                        .AddComponents(playButtons);
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, 
                        _playBuilder);
                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Pause button pressed"));
                }
                else if(e.Id.Equals("rewind"))
                {
                    var playButtons = new DiscordButtonComponent[] {pause, skip};
                    var _playBuilder = new DiscordInteractionResponseBuilder()
                        .WithContent(messageContent)
                        .AddComponents(playButtons);
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, 
                        _playBuilder);
                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Rewind button pressed"));
                }
                else if(e.Id.Equals("skip"))
                {
                    var playButtons = new DiscordButtonComponent[] {rewind, pause};
                    var _playBuilder = new DiscordInteractionResponseBuilder()
                        .WithContent(messageContent)
                        .AddComponents(playButtons);
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, 
                        _playBuilder);
                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Skip button pressed"));
                }
            };
        }

    }
}