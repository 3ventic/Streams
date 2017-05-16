using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Streams.Http.Models.Twitch
{
    class Stream
    {
        [JsonProperty("_id")] public string Id { get; set; }
        [JsonProperty("game")] public string Game { get; set; }
        [JsonProperty("community_id")] public string Community { get; set; }
        [JsonProperty("viewers")] public int Viewers { get; set; }
        [JsonProperty("video_height")] public int VideoHeight { get; set; }
        [JsonProperty("average_fps")] public double FramesPerSecond { get; set; }
        [JsonProperty("delay")] public int Delay { get; set; }
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }
        [JsonProperty("preview")] public Dictionary<string, string> Previews { get; set; }
        [JsonProperty("channel")] public Channel Channel { get; set; }
    }
}
