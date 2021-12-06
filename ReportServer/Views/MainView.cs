using CD4.DataLibrary.DataAccess;
using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using ReportServer.Extensibility.Models;
using ReportServer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReportServer.Views
{
    public partial class MainView : Form
    {
        private List<IExtensibility> _loadedExtensions;
        private ApplicationSettings _settings { get; set; }
        private bool IsMonitoringIncoming { get; set; }

        private event EventHandler InitializeMonitoring;
        private event EventHandler<FileInfo> DetectedReportDataFile;
        public MainView()
        {
            InitializeComponent();
            InitializeSettings();
            InitializeExtensions();
            //InitializeDataLib();

            DetectedReportDataFile += OnDetectedReportDataFile;
            InitializeMonitoring += OnInitializeMonitoringAsync;

            toolStripMenuItemExit.Click += ToolStripMenuItemExit_Click;
            FormClosing += MainView_FormClosing;
            IsMonitoringIncoming = true;
            InitializeMonitoring?.Invoke(this, EventArgs.Empty);

            ShowInitializeCompletedPopup();
        }

        private void ShowInitializeCompletedPopup()
        {
            Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
            {
                Message = "CD4 report server initialization successful",
                NotifyIcon = ToolTipIcon.Info
            });
        }

        private void InitializeDataLib()
        {
            var test = new GlobalSettingsDataAccess();
            var data = test.ReadAllGlobalSettingsAsync().GetAwaiter().GetResult();
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

                Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
                {
                    Message = ex.Message,
                    NotifyIcon = ToolTipIcon.Error
                });
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
                                Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
                                {
                                    Message = $"Detected a request for report generation.\n{controlFile}",
                                    NotifyIcon = ToolTipIcon.Info
                                });
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
                        Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
                        {
                            Message = ex.Message,
                            NotifyIcon = ToolTipIcon.Error
                        });
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
                Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
                { Message = ex.Message, NotifyIcon = ToolTipIcon.Error });
            }

        }

        private void InitializeSettings()
        {
            _settings = JsonConvert.DeserializeObject<ApplicationSettings>(Properties.Settings.Default.AppSettingsJson);
        }

        private void PrintReport(string jsonReportData)
        {
            if (_loadedExtensions.Count == 0) { return; }

            foreach (var extension in _loadedExtensions)
            {
                dynamic reportData = JsonConvert.DeserializeObject(jsonReportData);

                if (extension.ReportName == reportData.TemplateName.ToString())
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
                        Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
                        {
                            Message = "Cannot detect an episode number or sample number to generate the report. Request ignored!",
                            NotifyIcon = ToolTipIcon.Warning
                        });
                    }


                }
            }

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
                        _loadedExtensions.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                Instance_OnPopupMessageRequired(this, new ReportServerNotificationModel()
                {
                    Message = $"{ex.Message}\n{ex.StackTrace}",
                    NotifyIcon = ToolTipIcon.Error
                });
            }

        }

        private void Instance_OnPopupMessageRequired(object sender, ReportServerNotificationModel e)
        {
            notifyIcon.ShowBalloonTip(10, "Report Server notification", e.Message, e.NotifyIcon);
        }

        private IEnumerable<Type> GetAllTypesThatImplementInterface<T>(Assembly assembly)
        {
            return assembly
                .GetTypes()
                .Where(type => typeof(T).IsAssignableFrom(type) && !type.IsInterface);
        }

        private void ToolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            IsMonitoringIncoming = false;
            notifyIcon.Visible = false;
            Environment.Exit(0);
            Close();
        }
    }
}
