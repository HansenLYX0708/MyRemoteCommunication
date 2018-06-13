using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using NLog;
using Defines;

namespace Hitachi.Tester
{
    public class Funcs
    {
        public static void AnyUnhandledExceptionHandler(object sender, Exception e, NLog.Logger nlogger)
        {
            if (e is AbandonedMutexException)
            {
                MessageBox.Show("Mutex exception");
                Application.Exit();
            }

            // Get IP address
            StringBuilder ipAddrs = new StringBuilder();
            Regex rxCheckIpAddr = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$");

            foreach (NetworkInterface iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                   iface.OperationalStatus != OperationalStatus.Up) continue;
                foreach (IPAddressInformation ipInfo in iface.GetIPProperties().UnicastAddresses)
                {
                    string ipAddr = ipInfo.Address.ToString();
                    if (rxCheckIpAddr.IsMatch(ipAddr)) ipAddrs.Append(ipAddr).Append(", ");
                }
            }

            // Write to log
            nlogger.FatalException(
                string.Format("Unhandled exception.  IP:{0}", ipAddrs.ToString()), e);

            // Show dialog
            for (Exception innerEx = e.InnerException; innerEx != null; innerEx = innerEx.InnerException)
                nlogger.FatalException("Inner exception", innerEx);

            List<string> messageLines = new List<string>();
            messageLines.Add("Unexpected exception.  Please ask expert with information below.");
            messageLines.Add(" - Tester number");
            messageLines.Add(string.Format(" - Jade version {0}", Application.ProductVersion));
            messageLines.Add(string.Format(" - IP address: {0}", ipAddrs.ToString()));
            messageLines.Add(DateTime.Now.ToString(@" - Ti\me: yyyy/MM/dd HH:mm:ss"));
            messageLines.Add(string.Format(@" - Log file in {0}\logs", System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)));
            messageLines.Add("");
            messageLines.Add(e.Message);
            messageLines.Add(e.StackTrace);
            if (e is TargetInvocationException && e.InnerException != null)
            {
                messageLines.Add("");
                messageLines.Add("Inner exception:");
                messageLines.Add(e.InnerException.Message);
            }
            string message = string.Join(Environment.NewLine, messageLines.ToArray());

            /*
            LogWriter.Enable = true;
            LogWriter.WriteToLogFile(message);
            LogWriter.WriteToLogFile(e.StackTrace);
            */
            nlogger.FatalException("Unhandled exception", e);

            //DialogResult result = MessageBox.Show(
            //     message + e.StackTrace, "Unhandled Error.",
            //     MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Exclamation);

            MessageBoxWith mb = new MessageBoxWith(message + e.StackTrace, "Unhandled Error.");
            DialogResult result = mb.ShowDialog();
            switch (result)
            {
                case DialogResult.Abort:
                    nlogger.Fatal("Abort");
                    Application.Exit();
                    break;

                case DialogResult.Retry:
                    nlogger.Fatal("Retry");
                    Application.Exit();
                    break;

                case DialogResult.Ignore:
                    nlogger.Fatal("Ignore");
                    Application.Exit();
                    break;
            }
        }
    }
}
