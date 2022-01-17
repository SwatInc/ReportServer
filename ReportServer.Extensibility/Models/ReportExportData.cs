using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportServer.Extensibility.Models
{
    public class ReportExportDataModel
    {
        public string SampledSite { get; set; }
        public string Nidpp { get; set; }
        public string EpisodeNumber { get; set; }
        public string PatientName { get; set; }
    }
}
