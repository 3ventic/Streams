using Newtonsoft.Json;
using System;

namespace Streams.Http.Models.Twitch
{
    public class Channel
    {
        [JsonProperty("mature")] public bool Mature { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("broadcaster_language")] public string BroadcastLanguage { get; set; }
        [JsonProperty("display_name")] public string DisplayName { get; set; }
        [JsonProperty("game")] public string Game { get; set; }
        [JsonProperty("language")] public string Language { get; set; }
        [JsonProperty("_id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")] public DateTime UpdatedAt { get; set; }
        [JsonProperty("logo")] public string Logo { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("views")] public long Views { get; set; }
        [JsonProperty("followers")] public long Followers { get; set; }
        [JsonProperty("broadcaster_type")] public string Type { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
    }
}