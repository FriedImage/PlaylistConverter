using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Net;

namespace PlaylistConverter
{
    internal class LocalServer
    {
        private static string? _authCode = null;

        public static async Task<string> StartServer(int port)
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:{port}")
                .Configure(app => app.Run(async context =>
                {
                    if (context.Request.Query.ContainsKey("code"))
                    {
                        _authCode = context.Request.Query["code"];
                        await context.Response.WriteAsync("Authorization successful! You can close this window.");
                    }
                    else
                    {
                        await context.Response.WriteAsync("Authorization failed or canceled.");
                    }
                }));

            var host = hostBuilder.Build();
            var hostTask = host.RunAsync();

            while (_authCode == null)
            {
                await Task.Delay(100); // Wait until the code is received
            }

            return _authCode;
        }
    }
}
