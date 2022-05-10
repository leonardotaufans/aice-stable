using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using aice_stable.services;
using System.Net;

namespace aice_stable.services
{
    public sealed class YoutubeSearchProvider
    {
        private string ApiKey { get; }
        private HttpClient Http { get; }

        /// <summary>
        /// Creates a new YouTube search provider service instance.
        /// </summary>
        /// <param name="cfg">Configuration of this service.</param>
        public YoutubeSearchProvider(AiceConfigYouTube cfg)
        {
            this.ApiKey = cfg.ApiKey;
            this.Http = new HttpClient()
            {
                BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/search")
            };
            this.Http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Companion-Cube");
        }

        /// <summary>
        /// Performs a YouTube search and returns the results.
        /// </summary>
        /// <param name="term">What to search for.</param>
        /// <returns>A collection of search results.</returns>
        public async Task<IEnumerable<YouTubeSearchResult>> SearchAsync(string term)
        {
            var uri = new Uri($"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=5&type=video&fields=items(id(videoId),snippet(title,channelTitle))&key={this.ApiKey}&q={WebUtility.UrlEncode(term)}");

            var json = "{}";
            using (var req = await this.Http.GetAsync(uri))
            using (var res = await req.Content.ReadAsStreamAsync())
            using (var sr = new StreamReader(res, AiceUtilities.UTF8))
                json = await sr.ReadToEndAsync();

            var jsonData = JObject.Parse(json);
            var data = jsonData["items"].ToObject<IEnumerable<YouTubeApiResponseItem>>();

            return data.Select(x => new YouTubeSearchResult(x.Snippet.Title, x.Snippet.Author, x.Id.VideoId));
        }
    }
}
