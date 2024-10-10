using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PlaylistConverter
{
    internal class CustomLocalServerCodeReceiver : ICodeReceiver
    {
        private readonly int _port;
        private readonly string _redirectUri;

        public CustomLocalServerCodeReceiver(int port)
        {
            _port = port;
            _redirectUri = $"http://127.0.0.1:{_port}/authorize/";
        }

        public string RedirectUri => _redirectUri;

        public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(
            AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken)
        {
            using var listener = new HttpListener();
            // listener
            listener.Prefixes.Add(_redirectUri);
            listener.Start();

            string authorizationUrl = url.Build().ToString();

            // Correctly start the URL in the default web browser
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = authorizationUrl,
                UseShellExecute = true // Ensure it's set to true
            };
            System.Diagnostics.Process.Start(processStartInfo);

            // Wait for the authorization code
            HttpListenerContext context = await listener.GetContextAsync();
            string? code = context.Request.QueryString["code"];

            // Optionally handle errors from the authorization response
            string? error = context.Request.QueryString["error"];
            if (!string.IsNullOrEmpty(error))
            {
                // Log the error or throw an exception
                throw new Exception($"Authorization failed: {error}");
            }

            // Respond to the client to acknowledge the code reception
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            using (var responseWriter = new StreamWriter(context.Response.OutputStream))
            {
                await responseWriter.WriteAsync("<html><body>Authorization successful! You can close this window.</body></html>");
            }

            listener.Stop();

            return new AuthorizationCodeResponseUrl
            {
                Code = code,
                Error = error
            };
        }
    }
}
