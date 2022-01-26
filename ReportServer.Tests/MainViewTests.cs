using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ReportServer.Views;

namespace ReportServer.Tests
{
    [TestClass]
    public class MainViewTests
    {
        [TestMethod]
        public void MainViewTests_TestUncPathAvailability_True()
        {
            
            var testUncPath = @"\\swatinc-amina\CD4.AutoUpdateLocation";
            var isUncPath = new MainView().QuickBestGuessAboutAccessibilityOfNetworkPath(testUncPath);

            Assert.IsTrue(isUncPath);
        }

        [TestMethod]
        public void MainViewTests_TestUncPathAvailability_False()
        {
            
            var testUncPath = @"\\swatinc-nis\CD4.AutoUpdateLocation";
            var isUncPath = new MainView().QuickBestGuessAboutAccessibilityOfNetworkPath(testUncPath);

            Assert.IsTrue(!isUncPath);
        }
    }
}
