using CD4.DataLibrary.DataAccess;
using CD4.Entensibility.ReportingFramework.Models;
using CD4.ReportTemplate.AnalyserGeneratedReport.Models;
using DevExpress.XtraReports.UI;
using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using ReportServer.Extensibility.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CD4.ReportTemplate.AnalyserGeneratedReport
{
    public class AnalyserGeneratedReportTemplate : IExtensibility
    {
        private string _printerName { get; set; }
        private event EventHandler<ReportQueryParameters> GetReportData;
        public event EventHandler<ReportServerNotificationModel> OnPopupMessageRequired;

        public string ReportName { get; set; }

        public AnalyserGeneratedReportTemplate()
        {
            ReportName = "Medlab.AnalyserGeneratedReport";
            GetReportData += OnGetReportData;
        }
        private async void OnGetReportData(object sender, ReportQueryParameters e)
        {
            try
            {
                var reportDataAccess = new ReportsDataAccess();
                var data = await reportDataAccess.GetAnalysisReportByCinAsync(e.EpisodeNumber, 1);
                if (data is null) { throw new Exception("No results for printing."); }
                if (data.Count == 0) { throw new Exception("No results for printing."); }

                var mappedData = MapReportData(data);
                ExecuteReportPrint(mappedData);
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
            (List<Entensibility.ReportingFramework.Models.AnalysisRequestReportModel> mappedData)
        {
            var report = new Report.AnalyserGeneratedReport();
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

        private List<Entensibility.ReportingFramework.Models.AnalysisRequestReportModel>
            MapReportData(List<DataLibrary.Models.ReportModels.AnalysisRequestReportModel> requestModel)
        {
            if (requestModel is null) { throw new ArgumentNullException(nameof(requestModel), "No data to map for report"); }
            var mappedDataList = new List<Entensibility.ReportingFramework.Models.AnalysisRequestReportModel>();

            foreach (var requestReport in requestModel)
            {
                var mappedRequest = new Entensibility.ReportingFramework.Models.AnalysisRequestReportModel
                {
                    SampleSite = requestReport.SampleSite,
                    CollectedDate = requestReport.CollectedDate,
                    ReceivedDate = requestReport.ReceivedDate,
                    PrintedDate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                    Patient = new Patient()
                    {
                        NidPp = requestReport.Patient.NidPp,
                        Fullname = requestReport.Patient.Fullname,
                        AgeSex = requestReport.Patient.AgeSex,
                        Birthdate = requestReport.Patient.Birthdate,
                        Address = requestReport.Patient.Address,
                        Nationality = requestReport.Patient.Nationality

                    },
                    Assays = new BindingList<Assays>(),
                    EpisodeNumber = requestReport.EpisodeNumber,
                    QcCalValidatedBy = requestReport.QcCalValidatedBy,
                    ReportedAt = requestReport.ReportedAt,
                    ReceivedBy = requestReport.ReceivedBy,
                    SampleProcessedAt = requestReport.SampleProcessedAt,
                };

                //handle assays 
                foreach (var assay in requestReport.Assays)
                {
                    //check whether the assay is qualitative
                    if (assay.Assay.EndsWith("_I"))
                    {
                        //get the quantitative assay
                        var quantitativeAssay = requestReport.Assays.FirstOrDefault((x) => $"{x.Assay}_I" == assay.Assay);
                        if (quantitativeAssay is null) { continue; }

                        //result reported against quantitative assay
                        var reportingResult = $"{assay.Result.Replace("ދައްކާ", "").Replace("ނު", "").Trim()} ({quantitativeAssay.Result})";

                        var reportingAssay = new Assays()
                        {
                            Cin = quantitativeAssay.Cin,
                            Discipline = quantitativeAssay.Discipline,
                            Assay = quantitativeAssay.Assay,
                            Result = reportingResult,
                            Unit = quantitativeAssay.Unit,
                            DisplayNormalRange = quantitativeAssay.DisplayNormalRange,
                            Comment = quantitativeAssay.Comment,
                            SortOrder = quantitativeAssay.SortOrder,
                            PrimaryHeader = quantitativeAssay.PrimaryHeader,
                            SecondaryHeader = quantitativeAssay.SecondaryHeader,
                        };

                        mappedRequest.Assays.Add(reportingAssay);
                    }
                }

                mappedDataList.Add(mappedRequest);
            }

            return mappedDataList;
        }

        public Type GetModelType()
        {
            return typeof(ReportQueryParameters);
        }

        public void Print(string jsonData, string printerName, ReportMode reportMode)
        {
            if (string.IsNullOrEmpty(jsonData)) { throw new ArgumentException("No data or parameters passed in for report generation."); }
            _printerName = string.IsNullOrEmpty(printerName) == false ? printerName : null;

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
