using CD4.DataLibrary.DataAccess;
using CD4.DataLibrary.Models.ReportModels;
using CD4.Entensibility.ReportingFramework.Models;
using CD4.ReportTemplate.DrugOfAbuseTemplate.Models;
using DevExpress.XtraReports.UI;
using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using ReportServer.Extensibility.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CD4.ReportTemplate.DrugOfAbuseTemplate
{
    public class DoATemplateHandler : IExtensibility
    {
        private string _printerName { get; set; }
        private event EventHandler<ReportQueryParameters> GetReportData;
        public event EventHandler<ReportServerNotificationModel> OnPopupMessageRequired;

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
            (List<DoAAnalysisRequestReportModel> mappedData)
        {
            var report = new Report.AnalysisReportDoA();
            report.DataSource = mappedData;

            var printTool = new ReportPrintTool(report);
            printTool.Print();
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
