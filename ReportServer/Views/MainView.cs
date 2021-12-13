using CD4.DataLibrary.DataAccess;
using DevExpress.Skins;
using DevExpress.XtraReports.UI;
using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using ReportServer.Extensibility.Models;
using ReportServer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReportServer.Views
{
    public partial class MainView : DevExpress.XtraEditors.XtraForm
    {
        private List<IExtensibility> _loadedExtensions;
        private ApplicationSettings _settings { get; set; }
        private bool IsMonitoringIncoming { get; set; }
        private string _reportExportBasepath;
        private const int CP_NOCLOSE_BUTTON = 0x200;


        private delegate void PreviewReport(XtraReport report);
        private PreviewReport _previewReportDelegate;

        private event EventHandler InitializeMonitoring;
        private event EventHandler<FileInfo> DetectedReportDataFile;
        public MainView()
        {
            InitializeComponent();
            InitializeSettings();
            InitializeExtensions();
            _previewReportDelegate = new PreviewReport(PreviewReportDelegateHandler);

            InitializeDataLib();
            DetectedReportDataFile += OnDetectedReportDataFile;
            InitializeMonitoring += OnInitializeMonitoringAsync;

            toolStripMenuItemExit.Click += ToolStripMenuItemExit_Click;
            FormClosing += MainView_FormClosing;
            IsMonitoringIncoming = true;
            InitializeMonitoring?.Invoke(this, EventArgs.Empty);

            ShowInitializeCompletedPopup();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CP_NOCLOSE_BUTTON;
                return cp;
            }
        }

        private void ShowInitializeCompletedPopup()
        {
            InfoPopup("CD4 report server initialization successful");
        }

        private void ShowApplicationExitPopUp()
        {
            WarningPopup("CD4 report server is exiting!");
        }

        private async void InitializeDataLib()
        {
            try
            {
                var test = new GlobalSettingsDataAccess();
                var data = await test.ReadAllGlobalSettingsAsync().ConfigureAwait(true);
                _reportExportBasepath = data?.ReportExportBasePath;
                InfoPopup($"Report export base path: {_reportExportBasepath}");
            }
            catch(SqlException)
            {
                ExceptionPopup("Cannot access database [SqlException].\n" +
                    "To enable report export, please restart ReportServer after database access is restored.");
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
            }
        }

        private void MainView_FormClosing(object sender, FormClosingEventArgs e)
        {
            IsMonitoringIncoming = false;
            notifyIcon.Visible = false;
        }

        private void OnDetectedReportDataFile(object sender, FileInfo reportDataFileInfo)
        {
            if (reportDataFileInfo.Exists == false) { return; }
            try
            {
                var jsonData = File.ReadAllText(reportDataFileInfo.FullName);
                PrintReport(jsonData);
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
            }
        }

        private async void OnInitializeMonitoringAsync(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {

                while (IsMonitoringIncoming)
                {
                    try
                    {
                        var controlFiles = Directory.GetFiles(_settings.IncomingDirectory, $"*.{_settings.ControlExtension}");
                        if (controlFiles.Length > 0)
                        {
                            foreach (var controlFile in controlFiles)
                            {
                                InfoPopup($"Detected a request for report generation.\n{controlFile}");

                                var dataFile = controlFile
                                    .Replace(_settings.ControlExtension, _settings.ReportExtension);

                                DeleteFileIfExists(controlFile);
                                var reportDataFileInfo = new FileInfo(dataFile);
                                if (reportDataFileInfo.Exists == false) { continue; }

                                DetectedReportDataFile?.Invoke(this, reportDataFileInfo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionPopup(ex);
                    }

                    Task.Delay(_settings.PolFrequencyInSec * 1000).GetAwaiter().GetResult();
                }
            });
        }

        private void DeleteFileIfExists(string fullFilename)
        {
            try
            {
                var fileInfo = new FileInfo(fullFilename);
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
            }

        }

        private void InitializeSettings()
        {
            SkinManager.EnableFormSkins();
            SkinManager.EnableMdiFormSkins();

            _settings = JsonConvert.DeserializeObject<ApplicationSettings>(Properties.Settings.Default.AppSettingsJson);
        }

        private void PrintReport(string jsonReportData)
        {
            if (_loadedExtensions.Count == 0) { return; }

            foreach (var extension in _loadedExtensions)
            {
                dynamic reportData = JsonConvert.DeserializeObject(jsonReportData);

                if (ExtensionMatchedWithTemplateName
                    (extension.ReportName, reportData.TemplateName.ToString()))
                {
                    if (string.IsNullOrEmpty((string)reportData.EpisodeNumber) == false)
                    {
                        extension.Print(jsonReportData, "", ReportMode.Episode);
                    }
                    else if (string.IsNullOrEmpty((string)reportData.Sid) == false)
                    {
                        extension.Print(jsonReportData, "", ReportMode.Sample);
                    }
                    else
                    {
                        WarningPopup("Cannot detect an episode number or sample number to " +
                            "generate the report. Request ignored!");
                    }


                }
            }

        }

        private bool ExtensionMatchedWithTemplateName
            (string extensionReportName, string templateName)
        {
            foreach (var template in templateName.Split(','))
            {
                return extensionReportName == template.Trim();
            }
            return false;
        }

        private void InitializeExtensions()
        {
            _loadedExtensions = new List<IExtensibility>();
            try
            {
                List<Assembly> allAssemblies = new List<Assembly>();
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                foreach (string dll in Directory.GetFiles(Path.Combine(path, "Extensions"), "*.dll"))
                    allAssemblies.Add(Assembly.LoadFile(dll));

                foreach (var assembly in allAssemblies)
                {
                    foreach (var type in GetAllTypesThatImplementInterface<IExtensibility>(assembly))
                    {
                        var instance = (IExtensibility)Activator.CreateInstance(type);
                        instance.OnPopupMessageRequired += Instance_OnPopupMessageRequired;
                        instance.OnReportExportRequest += Instance_OnReportExportRequest;
                        instance.OnReportPreviewRequest += Instance_OnReportPreviewRequest;
                        _loadedExtensions.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
            }

        }

        private void Instance_OnReportPreviewRequest(object sender, XtraReport e)
        {
            documentViewer.Invoke(_previewReportDelegate, new[] { e });
        }

        private void PreviewReportDelegateHandler(XtraReport report)
        {
            documentViewer.DocumentSource = report;
            documentViewer.InitiateDocumentCreation();
            WindowState = FormWindowState.Maximized;
            Activate();
        }

        private void Instance_OnReportExportRequest(object sender, XtraReport e)
        {
            try 
            { 
                ValidateReportBasePath();
                if (string.IsNullOrEmpty(e.DisplayName))
                {
                    e.DisplayName = $"Report_{Guid.NewGuid()}.pdf";
                }

                e.ExportToPdf($"{_reportExportBasepath}{e.DisplayName}");
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
            }
        }

        private void ValidateReportBasePath()
        {
            if (string.IsNullOrEmpty(_reportExportBasepath?.Trim()) == false) { return; }
            WarningPopup("Report export base path is not available. Trying to get base path before exporting.");

            try
            {
                InitializeDataLib();
            }
            catch (Exception)
            {
                throw new Exception("Failed to fetch report base path. Exporting will be aborted.");
            }
        }


        #region PopUP Methods
        private void ExceptionPopup(string exceptionMessage)
        {
            Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
            {
                Message = exceptionMessage,
                NotifyIcon = ToolTipIcon.Error
            });
        }
        private void ExceptionPopup(Exception ex)
        {
            Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
            {
                Message = $"{ex.Message}\n{ex.StackTrace}",
                NotifyIcon = ToolTipIcon.Error
            });
        }
        private void WarningPopup(string warnMessage)
        {
            Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
            {
                Message = warnMessage,
                NotifyIcon = ToolTipIcon.Warning
            });
        }

        private void InfoPopup(string infoMessage)
        {
            Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
            {
                Message = infoMessage,
                NotifyIcon = ToolTipIcon.Info
            });
        }
        private void Instance_OnPopupMessageRequired(object sender, ReportServerNotificationModel e)
        {
            notifyIcon.ShowBalloonTip(10, "Report Server notification", e.Message, e.NotifyIcon);
        }

        #endregion

        private IEnumerable<Type> GetAllTypesThatImplementInterface<T>(Assembly assembly)
        {
            return assembly
                .GetTypes()
                .Where(type => typeof(T).IsAssignableFrom(type) && !type.IsInterface);
        }

        private void ToolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            ShowApplicationExitPopUp();
            IsMonitoringIncoming = false;
            notifyIcon.Visible = false;
            Environment.Exit(0);
            Close();
        }
    }
}
