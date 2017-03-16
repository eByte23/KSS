using CloudFlareUtilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace KSS
{
    public static class CloudFlareHandlerFactory
    {
        public static HttpClient Build(Settings settings)
        {
            var handler = new ClearanceHandler { };
            var post = new FormUrlEncodedContent(new[]
              {
                    new KeyValuePair<string, string>("username", settings.Username),
                    new KeyValuePair<string, string>("password", settings.Password)
                });

            var client = new HttpClient(handler);
            var result1 = client.GetAsync(settings.BaseUrl + "/Login").Result;
            var result = client.PostAsync(settings.BaseUrl + "/Login", post).Result;
            return client;
        }
    }
}
