using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.IO;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Emzi0767;
using aice_stable.services;
using aice_stable.Services;
using System.Collections.Generic;
using Emzi0767.Utilities;

namespace aice_stable
{
    public sealed class Program
    {
        private static Dictionary<int, Aice> Shards { get; set; }
        public int ShardId { get; }
        /// <summary>
        /// Main Executor
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            ///Start the Main function using async
            var async = new AsyncExecutor();
            async.Execute(MainAsync(args));
        }

        /// <summary>
        /// Task MainAsync where everything happens.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static async Task MainAsync(string[] args)
        {
            /// Loads the configuration file from current working directory
            var cfgFile = new FileInfo($"{Directory.GetCurrentDirectory()}/config.json");
            /// Use AiceConfigurationHelper to transform the configuration file to processed data
            var cfgLoader = new AiceConfigurationHelper();
            var cfg = await cfgLoader.LoadConfigurationAsync(cfgFile);

            /// Used to make several shards of this bot. The shard is currently set to 1
            Shards = new Dictionary<int, Aice>();
            /// Start an async function of shard starter
            var async = new AsyncExecutor();
            for (int i = 0; i < cfg.Discord.ShardCount; i++)
            {
                var shard = new Aice(cfg, i, async);
                await shard.StartAsync();
                Shards[i] = shard;
            }
            /// To make sure the program will always running
            await Task.Delay(-1);
        }
    }
}
