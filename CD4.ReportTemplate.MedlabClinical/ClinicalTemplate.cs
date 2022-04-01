using CD4.DataLibrary.DataAccess;
using CD4.DataLibrary.Models.ReportModels;
using CD4.ReportTemplate.MedlabClinical.Models;
using DevExpress.XtraReports.UI;
using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using ReportServer.Extensibility.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CD4.ReportTemplate.MedlabClinical
{
    public class ClinicalTemplate : IExtensibility
    {
        private string _printerName;
        private ReportMode _reportMode;
        private ReportAction _reportAction;
        private int _reportTemplateId;

        private event EventHandler<ReportQueryParameters> GetReportData;
        public event EventHandler<ReportServerNotificationModel> OnPopupMessageRequired;
        public event EventHandler<XtraReport> OnReportExportRequest;
        public event EventHandler<XtraReport> OnReportPreviewRequest;

        public string ReportName { get; set; }


        public ClinicalTemplate()
        {
            ReportName = "Medlab.Clinical.AnalysisReport";
            GetReportData += OnGetReportDataAsync;

        }

        /// <summary>
        /// This is Id the report uses to query only the data required for the report excluding data for other templates.
        /// This is the PK Id for the report on database
        /// </summary>
        /// <param name="reportId">This is the PK Id (int) for the report on database</param>
        public void SetReportId(int reportId)
        {
            _reportTemplateId = reportId;
        }

        private async void OnGetReportDataAsync(object sender, ReportQueryParameters e)
        {
            try
            {
                var reportDataAccess = new ReportsDataAccess();
                List<AnalysisRequestReportModel> data;
                switch (_reportMode)
                {
                    case ReportMode.Sample:
                        data = await reportDataAccess.GetAnalysisReportByCinAsync(e.Sid, e.LoggedInUserId, _reportTemplateId);

                        break;
                    case ReportMode.Episode:
                        data = await reportDataAccess.GetAnalysisReportForEpisodeAsync(e.EpisodeNumber,e.LoggedInUserId, _reportTemplateId);
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
                        OnReportPreviewRequest?.Invoke(this, new Report.AnalysisReport() { DataSource = mappedData });
                        break;
                    case ReportAction.Export:
                        var reportExportData = GetReportExportData(mappedData);
                        OnReportExportRequest?.Invoke(this, new Report.AnalysisReport() 
                        {
                            DataSource = mappedData,
                            DisplayName = $"{reportExportData.EpisodeNumber}_{reportExportData.PatientName}({reportExportData.Nidpp})",
                            Tag = reportExportData 
                        });

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

        private ReportExportDataModel GetReportExportData
            (List<Entensibility.ReportingFramework.Models.AnalysisRequestReportModel> mappedData)
        {
            try
            {
                //basepath\yyyy\Month\day\Site\memoNo_name(nidPp).pdf
                var firstRecord = mappedData.FirstOrDefault();
                return new ReportExportDataModel()
                {
                    SampledSite = firstRecord.SampleSite,
                    Nidpp = firstRecord.Patient.NidPp,
                    EpisodeNumber = firstRecord.EpisodeNumber,
                    PatientName = firstRecord.Patient.Fullname
                };
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return new ReportExportDataModel();
            }

        }

        private void ExecuteReportPrint
            (List<Entensibility.ReportingFramework.Models.AnalysisRequestReportModel> mappedData)
        {
            var report = new Report.AnalysisReport();
            report.DataSource = mappedData;

            var printTool = new ReportPrintTool(report);

            try
            {
                printTool.Print();
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

        private List<Entensibility.ReportingFramework.Models.AnalysisRequestReportModel>
            MapReportData(List<AnalysisRequestReportModel> data)
        {
            if (data is null) { throw new ArgumentNullException(nameof(data), "No data to map for report"); }
            var mappedDataList = new List<Entensibility.ReportingFramework.Models.AnalysisRequestReportModel>();

            foreach (var item in data)
            {
                var mappedData = new Entensibility.ReportingFramework.Models.AnalysisRequestReportModel();
                mappedData.Assays = new BindingList<Entensibility.ReportingFramework.Models.Assays>();

                //map common data
                mappedData.AnalysedBy = item.AnalysedBy;
                mappedData.CollectedDate = item.CollectedDate;
                mappedData.EpisodeNumber = item.EpisodeNumber;
                mappedData.InstituteAssignedPatientId = item.InstituteAssignedPatientId;
                mappedData.QcCalValidatedBy = item.QcCalValidatedBy;
                mappedData.ReceivedBy = item.ReceivedBy;
                mappedData.ReceivedDate = item.ReceivedDate;
                mappedData.ReportedAt = item.ReportedAt;
                mappedData.SampleProcessedAt = item.SampleProcessedAt;
                mappedData.SampleSite = item.SampleSite;

                //mapping patient
                mappedData.Patient = new Entensibility.ReportingFramework.Models.Patient()
                {
                    Address = item.Patient.Address,
                    AgeSex = item.Patient.AgeSex,
                    Birthdate = item.Patient.Birthdate,
                    Fullname = item.Patient.Fullname,
                    InstituteAssignedPatientId = item.InstituteAssignedPatientId,
                    Nationality = item.Patient.Nationality,
                    NidPp = item.Patient.NidPp,
                };

                mappedData.PrintedDate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

                //map assay data
                foreach (var assay in item.Assays)
                {

                    var currentAssay = new Entensibility.ReportingFramework.Models.Assays()
                    {
                        Assay = assay.Assay,
                        Cin = assay.Cin,
                        Comment = assay.Comment,
                        Discipline = assay.Discipline,
                        DisplayNormalRange = assay.DisplayNormalRange,
                        PrimaryHeader = assay.PrimaryHeader,
                        Result = assay.Result,
                        SecondaryHeader = assay.SecondaryHeader,
                        SortOrder = assay.SortOrder,
                        Unit = assay.Unit
                    };
                    mappedData.Assays.Add(currentAssay);
                }

                mappedDataList.Add(mappedData);
            }



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
