using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Owin;

namespace ContinousStreamingJson
{
    class Program
    {
        private const string baseAddress = "http://localhost:667/";
        private static HttpSelfHostConfiguration config;

        static void Main(string[] args)
        {
            config = new HttpSelfHostConfiguration(baseAddress);
            config.Routes.MapHttpRoute(name: "DefaultApi", routeTemplate: "{controller}");

            using (var server = WebApp.Start(new StartOptions(baseAddress), SetupOwin))
            {
                var client = new HttpClient();

                var response = client.GetAsync(baseAddress + "stream", HttpCompletionOption.ResponseHeadersRead).Result;

                using (var stream = response.Content.ReadAsStreamAsync().Result)
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        Console.WriteLine(reader.ReadLine());
                    }
                }

            }
        }

        private static void SetupOwin(IAppBuilder app)
        {
            app.UseWebApi(config);
        }
    }

    public class StreamController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Get()
        {
            var response = Request.CreateResponse(HttpStatusCode.OK);

            response.Content = new PushStreamContent(this.PushContent, "text/event-stream");
            
            return response;
        }

        private async Task PushContent(Stream stream, HttpContent content, TransportContext context)
        {
            var i = 0;
            using (stream)
            using (var writer = new StreamWriter(stream))
            {
                while (i < 10)
                {
                    await Task.Delay(500);

                    var newEntry = new ProgressEntry()
                    {
                        Processed = i++,
                        Data = new [] { new UserData { Name = i.ToString() } }
                    };

                    await writer.WriteLineAsync("data: " + JsonConvert.SerializeObject(newEntry) + "\n");
                    await writer.FlushAsync();
                }
            }
        }
    }

    internal sealed class ProgressEntry
    {
        public int Processed { get; set; }

        public UserData[] Data { get; set; }
    }

    internal sealed class UserData
    {
        public string Name { get; set; }
    }
}
