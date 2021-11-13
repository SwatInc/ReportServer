using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using System;
using System.Collections.Generic;

namespace ReportA
{
    public class Something : IExtensibility
    {
        public Something()
        {
            ReportName = "ReportA";
        }
        public string ReportName { get; private set; }

        public Type GetModelType()
        {
            return typeof(List<TestModel>);
        }

        public void Print(string jsonData, string printerName)
        {
            //var report = new CrystalReport1();
            var data = JsonConvert.DeserializeObject<ReportSchema>(jsonData);
            //report.Database.Tables[0].SetDataSource(data.ReportData);
            //report.PrintToPrinter(1, false, 0, 0);
        }
    }

    public class TestModel
    {
        public int id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string gender { get; set; }
        public string ip_address { get; set; }
        public double Amount { get; set; }
    }

    public class ReportSchema
    {
        public string TemplateName { get; set; }
        public List<TestModel> ReportData { get; set; }
    }
}
