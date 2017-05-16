using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Streams.Http.Models.Twitch
{
    class Streams
    {
        [JsonProperty("_total")] public int Total { get; set; }
        [JsonProperty("streams")] public Stream[] StreamList { get; set; }
    }
}
