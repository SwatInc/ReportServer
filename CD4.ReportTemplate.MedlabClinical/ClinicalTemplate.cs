using ReportServer.Extensibility.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CD4.ReportTemplate.MedlabClinical
{
    public class ClinicalTemplate : IExtensibility
    {
        public string ReportName => throw new NotImplementedException();

        public Type GetModelType()
        {
            throw new NotImplementedException();
        }

        public void Print(string jsonData, string printerName)
        {
            throw new NotImplementedException();
        }
    }
}
