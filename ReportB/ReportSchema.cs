using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportB
{

    public class ReportSchema
    {
        public string TemplateName { get; set; }
        public List<TestModel> ReportData { get; set; }
    }
}
