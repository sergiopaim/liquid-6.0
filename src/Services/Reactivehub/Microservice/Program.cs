using Liquid;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Microservice
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new("pt");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new("pt");

            var host = CreateWebHostBuilder(args).Build();

            if (host.ProcessCommands(args, isReactiveHub: true))
                host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
