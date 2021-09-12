using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using System;
using System.Collections.Generic;

namespace ReportC
{
    public class ReportSee : IExtensibility
    {
        public string ReportName { get; private set; }
        public ReportSee()
        {
            ReportName = "See";
        }
        public Type GetModelType()
        {
            return typeof(AnalysisReport);
        }

        public void Print(string jsonData, string printerName)
        {
            try
            {
                var reportModel = JsonConvert.DeserializeObject<AnalysisReport>(jsonData);

                var analysisReport = new Report();
                //analysisReport.Database.Tables[0].SetDataSource(new List<ReportMiscData>() { GetPatientData(reportModel) });
                //analysisReport.Database.Tables[1].SetDataSource(reportModel.ReportData.Results);
                analysisReport.SetDataSource(new object[] { GetPatientData(reportModel), reportModel.ReportData.Results });

                analysisReport.PrintToPrinter(1, false, 0, 0);
            }
            catch (Exception ex)
                                                                                                                                                                                                                                                                                                                                                                                                                                                                               {
                throw;
            }

        }

        private ReportMiscData GetPatientData(AnalysisReport reportModel)
        {
            return new ReportMiscData()
            {
                NidPp = reportModel.ReportData.NidPp,
                FullName = reportModel.ReportData.FullName,
                AgeSex = reportModel.ReportData.AgeSex,
                Birthdate = reportModel.ReportData.Birthdate,
                Address = reportModel.ReportData.Address,
                Nationality = reportModel.ReportData.Nationality,
                SampleSite = reportModel.ReportData.SampleSite,
                CollectedDate = reportModel.ReportData.CollectedDate,
                ReceivedDate = reportModel.ReportData.ReceivedDate,
                Cin = reportModel.ReportData.Cin,
                EpisodeNumber = reportModel.ReportData.EpisodeNumber,
                QcCalValidatedBy = reportModel.ReportData.QcCalValidatedBy,
                AnalysedBy = reportModel.ReportData.AnalysedBy,
                InstituteAssignedPatientId = reportModel.ReportData.InstituteAssignedPatientId,
                SampleProcessedAt = reportModel.ReportData.SampleProcessedAt
            };
        }
    }

    public class ReportMiscData
    {
        public string NidPp { get; set; }
        public string FullName { get; set; }
        public string AgeSex { get; set; }
        public string Birthdate { get; set; }
        public string Address { get; set; }
        public string Nationality { get; set; }
        public string SampleSite { get; set; }
        public string CollectedDate { get; set; }
        public string ReceivedDate { get; set; }
        public string Cin { get; set; }
        public string EpisodeNumber { get; set; }
        public string QcCalValidatedBy { get; set; }
        public string ReportedAt { get; set; }
        public string ReceivedBy { get; set; }
        public string AnalysedBy { get; set; }
        public string InstituteAssignedPatientId { get; set; }
        public string SampleProcessedAt { get; set; }
    }
}
