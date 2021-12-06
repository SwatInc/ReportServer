using ReportServer.Extensibility.Models;
using System;

namespace ReportServer.Extensibility.Interfaces
{
    public interface IExtensibility
    {
        event EventHandler<ReportServerNotificationModel> OnPopupMessageRequired;
        string ReportName { get;}
        Type GetModelType();
        void Print(string jsonData, string printerName, ReportMode reportMode);
    }
}
