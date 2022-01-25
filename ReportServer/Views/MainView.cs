using CD4.DataLibrary.DataAccess;
using DevExpress.Skins;
using DevExpress.XtraBars.Alerter;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraReports.UI;
using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using ReportServer.Extensibility.Models;
using ReportServer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
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
        private string _reportExportBasePath;
        private readonly BindingList<XtraReport> _listOfReports;
        private TabPane _tabPane;
        // ReSharper disable once InconsistentNaming
        private const int CP_NOCLOSE_BUTTON = 0x200;
        private readonly AlertControl _alertControl;

        private ApplicationSettings Settings { get; set; }
        private List<ReportConfigModel> ReportConfig { get; set; }
        private bool IsMonitoringIncoming { get; set; }

        private delegate void PreviewReport(XtraReport report);
        private readonly PreviewReport _previewReportDelegate;

        private event EventHandler InitializeMonitoring;
        private event EventHandler<FileInfo> DetectedReportDataFile;
        public MainView()
        {
            InitializeComponent();
            _alertControl = new AlertControl() { FormShowingEffect = AlertFormShowingEffect.SlideHorizontal };
            InitializeTabPane();
            _listOfReports = new BindingList<XtraReport>();
            InitializeSettings();
            InitializeReportConfig();
            InitializeExtensions();
            _previewReportDelegate = new PreviewReport(PreviewReportDelegateHandler);

            InitializeDataLib();
            DetectedReportDataFile += OnDetectedReportDataFile;
            InitializeMonitoring += OnInitializeMonitoringAsync;
            _listOfReports.ListChanged += _listOfReports_ListChanged;
            _tabPane.SelectedPageChanged += TabPane_SelectedPageChangedAssignDocument;

            toolStripMenuItemExit.Click += ToolStripMenuItemExit_Click;
            FormClosing += MainView_FormClosing;
            IsMonitoringIncoming = true;
            InitializeMonitoring?.Invoke(this, EventArgs.Empty);

            ShowInitializeCompletedPopup();
            Resize += MainView_Resize;
            KeyDown += MainView_KeyDown;

        }

        private void InitializeReportConfig()
        {
            try
            {
                ReportConfig = new List<ReportConfigModel>();
                var reportSettingsJson = Properties.Settings.Default.ReportConfig;
                var reportSettings = JsonConvert.DeserializeObject<List<ReportConfigModel>>(reportSettingsJson);
                if (reportSettings != null)
                {
                    ReportConfig.AddRange(reportSettings);
                }
                else { ExceptionPopup("Cannot load settings for report server"); };
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
            }

        }

        private void MainView_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                #region Hide to system tray
                if (e.KeyCode == Keys.Escape)
                {
                    WindowState = FormWindowState.Minimized;
                }
                #endregion
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
            }


        }

        private void TabPane_SelectedPageChangedAssignDocument
            (object sender, SelectedPageChangedEventArgs e)
        {
            _tabPane.SuspendLayout();
            if (_tabPane.SelectedPageIndex != -1)
            {
                documentViewer.DocumentSource = _listOfReports[_tabPane.SelectedPageIndex];
                documentViewer.InitiateDocumentCreation();
                _tabPane.SelectedPage.Controls.Add(documentViewer);
            }

            _tabPane.ResumeLayout();
        }

        private void _listOfReports_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                //TabPaneRemoveAllPages();
                AddTabPanePagesForReports(e.NewIndex);
            }
        }

        /// <summary>
        /// Adds Tab pages to tab pane
        /// </summary>
        /// <param name="startIndex">This is the index of the newly added report. 
        /// loop will start adding from this index to avoid duplicate tab panes</param>
        private void AddTabPanePagesForReports(int startIndex)
        {
            var tabsNo = _listOfReports.Count;
            var pages = new List<TabNavigationPage>();

            _tabPane.SuspendLayout();

            for (int i = startIndex; i <= tabsNo - 1; i++)
            {
                var page = new TabNavigationPage() { Caption = $@"Report [ {i + 1} ]" };
                page.Controls.Add(documentViewer);
                pages.Add(page);
                _tabPane.Controls.Add(page);
            }

            _tabPane.Pages.AddRange(pages);
            _tabPane.SelectedPage = pages.LastOrDefault();
            _tabPane.ResumeLayout();

        }

        private void TabPaneRemoveAllPages()
        {
            _tabPane.SuspendLayout();
            _tabPane.Pages.Clear();
            _tabPane.Controls.Clear();
            _tabPane.ResumeLayout();
        }

        private void InitializeTabPane()
        {
            _tabPane = new TabPane { Dock = DockStyle.Fill };

            #region Prepare to init layout
            _tabPane.SuspendLayout();
            SuspendLayout();
            #endregion

            Controls.Add(_tabPane);

            #region Complete layout init
            _tabPane.ResumeLayout(false);
            ResumeLayout(false);
            #endregion
        }

        private void MainView_Resize(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case FormWindowState.Normal:
                    break;
                case FormWindowState.Minimized:
                    Hide(); // this required to hide the window from TAB key when minimized.
                    _listOfReports.Clear();
                    TabPaneRemoveAllPages();

                    break;
                case FormWindowState.Maximized:
                    break;
                default:
                    break;
            }
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
                _reportExportBasePath = data?.ReportExportBasePath;
                InfoPopup($"Report export base path: {_reportExportBasePath}");
            }
            catch (SqlException)
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
                        var controlFiles = Directory.GetFiles(Settings.IncomingDirectory, $"*.{Settings.ControlExtension}");
                        if (controlFiles.Length > 0)
                        {
                            foreach (var controlFile in controlFiles)
                            {
                                InfoPopup($"Detected a request for report generation.\n{controlFile}");

                                var dataFile = controlFile
                                    .Replace(Settings.ControlExtension, Settings.ReportExtension);

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

                    Task.Delay(Settings.PolFrequencyInSec * 1000).GetAwaiter().GetResult();
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

            Settings = JsonConvert.DeserializeObject<ApplicationSettings>(Properties.Settings.Default.AppSettingsJson);
        }

        private void PrintReport(string jsonReportData)
        {
            if (_loadedExtensions.Count == 0)
            {
                WarningPopup($"No report extensions loaded!");
                return;
            }
            dynamic reportData = JsonConvert.DeserializeObject(jsonReportData);
            if (reportData is null)
            {
                ExceptionPopup($"Report data is null. Aborting report generating");
                return;
            }
            var templateFound = false;

            foreach (var template in GetTemplateNames(reportData.TemplateName.ToString()))
            {

                foreach (var extension in _loadedExtensions)
                {
                    var reportAction = GetReportAction(reportData);

                    if (ExtensionMatchedWithTemplateName
                        (extension.ReportName, template))
                    {
                        templateFound = true;
                        InfoPopup($"Selected report template [{template}]");

                        if (string.IsNullOrEmpty((string)reportData.EpisodeNumber) == false)
                        {
                            extension.Print(jsonReportData, "", ReportMode.Episode, reportAction);
                        }
                        else if (string.IsNullOrEmpty((string)reportData.Sid) == false)
                        {
                            extension.Print(jsonReportData, "", ReportMode.Sample, reportAction);
                        }
                        else
                        {
                            WarningPopup("Cannot detect an episode number or sample number to " +
                                "generate the report. Request ignored!");
                        }

                    }

                }

            }


            if (!templateFound)
            {
                ExceptionPopup($"Cannot find the plugin for report template [ {reportData.TemplateName.ToString()} ] specified.");
            }

        }

        private ReportAction GetReportAction(dynamic reportData)
        {
            var actionString = (string)reportData.Action;
            var isActionEnum = Enum.TryParse(actionString, out ReportAction action);
            if (isActionEnum) { return action; }

            WarningPopup("Cannot detect report action. Assuming report action as preview.");
            return ReportAction.Preview;
        }

        private bool ExtensionMatchedWithTemplateName
            (string extensionReportName, string templateName)
        {
            foreach (var template in GetTemplateNames(templateName))
            {
                Console.WriteLine(@"looking for template: " + template);
                return extensionReportName == template.Trim();
            }
            return false;
        }

        private string[] GetTemplateNames(string csvTemplateName)
        {
            return csvTemplateName.Split(',');
        }

        private void InitializeExtensions()
        {
            _loadedExtensions = new List<IExtensibility>();
            var extensionNamesForPopUp = "";
            try
            {
                //
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                if (path != null)
                {
                    var allAssemblies = Directory.GetFiles(Path.Combine(path, "Extensions"),
                            "*.dll")
                        .Select(Assembly.LoadFile)
                        .ToList();

                    if (allAssemblies.Count == 0)
                    {
                        ExceptionPopup($"No assemblies detected in extensions directory\n{path}");
                        return;
                    }

                    foreach (var instance in from assembly in allAssemblies
                                             from type in GetAllTypesThatImplementInterface<IExtensibility>(assembly)
                                             select (IExtensibility)Activator.CreateInstance(type))
                    {
                        instance.OnPopupMessageRequired += Instance_OnPopupMessageRequired;
                        instance.OnReportExportRequest += Instance_OnReportExportRequest;
                        instance.OnReportPreviewRequest += Instance_OnReportPreviewRequest;

                        var reportId = GetReportIdByNameFromConfig(instance.ReportName);
                        instance.SetReportId(reportId);
                        _loadedExtensions.Add(instance);
                    }

                    if (_loadedExtensions.Count == 0)
                    {
                        ExceptionPopup("No report template extensions loaded");
                        return;
                    }

                    //get the names of all extensions to show in notification
                    extensionNamesForPopUp = _loadedExtensions.Aggregate(extensionNamesForPopUp,
                        (reportNames,
                            extension) => $"{reportNames}\n{extension.ReportName}");

                    InfoPopup($"Report extensions loaded. {extensionNamesForPopUp}");
                }
                else
                {
                    ExceptionPopup($"Cannot find the path to load extensions.");
                }
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
            }

        }

        private int GetReportIdByNameFromConfig(string reportName)
        {
            if (ReportConfig is null || ReportConfig?.Count <= 0)
            {
                ExceptionPopup("Report configuration does not exist.");
                return 0;
            }

            foreach (var item in ReportConfig)
            {
                if (item.ReportName == reportName)
                {
                    return item.Id;
                }
            }

            ExceptionPopup($"Cannot find [ReportId] for report template: [{reportName}]");
            return 0;
        }

        private void Instance_OnReportPreviewRequest(object sender, XtraReport e)
        {
            documentViewer.Invoke(_previewReportDelegate, new[] { e });
        }

        private void PreviewReportDelegateHandler(XtraReport report)
        {
            _listOfReports.Add(report);

            documentViewer.DocumentSource = report;
            documentViewer.InitiateDocumentCreation();
            WindowState = FormWindowState.Maximized;
            Show(); //shows the window
            Activate(); //brings the window to TOP
        }

        private void Instance_OnReportExportRequest(object sender, XtraReport e)
        {
            try
            {
                var reportExportData = GetReportExportData(e);

                ValidateReportBasePath();
                if (string.IsNullOrEmpty(e.DisplayName))
                {
                    e.DisplayName = $@"Report_{Guid.NewGuid()}.pdf";
                }


                var tempReportExportPath = "";
                if (_reportExportBasePath.StartsWith(@"\\"))
                {
                    var pathExists = QuickBestGuessAboutAccessibilityOfNetworkPath(_reportExportBasePath);
                    if (!pathExists)
                    {
                        ExceptionPopup($@"Export path is not accessible [{_reportExportBasePath}].{"\n"}Exporting to default path [ C:\ReportExports\ ]");
                        tempReportExportPath = @"C:\ReportExports";
                    }
                    else
                    {
                        tempReportExportPath = _reportExportBasePath;
                    }
                }
                else
                {
                    tempReportExportPath = _reportExportBasePath;
                }

                var exportDirectoryStructure = $"{DateTime.Today:yyyy}\\{DateTime.Today:MMMM}\\{DateTime.Today:dd}\\{reportExportData.SampledSite.Trim()}";
                var exportDirPath = $"{tempReportExportPath}\\{exportDirectoryStructure}";
                e.DisplayName = RemoveInvalidCharactersForExport(e.DisplayName);

                CreateDirectoryIfNotExists(exportDirPath);
                e.ExportToPdf($"{exportDirPath}\\{e.DisplayName}.pdf");
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
            }
        }

        private void CreateDirectoryIfNotExists(string directoryPath)
        {
            // If directory does not exist, create it
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Replaces invalid characters for a windows file name
        /// </summary>
        /// <returns></returns>
        private string RemoveInvalidCharactersForExport(string value)
        {
            return value
                .Replace('\\', '-')
                .Replace('/', '-')
                .Replace('(', '-')
                .Replace(')', '-')
                .Replace("C|", "")
                .Replace("E|", "");
        }

        /// <summary>
        /// The Tag of the xtrareport should have an instance of ReportExportModel assigned with data
        /// </summary>
        private ReportExportDataModel GetReportExportData(XtraReport e)
        {
            try
            {
                return (ReportExportDataModel)e.Tag;
            }
            catch (Exception ex)
            {
                ExceptionPopup(ex);
                return new ReportExportDataModel();
            }
        }

        private void ValidateReportBasePath()
        {
            if (string.IsNullOrEmpty(_reportExportBasePath?.Trim()) == false) { return; }
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
            if (InvokeRequired)
            {
                _ = BeginInvoke(new MethodInvoker(() => Instance_OnPopupMessageRequired(sender, e)));
            }
            else
            {
                Image image = null;
                switch (e.NotifyIcon)
                {
                    case ToolTipIcon.Info:
                        image = Properties.Resources.information;
                        break;
                    case ToolTipIcon.Warning:
                        image = Properties.Resources.warning;
                        break;
                    case ToolTipIcon.Error:
                        image = Properties.Resources.error;
                        break;

                    case ToolTipIcon.None:
                        image = Properties.Resources.unknown;
                        break;
                    default:
                        image = Properties.Resources.unknown;
                        break;
                }

                if (image is null)
                {
                    image = Properties.Resources.unknown;
                }

                _alertControl.Show(this,
                    $"Report Server notification: {e.NotifyIcon}",
                    e.Message, image);
            }
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

        public static bool QuickBestGuessAboutAccessibilityOfNetworkPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string pathRoot = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(pathRoot)) return false;
            ProcessStartInfo info = new ProcessStartInfo("net", "use")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var output = "";
            using (var p = Process.Start(info))
            {
                if (p != null) output = p.StandardOutput.ReadToEnd();
            }

            return output.Split('\n')
                .Any(line => line.Contains(pathRoot) && line.Contains("OK"));
        }
    }
}
