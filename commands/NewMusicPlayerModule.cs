using System;
using System.Linq;
using System.Threading.Tasks;
using aice_stable.models;
using aice_stable.services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace aice_stable 
{
    public class NewMusicPlayerModule : BaseCommandModule
    {
        private MusicPlayer MusicPlayer { get; set; }

        private DiscordButtonComponent shuffle, noShuffle, rewind, play, pause, skip, stop, playlist, repeatMode;
        private DiscordButtonComponent[] component = {};
        private DiscordButtonComponent[] component2 = {};
        private DiscordButtonComponent[] component3 = {};

        [Command("version"), Description("Version information")]
        public async Task DebugVersion(CommandContext context)
        {
            DateTime now = DateTime.Now;
            await context.RespondAsync($"DEBUG VERSION: {now.TimeOfDay} {now.ToLocalTime().Date}");
        }

        //bool isPlaying, isFirstQueue, isLastQueue, isShuffleQueue;
        [Command("nowplay"), Description("Displays information about currently-played track."), Aliases("npd")]
        public async Task NowPlayingAsync(CommandContext context)
        {
            // shuffle = new DiscordButtonComponent(ButtonStyle.Success, MusicButtonReference.SHUFFLE_ON, "Shuffle On");
            // noShuffle = new DiscordButtonComponent(ButtonStyle.Danger, MusicButtonReference.SHUFFLE_OFF, "Shuffle Off");
            play = new DiscordButtonComponent(ButtonStyle.Secondary, MusicButtonReference.PLAY, "Play");
            pause = new DiscordButtonComponent(ButtonStyle.Secondary, MusicButtonReference.PAUSE, "Pause");
            skip = new DiscordButtonComponent(ButtonStyle.Secondary, MusicButtonReference.SKIP, "Skip");
            stop = new DiscordButtonComponent(ButtonStyle.Secondary, MusicButtonReference.STOP, "Stop");
            playlist = new DiscordButtonComponent(ButtonStyle.Secondary, MusicButtonReference.PLAYLIST, "Playlist");
            var track = this.MusicPlayer.NowPlaying;
            //initButton();
            //initComponents(context);
            component = new DiscordButtonComponent[]
            {
                    play,
                    pause,
                    skip,
                    stop,
                    playlist
            };

            component2 = new DiscordButtonComponent[]
            {
                shuffle,
                noShuffle
            };
            // var testButton = new DiscordButtonComponent(ButtonStyle.Secondary, "playlist", "UwU");
            // await context.RespondAsync("DEBUG");
            // Create the message
            // $"Now playing: {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} [{this.MusicPlayer.GetCurrentPosition().ToDurationString()}/{this.MusicPlayer.NowPlaying.Track.Length.ToDurationString()}] requested by {Formatter.Bold(Formatter.Sanitize(this.MusicPlayer.NowPlaying.RequestedBy.DisplayName))}."
            var builder = new DiscordMessageBuilder();
            
            builder.WithContent("Message")
                   .AddComponents(component)
                   .AddComponents(component2);
            var message = await builder.SendAsync(context.Channel);
            context.Client.ComponentInteractionCreated += async (s, e) =>
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                switch(e.Id)
                {
                    case MusicButtonReference.PLAY:
                        await this.MusicPlayer.ResumeAsync();
                        await context.Channel.SendMessageAsync("Resumed!");
                        //await NewPlay(context, e);
                        break;
                    case MusicButtonReference.PAUSE:
                        await this.MusicPlayer.PauseAsync();
                        await context.Channel.SendMessageAsync("Paused!");
                        //await NewPause(context, e);
                        break;
                    case MusicButtonReference.STOP:
                        await this.MusicPlayer.SkipOrStopAsync();
                        await this.MusicPlayer.DestroyPlayerAsync();
                        //await NewStop(context, e);
                        break;
                    case MusicButtonReference.SKIP:
                        //await NewSkip(context, e);
                        break;
                    case MusicButtonReference.SHUFFLE_ON:
                        //await NewShuffe(context, e, true);
                        break;
                    case MusicButtonReference.SHUFFLE_OFF:
                        //await NewShuffe(context, e, false);
                        break;
                    case MusicButtonReference.PLAYLIST:
                        //await NewPlaylist(context, e);
                        break;
                }
                var newBuilder = new DiscordWebhookBuilder();
                newBuilder.WithContent("OwO")
                          .AddComponents(component)
                          .AddComponents(component2);
                await e.Interaction.EditOriginalResponseAsync(newBuilder);
            };
        }

            

        // private async Task NewPlay(CommandContext context, ComponentInteractionCreateEventArgs e)
        // {
        //     await this.MusicPlayer.ResumeAsync();
        //     await context.Channel.SendMessageAsync("Resumed!");
        //     // await UpdateUi(context, e);
        // }

        // private async Task NewPause(CommandContext context, ComponentInteractionCreateEventArgs e)
        // {
        //     await this.MusicPlayer.PauseAsync();
        //     await context.Channel.SendMessageAsync("Paused!");
        //     // await UpdateUi(context, e);
        // }

        // private async Task NewStop(CommandContext context, ComponentInteractionCreateEventArgs e)
        // {
        //     await this.MusicPlayer.StopAsync();
        //     await this.MusicPlayer.DestroyPlayerAsync();
        //     await context.Channel.SendMessageAsync("Stopped!");
        // }

        // private async Task NewSkip(CommandContext context, ComponentInteractionCreateEventArgs e)
        // {
            
        // }

        // private async Task NewShuffe(CommandContext context, ComponentInteractionCreateEventArgs e, bool v)
        // {
            
        // }

        // private async Task NewPlaylist(CommandContext context, ComponentInteractionCreateEventArgs e)
        // {
            
        // }

        // private async Task UpdateUi(CommandContext context, ComponentInteractionCreateEventArgs eventArgs)
        // {
        //     //todo:
        //     DiscordWebhookBuilder newBuilder = new DiscordWebhookBuilder();
        //     newBuilder.WithContent("Message")
        //            .AddComponents(component)
        //            .AddComponents(component2)
        //            .AddComponents(component3);
        //     var update = await eventArgs.Interaction.EditOriginalResponseAsync(newBuilder);
        //     onClick(context);
        // }

        private async Task<int> NewRepeatMode(CommandContext context, ComponentInteractionCreateEventArgs e, int repeatMode)
        {
            // var rmc = new RepeatModeConverter();
            // if (!rmc.TryFromString(mode, out var rm))
            // {
            //     await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Invalid repeat mode specified.");
            //     return;
            // }

            // this.MusicPlayer.SetRepeatMode(rm);
            // await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Repeat mode set to {rm}.");
            var repeatConverter = new RepeatModeConverter();
            int value = 0;
            switch (repeatMode) 
            {
                case 0:
                    this.MusicPlayer.SetRepeatMode(RepeatMode.All);
                    value = 1;
                    var msg = await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Repeat mode set to ALL"));
                    Action<Task> p = async (task) => await e.Interaction.DeleteFollowupMessageAsync(msg.Id);
                    await Task.Delay(5000).ContinueWith(p);
                    break;
                case 1:
                    this.MusicPlayer.SetRepeatMode(RepeatMode.Single);
                    value = 2;
                    msg = await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Repeat mode set to SINGLE"));
                    p = async (task) => await e.Interaction.DeleteFollowupMessageAsync(msg.Id);
                    await Task.Delay(5000).ContinueWith(p);
                    break;
                case 2:
                    this.MusicPlayer.SetRepeatMode(RepeatMode.None);
                    value = 0;
                    msg = await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Repeat mode set to NONE"));
                    p = async (task) => await e.Interaction.DeleteFollowupMessageAsync(msg.Id);
                    await Task.Delay(5000).ContinueWith(p);
                    break;
            }
            return value; //todo:
        }

    }
}