using CD4.DataLibrary.DataAccess;
using Newtonsoft.Json;
using ReportServer.Extensibility.Interfaces;
using ReportServer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
            catch (Exception)
            {

                throw;
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
                                var dataFile = controlFile
                                    .Replace(_settings.ControlExtension, _settings.ReportExtension);

                                DeleteFileIfExists(controlFile);
                                var reportDataFileInfo = new FileInfo(dataFile);
                                if (reportDataFileInfo.Exists == false) { continue; }

                                DetectedReportDataFile?.Invoke(this, reportDataFileInfo);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        throw;
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
            catch (Exception)
            {

                throw;
            }

        }

        private void InitializeSettings()
        {
            _settings = JsonConvert.DeserializeObject<ApplicationSettings>(Properties.Settings.Default.AppSettingsJson);
        }

        private void PrintReport(string jsonReportData)
        {
            if(_loadedExtensions.Count == 0) { return; }

            foreach (var extension in _loadedExtensions)
            {
                dynamic reportData = JsonConvert.DeserializeObject(jsonReportData);

                if (extension.ReportName == reportData.TemplateName.ToString())
                {
                    extension.Print(jsonReportData, "");
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

                foreach (string dll in Directory.GetFiles(Path.Combine(path,"Extensions"), "*.dll"))
                    allAssemblies.Add(Assembly.LoadFile(dll));

                foreach (var assembly in allAssemblies)
                {
                    foreach (var type in GetAllTypesThatImplementInterface<IExtensibility>(assembly))
                    {
                        var instance = (IExtensibility)Activator.CreateInstance(type);
                        _loadedExtensions.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}\n{ex.StackTrace}");
            }

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
