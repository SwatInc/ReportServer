using CD4.DataLibrary.DataAccess;
using CD4.Entensibility.ReportingFramework.Models;
using CD4.ReportTemplate.DrugOfAbuseTemplate.Models;
using DevExpress.XtraReports.UI;
using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using ReportServer.Extensibility.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CD4.ReportTemplate.DrugOfAbuseTemplate
{
    public class DoATemplateHandler : IExtensibility
    {
        private string _printerName;
        private ReportMode _reportMode;
        private ReportAction _reportAction;

        private event EventHandler<ReportQueryParameters> GetReportData;
        public event EventHandler<ReportServerNotificationModel> OnPopupMessageRequired;
        public event EventHandler<XtraReport> OnReportExportRequest;
        public event EventHandler<XtraReport> OnReportPreviewRequest;

        public string ReportName { get; set; }

        public DoATemplateHandler()
        {
            ReportName = "Medlab.DoATemplate";
            GetReportData += OnGetReportDataAsync;
        }

        private async void OnGetReportDataAsync(object sender, ReportQueryParameters e)
        {
            try
            {
                var reportDataAccess = new ReportsDataAccess();
                List<DataLibrary.Models.ReportModels.AnalysisRequestReportModel> data;
                switch (_reportMode)
                {
                    case ReportMode.Sample:
                        data = await reportDataAccess.GetAnalysisReportByCinAsync(e.Sid, 1);

                        break;
                    case ReportMode.Episode:
                        data = await reportDataAccess.GetAnalysisReportForEpisodeAsync(e.EpisodeNumber, 1);
                        break;
                    default:
                        data = null;
                        break;
                }

                if (data is null) { throw new Exception("No results for printing."); }
                if (data.Count == 0) { throw new Exception("No results for printing."); }

                var mappedData = MapReportData(data);

                switch (_reportAction)
                {
                    case ReportAction.Print:
                        OnPopupMessageRequired?.Invoke(this, new ReportServerNotificationModel
                        { Message = $"Printing report {data[0].Assays[0].Cin}", NotifyIcon = System.Windows.Forms.ToolTipIcon.Info });
                        ExecuteReportPrint(mappedData);
                        break;

                    case ReportAction.Preview:
                        OnReportPreviewRequest?.Invoke(this, new Report.AnalysisReportDoA() { DataSource = mappedData });
                        break;
                    case ReportAction.Export:
                        OnReportExportRequest?.Invoke(this, new Report.AnalysisReportDoA() { DataSource = mappedData });
                        break;

                    default:
                        OnPopupMessageRequired?.Invoke(this, new ReportServerNotificationModel
                        { Message = "Report action not specified. Actions: print, preview, export.", NotifyIcon = System.Windows.Forms.ToolTipIcon.Error });
                        break;
                }

            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ShowError(Exception ex)
        {
            OnPopupMessageRequired?.Invoke(this, new ReportServerNotificationModel()
            {
                Message = ex.Message,
                NotifyIcon = System.Windows.Forms.ToolTipIcon.Error
            });
        }

        private void ExecuteReportPrint
            (List<DoAAnalysisRequestReportModel> mappedData)
        {
            var report = new Report.AnalysisReportDoA();
            report.DataSource = mappedData;

            try
            {
                var printTool = new ReportPrintTool(report);
                printTool.Print();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

        }

        private List<DoAAnalysisRequestReportModel>
            MapReportData(List<DataLibrary.Models.ReportModels.AnalysisRequestReportModel> requestModel)
        {
            if (requestModel is null) { throw new ArgumentNullException(nameof(requestModel), "No data to map for report"); }
            var mappedDataList = new List<DoAAnalysisRequestReportModel>();

            var data = requestModel.FirstOrDefault();
            var analysis = data.Assays;
            var doaModel = new DoAAnalysisRequestReportModel()
            {
                CollectedDate = data.CollectedDate,
                Patient = new Patient()
                {
                    NidPp = data.Patient.NidPp,
                    Fullname = data.Patient.Fullname,
                    AgeSex = data.Patient.AgeSex,
                    Birthdate = data.Patient.Birthdate,
                    Address = data.Patient.Address,
                    Nationality = data.Patient.Nationality,
                    InstituteAssignedPatientId = data.InstituteAssignedPatientId
                },
                PrintedDate = DateTime.Now.ToString("dd-MM-yyyy HH:mm"),
                ReceivedDate = data.ReceivedDate,
                SampleSite = data.SampleSite,
                QcCalValidatedBy = data.QcCalValidatedBy,
                ReportedAt = data.ReportedAt,
                ReceivedBy = data.ReceivedBy,
                AnalysedBy = data.AnalysedBy,
                InstituteAssignedPatientId = data.InstituteAssignedPatientId,
            };

            doaModel.Methadone = analysis.FirstOrDefault((x) => x.Assay.ToLower().Contains("methadone") && x.Assay.EndsWith("_I"))?.Result;
            doaModel.Amphetamine = analysis.FirstOrDefault((x) => x.Assay.ToLower().Contains("amphetamine") && x.Assay.EndsWith("_I"))?.Result;
            doaModel.Benzodiazepines = analysis.FirstOrDefault((x) => x.Assay.ToLower().Contains("benzodiazepines") && x.Assay.EndsWith("_I"))?.Result;
            doaModel.Cannabinoids = analysis.FirstOrDefault((x) => x.Assay.ToLower().Contains("cannabinoids") && x.Assay.EndsWith("_I"))?.Result;
            doaModel.Cocaine = analysis.FirstOrDefault((x) => x.Assay.ToLower().Contains("cocaine") && x.Assay.EndsWith("_I"))?.Result;
            doaModel.Ethylglucuronide = analysis.FirstOrDefault((x) => x.Assay.ToLower().Contains("ethyl glucuronide") && x.Assay.EndsWith("_I"))?.Result;
            doaModel.Opiates = analysis.FirstOrDefault((x) => x.Assay.ToLower().Contains("opiates") && x.Assay.EndsWith("_I"))?.Result;
            doaModel.EpisodeNumber = data.EpisodeNumber;

            mappedDataList.Add(doaModel);
            return mappedDataList;

        }

        public Type GetModelType()
        {
            return typeof(ReportQueryParameters);
        }

        public void Print(string jsonData, string printerName, ReportMode reportMode, ReportAction reportAction = ReportAction.Print)
        {
            if (string.IsNullOrEmpty(jsonData)) { throw new ArgumentException("No data or parameters passed in for report generation."); }
            _printerName = string.IsNullOrEmpty(printerName) == false ? printerName : null;
            _reportMode = reportMode;
            _reportAction = reportAction;

            try
            {
                var parameter = JsonConvert.DeserializeObject<ReportQueryParameters>(jsonData);
                GetReportData?.Invoke(this, parameter);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
    }
}
