using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportServer.Extensibility.Interfaces
{
    public interface IExtensibility
    {
        string ReportName { get;}
        Type GetModelType();
        void Print(string jsonData, string printerName);
    }
}
