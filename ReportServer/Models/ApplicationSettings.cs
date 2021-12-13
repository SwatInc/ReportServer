using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportServer.Models
{
    public class ApplicationSettings
    {
        public string ApplicationName { get; set; }
        public string IncomingDirectory { get; set; }
        public int PolFrequencyInSec { get; set; }
        public string ControlExtension { get; set; }
        public string ReportExtension { get; set; }
    }
}
