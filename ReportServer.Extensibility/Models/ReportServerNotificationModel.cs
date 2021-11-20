using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReportServer.Extensibility.Models
{
    public class ReportServerNotificationModel
    {
        public ToolTipIcon NotifyIcon { get; set; }
        public string Message { get; set; }
    }
}
