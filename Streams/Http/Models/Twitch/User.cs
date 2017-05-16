using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Streams.Http.Models.Twitch
{
    class User
    {
        [JsonProperty("display_name")] public string DisplayName { get; set; }
        [JsonProperty("_id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("logo")] public string Logo { get; set; }
    }
}
