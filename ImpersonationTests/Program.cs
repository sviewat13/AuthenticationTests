namespace AsyncConsole
{
    using Microsoft.Win32.SafeHandles;
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class Program
    {
        #region Private implementation
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
       int dwLogonType, int dwLogonProvider, out SafeAccessTokenHandle phToken);

        private static void Main()
        {
            Console.WriteLine(Thread.CurrentPrincipal.Identity.Name);
            Program.RunAsync()
                   .GetAwaiter()
                   .GetResult();
        }

        private static async Task RunAsync()
        {
            for (; ; )
            {
                TryCatchGetCuurent("(1)", "before first yield");

                await Task.Yield();

                TryCatchGetCuurent("(2)", "after first yield");

                using (var identity = WindowsIdentity.GetCurrent())
                using (identity.Impersonate())
                {
                    TryCatchGetCuurent("(3)", "before second yield");
                    await Task.Yield();
                }

                TryCatchGetCuurent("(4)", "afer second yield");

                await Task.Delay(5000);

                const int LOGON32_PROVIDER_DEFAULT = 0;
                //This parameter causes LogonUser to create a primary token.   
                const int LOGON32_LOGON_INTERACTIVE = 2;

                // Call LogonUser to obtain a handle to an access token.   
                SafeAccessTokenHandle safeAccessTokenHandle;
                bool returnValue = LogonUser("student01","company", "DescriptionExamTeacher",
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
                        // Check the identity.  
                        Console.WriteLine($"* {Thread.CurrentThread.ManagedThreadId}: During impersonation: " + WindowsIdentity.GetCurrent().Name);
                        Task.Run(() => {
                            Console.WriteLine($" ** Inner task - {Thread.CurrentThread.ManagedThreadId}: During impersonation: " + WindowsIdentity.GetCurrent().Name);
                            for(int i = 0; i < 50; i++)
                                SubFunction(i);
                        });
                    }
                    );
                }

                // Check the identity again.  
                Console.WriteLine("After impersonation: " + WindowsIdentity.GetCurrent().Name);
            }
        }

        private static void SubFunction(int i )
        {
            Task.Run(() =>
                Console.WriteLine($" *** Inner task 2 #{i}- {Thread.CurrentThread.ManagedThreadId}: During impersonation: " + WindowsIdentity.GetCurrent().Name));
        }

        private static void TryCatchGetCuurent(string prefix, string suffix)
        {
            try
            {
                using (var threadIdentity = WindowsIdentity.GetCurrent(true))
                {
                    Console.WriteLine($"Name: {threadIdentity.Name}");
                    if (threadIdentity != null)
                    {
                        throw new InvalidOperationException($"{prefix} {Thread.CurrentThread.ManagedThreadId} must not be impersonating {threadIdentity.Name} in {suffix}");
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion
    }
}