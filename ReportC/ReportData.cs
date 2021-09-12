using System.Collections.Generic;

namespace ReportC
{
    public class ReportData
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
        public List<Test> Results { get; set; }
    }
}
