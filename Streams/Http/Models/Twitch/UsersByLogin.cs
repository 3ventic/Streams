using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Streams.Http.Models.Twitch
{
    class UsersByLogin
    {
        [JsonProperty("users")] public User[] Users { get; set; }
    }
}
