using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwinTest
{
    class Program
    {      
        static void Main(string[] args)
        {
            var httpsPort = Convert.ToInt32(ConfigurationManager.AppSettings["httpsport"]);
            // BindCertificateToHttpsPort();
            using (WebApp.Start<Startup>($"https://+:{httpsPort}"))
            {
                Console.WriteLine($"Starting https listener on {httpsPort}");
                Console.WriteLine("Press Enter to quit.");
                Console.ReadKey();
            }

            //Console.WriteLine($"Starting https listener on {httpsPort}");

            //// Start listening on both the http and https ports
            //var httpDisposable = WebApp.Start<OwinStartup>(this.httpStartOptions);
            //var httpsDisposable = WebApp.Start<OwinHttpsStartup>(this.httpsStartOptions);

            //// Return both as an IDisposable collection, so the listeners can be disposed when the application or service ends.
            //disposableList.AddRange(new List<IDisposable>() { httpDisposable, httpsDisposable });
            //return new DisposableList(disposableList);
        }

        private static int BindCertificateToHttpsPort()
        {
            var httpsPort = Convert.ToInt32(ConfigurationManager.AppSettings["httpsport"]);
            var thumbprint = ConfigurationManager.AppSettings["thumbprint"];
            var appId = ConfigurationManager.AppSettings["appid"];
            BindCertificateToPort(thumbprint, httpsPort, appId);
            return httpsPort;
        }

        /// <summary>
        /// Binds a certificate to a port using netsh, and saves the port number and certificate thumbprint. Binds to 0.0.0.0:{port}.
        /// Tries to unbind from the previous port
        /// </summary>
        /// <param name="certificateThumbPrint"></param>
        /// <param name="port"></param>
        /// <param name="unbindPrevious"></param>
        public static void BindCertificateToPort(string certificateThumbPrint, int port, string appId, bool unbindPrevious = true)
        {
            //var guid = Guid.NewGuid().ToString("B");
            try
            {
                UnbindCertificateFromPort(port);
                Console.WriteLine($"Succesfully unbound certificate from previous port ({port})");
            }
            catch (Exception)
            {
                Console.WriteLine($"Unable to unbind certificate from previous port ({port}).\n" +
                    $"This is most likely not an issue, especially if you never changed https hosting ports before. \n" +
                    $"Continuing to bind to new port ({port})");
            }

            // Removing all spaces. The thumbprint will have spaces when copied over directly from the Certificate Manager in windows, but
            // netsh doesn't like this
            certificateThumbPrint = certificateThumbPrint.Replace(" ", string.Empty);

            // netsh arguments: https://technet.microsoft.com/en-us/library/cc725882(v=ws.10).aspx#BKMK_2
            // appid as {00000000-0000-0000-0000-000000000000}
            string arguments =
                $"http add sslcert ipport=0.0.0.0:{port} certhash={certificateThumbPrint} appid={appId}";

            // Process start info. make sure it doesn't create a window and redirects output
            var processStartInfo = new ProcessStartInfo("netsh", arguments)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,

            };
            // Start the process and make sure we know when it exits by enabling EnableRaisingEvents
            var process = new Process() { StartInfo = processStartInfo, EnableRaisingEvents = true, };

            // When it exists, make sure that we redirect the standard output to the console, and that we
            // check for an error exit code (everything > 0). When there is an error, throw an exception so that 
            // we can notify the user and prevent that we save the port and thumbprint in app.config.
            process.Exited += (sender, args) =>
            {
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                if (process.ExitCode > 0)
                {
                    Console.WriteLine(process.StandardError.ReadToEnd());
                    throw new Exception($"Error while binding SSL certificates to port {port}");
                }
            };

            // Start the process and make sure to wait for it to exit
            process.Start();
            process.WaitForExit();
        }


        /// <summary>
        /// Unbinds the certificate from the given port using netsh. Unbinds 0.0.0.0:{port}
        /// </summary>
        /// <param name="port">The port for which the certificate needs to be removed</param>
        public static void UnbindCertificateFromPort(int port)
        {
            // netsh arguments: https://technet.microsoft.com/en-us/library/cc725882(v=ws.10).aspx#BKMK_2
            string arguments =
                $"http delete sslcert ipport=0.0.0.0:{port}";

            // Process start info. make sure it doesn't create a window and redirects output
            var processStartInfo = new ProcessStartInfo("netsh", arguments)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,

            };
            // Start the process and make sure we know when it exits by enabling EnableRaisingEvents
            var process = new Process() { StartInfo = processStartInfo, EnableRaisingEvents = true, };

            process.Exited += (sender, args) =>
            {
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                if (process.ExitCode > 0)
                {
                    Console.WriteLine(process.StandardError.ReadToEnd());
                    throw new Exception($"Error while unbinding SSL certificates to port {port}");
                }
            };

            // Start the process and make sure to wait for it to exit
            process.Start();
            process.WaitForExit();
        }

        public static void AddUrlAclFromPort(int port)
        {
            string arguments =
                $"http add urlacl url=https://+:{port}/ user=Everyone";

            // Process start info. make sure it doesn't create a window and redirects output
            var processStartInfo = new ProcessStartInfo("netsh", arguments)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,

            };
            // Start the process and make sure we know when it exits by enabling EnableRaisingEvents
            var process = new Process() { StartInfo = processStartInfo, EnableRaisingEvents = true, };

            process.Exited += (sender, args) =>
            {
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                if (process.ExitCode > 0)
                {
                    Console.WriteLine(process.StandardError.ReadToEnd());
                    throw new Exception($"Error while adding URL ACL to port {port}");
                }
            };

            // Start the process and make sure to wait for it to exit
            process.Start();
            process.WaitForExit();
        }
    }
}
