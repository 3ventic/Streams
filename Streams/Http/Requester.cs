using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Streams.Http
{
    class Requester : IDisposable
    {
        HttpClient client;

        public Requester(Uri baseUrl)
        {
            client = new HttpClient();
            client.BaseAddress = baseUrl;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.Add("Client-Id", "ve1quqhn3hpbs1ntgfgt2zc4mxojjp");
        }

        public void Dispose() => client.Dispose();

        public async Task<T> GetObjectAsync<T>(string resource) => JsonConvert.DeserializeObject<T>(await client.GetStringAsync(resource));
    }
}
