using Newtonsoft.Json;
using System.Collections.Immutable;

namespace aice_stable.services
{
    public sealed class AiceConfigurationData
    {
        [JsonProperty("version")]
        public AiceConfigurationVersion Version { get; private set; } = new AiceConfigurationVersion();
        /// <summary>
        /// Gets the Discord configuration.
        /// </summary>
        [JsonProperty("discord")]
        public AiceConfigurationDiscord Discord { get; private set; } = new AiceConfigurationDiscord();
        /// <summary>
        /// Gets the Lavalink configuration
        /// </summary>
        [JsonProperty("lavalink")]
        public AiceConfigurationLavalink Lavalink { get; private set; } = new AiceConfigurationLavalink();
        /// <summary>
        /// Gets the Youtube configuration
        /// </summary>
        [JsonProperty("youtube")]
        public AiceConfigYouTube Youtube { get; private set; } = new AiceConfigYouTube();
        /// <summary>
        /// Gets the Redis configuration.
        /// </summary>
        [JsonProperty("redis")]
        public AiceConfigRedis Redis { get; private set; } = new AiceConfigRedis();
    }

    public sealed class AiceConfigurationVersion
    {
        [JsonProperty("config")]
        public string Version { get; private set; } = "2";
    }

    public sealed class AiceConfigRedis
    {
        /// <summary>
        /// Gets the hostname of the Redis server.
        /// </summary>
        [JsonProperty("hostname")]
        public string Hostname { get; private set; } = "localhost";

        /// <summary>
        /// Gets the port used to communicate with Redis server.
        /// Defaults to 6379 when not found.
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; private set; } = 6379;

        /// <summary>
        /// Gets the password used to authenticate with Redis server.
        /// Defaults to null.
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; private set; } = null;

        /// <summary>
        /// Gets whether to use SSL/TLS for communication with Redis server.
        /// Defaults to true.
        /// </summary>
        [JsonProperty("ssl")]
        public bool UseEncryption { get; private set; } = true;
    }
    /// <summary>
    /// Represents Discord section of the configuration file.
    /// </summary>
    public sealed class AiceConfigurationDiscord
    {
        /// <summary>
        /// Gets the token for Discord API.
        /// Defaults to... that.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; private set; } = "NjAzODQzMTkxMzc2MTgzMzA2.XTlSyw.pnfjeAZIKePBufs29mvtOj8UPz8";

        /// <summary>
        /// Gets the default command prefixes of the bot.
        /// Default is !, change-able in config.json
        /// </summary>
        [JsonProperty("prefixes")]
        public ImmutableArray<string> DefaultPrefixes { get; private set; } = new[] { "!" }.ToImmutableArray();

        /// <summary>
        /// Gets whether to enable the user mention prefix for the bot.
        /// Defaults to true.
        /// </summary>
        [JsonProperty("mention_prefix")]
        public bool EnableMentionPrefix { get; private set; } = true;

        /// <summary>
        /// Gets the size of the message cache. 0 means disable.
        /// </summary>
        [JsonProperty("message_cache_size")]
        public int MessageCacheSize { get; private set; } = 512;

        /// <summary>
        /// Gets the total number of shards on which the bot will operate.
        /// </summary>
        [JsonProperty("shards")]
        public int ShardCount { get; private set; } = 1;

        /// <summary>
        /// Gets the game the bot will be playing. Null means disable.
        /// </summary>
        [JsonProperty("game")]
        public string Game { get; private set; } = "!help";
    }
    public sealed class AiceConfigurationLavalink
    {
        /// <summary>
        /// Gets the hostname of the Lavalink node server.
        /// Since the lavalink is running on the same server
        /// as the bot, we can simply use localhost.
        /// </summary>
        [JsonProperty("hostname")]
        public string Hostname { get; private set; } = "localhost";

        /// <summary>
        /// Gets the port of the Lavalink node.
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; private set; } = 2333;

        /// <summary>
        /// Gets the password to Lavalink node.
        /// 
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; private set; }
    }
    public sealed class AiceConfigYouTube
    {
        /// <summary>
        /// Gets the API key for YouTube's data API.
        /// </summary>
        [JsonProperty("api_key")]
        public string ApiKey { get; private set; } = "AIzaSyAR708k_ezGJ2hP-_L1FnxhjiqOBUKRgDQ";
    }

}