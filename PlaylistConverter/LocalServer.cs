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
        private static IWebHost? _host = null;

        public static async Task<string> StartServer(int port)
        {
            _authCode = null; // Reset the authorization code on every new attempt

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

            _host = hostBuilder.Build();
            var hostTask = _host.RunAsync(); // Start the host

            while (_authCode == null)
            {
                await Task.Delay(100); // Wait until the code is received
            }

            await StopServer(); // Shut down the server once the code is received

            return _authCode;
        }

        public static async Task StopServer()
        {
            if (_host != null)
            {
                await _host.StopAsync(); // Ensure server stops after the auth code is received
                _host.Dispose();
                _host = null;
            }
        }
    }
}
