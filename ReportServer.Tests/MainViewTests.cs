using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ReportServer.Views;
using ReportServer.Helpers;

namespace ReportServer.Tests
{
    [TestClass]
    public class ReportServerHelpers
    {
        [TestMethod]
        public void ReportServerHelpers_TestUncPathAvailability_True()
        {
            
            var testUncPath = @"\\swatinc-amina\CD4.AutoUpdateLocation";
            var isUncPath = CheckNetworkAccessHelper.QuickBestGuessAboutAccessibilityOfNetworkPath(testUncPath);

            Assert.IsTrue(isUncPath);
        }

        [TestMethod]
        public void ReportServerHelpers_TestUncPathAvailability_False()
        {
            
            var testUncPath = @"\\swatinc-nis\CD4.AutoUpdateLocation";
            var isUncPath = CheckNetworkAccessHelper.QuickBestGuessAboutAccessibilityOfNetworkPath(testUncPath);

            Assert.IsTrue(!isUncPath);
        }
    }
}
