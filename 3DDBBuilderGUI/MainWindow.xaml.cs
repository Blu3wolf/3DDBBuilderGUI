using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;

namespace _3DDBBuilderGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();

            // load previously used Object folder locations
            // to do still
            // where should that get saved? appinfo?

            // load default Object folder location from registry
            using (RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (RegistryKey bmskey = view32.OpenSubKey(@"Software\Benchmark Sims\Falcon BMS 4.33 U1", false))
                {
                    if (bmskey != null)
                    {
                        Object dir = bmskey.GetValue("baseDir");
                        if (dir != null)
                        {
                            BMSInstall = dir.ToString();
                        }
                    }
                }
            }

            extractionPath = "Path to extract database to TEST";

            DataContext = this;
        }

        private string BMSInstall;

        private string extractionPath;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ExtractionPath
        {
            get => extractionPath;
            set
            {
                extractionPath = value;
                PropertyChanged(this, )
            }
        }

        private bool DBExists(string path)
        {
            string filepath = path += @"\KoreaObj.LOD";
            if (File.Exists(filepath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SourceSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.EnsureReadOnly = true;
            dialog.Multiselect = false;
            dialog.Title = "Select Objects Folder...";
            dialog.InitialDirectory = BMSInstall += @"\Data\Terrdata";
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var dir = dialog.FileName;
                if (DBExists(dir))
                {
                    int item = DBBox.Items.Add(dir);
                    DBBox.SelectedIndex = item;
                }
                else
                {
                    string file = "Error: Could not find a database at " + dir;
                    MessageBox.Show(file);
                }
            }
        }

        private void DestSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Multiselect = false;
            dialog.Title = "Select folder to extract Database to...";
            if (Directory.Exists(ExtractionPath))
            {
                dialog.InitialDirectory = ExtractionPath;
            }
            else
            {
                dialog.InitialDirectory = "Desktop";
            }
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var dir = dialog.FileName;
                if (Directory.Exists(dir))
                {
                    extractionPath = dir;

                }
            }
        }
    }
}
