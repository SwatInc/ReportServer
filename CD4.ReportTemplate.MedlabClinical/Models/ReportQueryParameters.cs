using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CD4.ReportTemplate.MedlabClinical.Models
{
    public class ReportQueryParameters
    {
        public string TemplateName { get; set; }
        public string EpisodeNumber { get; set; }
        public string Sid { get; set; }
        public int LoggedInUserId { get; set; }
    }
}
