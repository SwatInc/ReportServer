using System;

namespace ReportServer.Extensibility.Interfaces
{
    public interface IExtensibility
    {
        string ReportName { get;}
        Type GetModelType();
        void Print(string jsonData, string printerName);
    }
}
