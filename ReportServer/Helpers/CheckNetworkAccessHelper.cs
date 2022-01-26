using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportServer.Helpers
{
    public static class CheckNetworkAccessHelper
    {
        public static bool QuickBestGuessAboutAccessibilityOfNetworkPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return false;
                var pathRoot = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(pathRoot)) return false;
                var processInfo = new ProcessStartInfo("net", "use")
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var output="";
                using (var p = Process.Start(processInfo))
                {
                    if (p != null) output = p.StandardOutput.ReadToEnd();
                }

                return output.Split('\n')
                    .Any(line => line.Contains(pathRoot) && line.StartsWith("OK"));
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
