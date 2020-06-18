using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace HttpClientTests
{
    class Program
    {        
        // HttpClient is intended to be instantiated once per application, rather than per-use. See Remarks.
        static readonly HttpClient client = new HttpClient();

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
      int dwLogonType, int dwLogonProvider, out SafeAccessTokenHandle phToken);

        static async Task Main()
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                const int LOGON32_PROVIDER_DEFAULT = 0;
                //This parameter causes LogonUser to create a primary token.   
                const int LOGON32_LOGON_INTERACTIVE = 2;

                // Call LogonUser to obtain a handle to an access token.   
                SafeAccessTokenHandle safeAccessTokenHandle;
                bool returnValue = LogonUser("student01", "company", "DescriptionExamTeacher",
                    LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                    out safeAccessTokenHandle);

                if (false == returnValue)
                {
                    int ret = Marshal.GetLastWin32Error();
                    Console.WriteLine("LogonUser failed with error code : {0}", ret);
                    throw new System.ComponentModel.Win32Exception(ret);
                }

                using (safeAccessTokenHandle)
                {
                    // Note: if you want to run as unimpersonated, pass  
                    //       'SafeAccessTokenHandle.InvalidHandle' instead of variable 'safeAccessTokenHandle'  
                    WindowsIdentity.RunImpersonated(
                    safeAccessTokenHandle,
                    // User action  
                    () =>
                    {
                        for (int i = 0; i < 50; i ++)
                        {
                            WebBrowserRun(i);                          
                        }

                        using (var identity = WindowsIdentity.GetCurrent())
                        {
                            Console.WriteLine($"Authentication Type: {identity.AuthenticationType}");
                        }

                        Console.WriteLine($"Impersonated Context: .... {WindowsIdentity.GetCurrent()}");
                    });
                }

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            Console.WriteLine();
            Console.ReadKey();
        }

        private static async Task WebBrowserRun(int idx)
        {
            try
            {
                const string uriSources = "https://piweb01.pischool.int:7443/";
                using (var handler = new HttpClientHandler
                {                    
                    UseDefaultCredentials = true
                })
                {
                    using (var client = new HttpClient(handler))
                    {
                        var result = await client.GetStringAsync(uriSources);
                        Console.WriteLine($"Browser {idx}: message from {uriSources}: " + result);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("From HttpClient: " + ex);
            }
        }
    }
}
