using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportServer.Extensibility.Models
{
    public enum ReportMode
    {
        Sample,
        Episode
    }

    public enum ReportAction
    {
        None,
        Preview,
        Export,
        Print
    }
}
