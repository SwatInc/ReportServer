using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using System;
using System.Collections.Generic;

namespace ReportB
{
    public class SomethingElse : IExtensibility
    {
        public SomethingElse()
        {
            ReportName = "ReportB";
        }
        public string ReportName { get; private set; }

        public Type GetModelType()
        {
            return typeof(List<TestModel>);
        }

        public void Print(string jsonData, string printerName)
        {
            //var report = new CrystalReportB();
            var data = JsonConvert.DeserializeObject<ReportSchema>(jsonData);
            //report.Database.Tables[0].SetDataSource(data.ReportData);
            //report.PrintToPrinter(1, false, 0, 0);
        }
    }
}
