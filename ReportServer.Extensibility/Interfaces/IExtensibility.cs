using DevExpress.XtraReports.UI;
using ReportServer.Extensibility.Models;
using System;

namespace ReportServer.Extensibility.Interfaces
{
    public interface IExtensibility
    {
        event EventHandler<ReportServerNotificationModel> OnPopupMessageRequired;
        event EventHandler<XtraReport> OnReportExportRequest;
        event EventHandler<XtraReport> OnReportPreviewRequest;

        string ReportName { get;}
        Type GetModelType();
        void SetReportId(int reportId);
        void Print(string jsonData, string printerName, ReportMode reportMode, ReportAction reportAction = ReportAction.Print);
    }
}
