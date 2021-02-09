using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TekTox.Bot
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://0.0.0.0:6000");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
