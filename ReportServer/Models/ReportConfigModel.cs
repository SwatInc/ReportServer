using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportServer.Models
{
    public class ReportConfigModel
    {
        [JsonProperty("ReportName")]
        public string ReportName { get; set; }

        [JsonProperty("ReportDatabaseId")]
        public int Id { get; set; }

    }
}
