using CD4.DataLibrary.DataAccess;
using CD4.DataLibrary.Models.ReportModels;
using CD4.ReportTemplate.MedlabClinical.Models;
using DevExpress.XtraReports.UI;
using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CD4.ReportTemplate.MedlabClinical
{
    public class ClinicalTemplate : IExtensibility
    {
        private string _printerName;
        private event EventHandler<ReportQueryParameters> GetReportData;

        public string ReportName { get; set; }


        public ClinicalTemplate()
        {
            ReportName = "Medlab.ClinicalTemplate";
            GetReportData += OnGetReportData;
        }

        private async void OnGetReportData(object sender, ReportQueryParameters e)
        {
            try
            {
                var reportDataAccess = new ReportsDataAccess();
                var data = await reportDataAccess.GetAnalysisReportByCinAsync(e.EpisodeNumber, 1);
                if (data is null) { return; }

                var mappedData = MapReportData(data);
                ExecuteReportPrint(mappedData);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void ExecuteReportPrint
            (List<Entensibility.ReportingFramework.Models.AnalysisRequestReportModel> mappedData)
        {
            var report = new Report.AnalysisReport();
            report.DataSource = mappedData;

            var printTool = new ReportPrintTool(report);
            printTool.Print();
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

        public void Print(string jsonData, string printerName)
        {
            if (string.IsNullOrEmpty(jsonData)) { throw new ArgumentException("No data or parameters passed in for report generation."); }
            _printerName = string.IsNullOrEmpty(printerName) == false ? printerName : null;

            try
            {
                var parameter = JsonConvert.DeserializeObject<ReportQueryParameters>(jsonData);
                GetReportData?.Invoke(this, parameter);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
