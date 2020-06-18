using Owin;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Host.HttpListener;
using System.Security.Principal;

namespace OwinTest
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var listener =
                (System.Net.HttpListener)app.Properties["System.Net.HttpListener"];
            listener.AuthenticationSchemes = AuthenticationSchemes.IntegratedWindowsAuthentication;

            app.Run(context =>
            {
                var req = string.Empty;
                var identity = (WindowsIdentity)context.Request.User.Identity;
                TokenImpersonationLevel innerLevel = TokenImpersonationLevel.None;
                var  authType = string.Empty;
                bool isAuth = false;
                string handle = string.Empty;
                using (var clonedIdentity = (IDisposable)identity.Clone())
                {
                    Console.WriteLine($"Outside Impersonation: {WindowsIdentity.GetCurrent().Name}");
                    using (var impersonationContext = (clonedIdentity as WindowsIdentity).Impersonate())
                    {
                        using (var id = WindowsIdentity.GetCurrent())
                        {
                            req = id.Name;
                            handle = id.AccessToken.ToString();
                            Console.WriteLine($"Inside Impersonation: {id.Name}");
                            Console.WriteLine($"Inside Impersonation Level:  {innerLevel = id.ImpersonationLevel}");
                            Console.WriteLine($"Inside Authentication Type:  {authType = id.AuthenticationType}");
                            Console.WriteLine($"Inside IsAuthenticated:  {isAuth = id.IsAuthenticated}");
                        }                         
                    }
                }
                context.Response.ContentType = "text/plain";
                Console.WriteLine($"{req}");
                return context.Response.WriteAsync($"Hello {req}! Authenticated: {isAuth}, Inner Level: {innerLevel}, Auth Type: {authType}!");
            });
        }
    }
}
