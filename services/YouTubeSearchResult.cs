using Newtonsoft.Json;

namespace aice_stable.services
{
    public struct YouTubeSearchResult
    {
        /// <summary>
        /// Gets the title of this item.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the name of the item's author.
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// Gets the item's ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Creates a new YouTube search result with specified parameters.
        /// </summary>
        /// <param name="title">Title of the item.</param>
        /// <param name="author">Item's author.</param>
        /// <param name="id">Item's ID.</param>
        public YouTubeSearchResult(string title, string author, string id)
        {
            this.Title = title;
            this.Author = author;
            this.Id = id;
        }
    }

    internal struct YouTubeApiResponseItem
    {
        [JsonProperty("id")]
        public ResponseId Id { get; private set; }

        [JsonProperty("snippet")]
        public ResponseSnippet Snippet { get; private set; }


        public struct ResponseId
        {
            [JsonProperty("videoId")]
            public string VideoId { get; private set; }
        }

        public struct ResponseSnippet
        {
            [JsonProperty("title")]
            public string Title { get; private set; }

            [JsonProperty("channelTitle")]
            public string Author { get; private set; }
        }
    }
}