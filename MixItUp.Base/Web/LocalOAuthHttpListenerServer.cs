using MixItUp.Base.Services;
using MixItUp.Base.Util;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Web
{
    /// <summary>
    /// An Http listening server for processing OAuth requests.
    /// </summary>
    public class LocalOAuthHttpListenerServer : LocalHttpListenerServer
    {
        public const string REDIRECT_URL = "http://localhost:8919/";

        public const string AUTHORIZATION_CODE_URL_PARAMETER = "code";

        public const string LOGIN_REDIRECT_HTML = @"<!DOCTYPE html>
                <html>
                <head>
                <meta charset=""utf-8"">
                <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
                <title>Mix It Up - Logged In</title>
                <link rel=""shortcut icon"" href=""https://github.com/SaviorXTanren/mixer-mixitup/raw/master/Branding/MixItUp-Logo-Base-WhiteXS.png"">
                <style>
                *{margin:0;padding:0;box-sizing:border-box}
                body{font-family:system-ui,sans-serif;background:radial-gradient(circle at 125% 125%,#9b305e 0%,#12053a 85%);background-attachment:fixed;height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center;overflow:hidden}
                .logo{width:80px;height:80px;margin-bottom:1.5rem;opacity:0;animation:fadeIn .8s .2s forwards}
                .card{background:rgba(255,255,255,.1);backdrop-filter:blur(20px);border:1px solid rgba(255,255,255,.2);border-radius:16px;padding:2.5rem 2rem;text-align:center;box-shadow:0 8px 32px rgba(0,0,0,.3);max-width:400px;width:90%;opacity:0;animation:fadeIn .8s .4s forwards}
                .title{color:#fff;font-size:2rem;font-weight:600;margin-bottom:.5rem}
                .subtitle{color:#e2e8f0;font-size:1.25rem;margin-bottom:1.5rem}
                .message{color:#cbd5e1;font-size:.95rem;line-height:1.5}
                @keyframes fadeIn{to{opacity:1}}
                @media(max-width:640px){.card{padding:2rem 1.5rem}.title{font-size:1.75rem}.logo{width:60px;height:60px}}
                </style>
                </head>
                <body>
                <div class=""card"">
                <img src=""https://static.mixitupapp.com/desktop/Mix-It-Up_Logo_Auth-Callback.png"" alt=""Mix It Up Logo"" class=""logo"">
                <h1 class=""title"">Mix It Up</h1>
                <h2 class=""subtitle"">Logged In Successfully</h2>

                <p class=""message"">You have been logged in successfully. You may now close this webpage.</p>
                </div>
                </body>
                </html>";

        private string authorizationCode;

        public LocalOAuthHttpListenerServer() { }

        public async Task<string> GetAuthorizationCode(string authorizationURL, int secondsToWait)
        {
            try
            {
                if (this.Start(REDIRECT_URL))
                {
                    ServiceManager.Get<IProcessService>().LaunchLink(authorizationURL);

                    for (int i = 0; i < secondsToWait; i++)
                    {
                        if (!string.IsNullOrEmpty(this.authorizationCode))
                        {
                            break;
                        }
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            this.Stop();

            return this.authorizationCode;
        }

        public async Task<Result<string>> GetAuthorizationCode(string authorizationURL, CancellationToken cancellationToken)
        {
            try
            {
                if (this.Start(REDIRECT_URL))
                {
                    ServiceManager.Get<IProcessService>().LaunchLink(authorizationURL);

                    while (!cancellationToken.IsCancellationRequested && string.IsNullOrWhiteSpace(this.authorizationCode))
                    {
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    return new Result<string>(success: false, message: Resources.UnableToStartAuthenticationSession);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            this.Stop();

            return new Result<string>(success: !string.IsNullOrWhiteSpace(this.authorizationCode), message: null)
            {
                Value = this.authorizationCode
            };
        }

        /// <summary>
        /// Processes an http request.
        /// </summary>
        /// <param name="listenerContext">The context of the request</param>
        /// <returns>An awaitable task to process the request</returns>
        protected override async Task ProcessConnection(HttpListenerContext listenerContext)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            string result = string.Empty;

            string token = this.GetRequestParameter(listenerContext, AUTHORIZATION_CODE_URL_PARAMETER);
            if (!string.IsNullOrEmpty(token))
            {
                statusCode = HttpStatusCode.OK;
                result = LOGIN_REDIRECT_HTML;

                this.authorizationCode = token;
            }

            await this.CloseConnection(listenerContext, statusCode, result);
        }
    }
}