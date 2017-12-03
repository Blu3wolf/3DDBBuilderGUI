using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Diagnostics;

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
                        else
                        {
                            MessageBox.Show("Found a BMS key in the registry, but no baseDir subkey. Your BMS install is probably borked. Search for Dunc's registry cleaner and use that before reinstalling.", "Registry Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Could not find a BMS install in the registry. You will have to point to the DB you want to use manually.", "Could not find a BMS Install", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            DBsList = new ObservableCollection<ObjDB>();

            // Now knowing the BMSInstall, find all object folders in that install
            // can do this by reading theater.lst and then reading all .tdf files
            if (BMSInstall != null)
            {
                string theaterlst = BMSInstall + @"\Data\Terrdata\theaterdefinition\theater.lst";
                if (File.Exists(theaterlst))
                {
                    string[] theaters = File.ReadAllLines(theaterlst);
                    Regex objmatch = new Regex(@"3ddatadir (.*)");
                    foreach (string theater in theaters)
                    {
                        string tdfstring = BMSInstall + @"\Data\" + theater;
                        if (File.Exists(tdfstring))
                        {
                            string[] tdf = File.ReadAllLines(tdfstring);
                            foreach (string line in tdf)
                            {
                                // can any of this be tidied up?
                                if (line.StartsWith("3ddatadir"))
                                {
                                    Match match = objmatch.Match(line);
                                    string lobjdir = match.Result("$1");
                                    string objdir = BMSInstall + @"\Data\" + lobjdir;
                                    DBsList.Add(new ObjDB() { DirPath=objdir });
                                }
                            }
                        }
                    }
                    SelectedDB = DBsList[0];
                    MessageBox.Show("Found " + DBsList.Count + " object databases in your BMS install. You can select them from the dropdown menu.", "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Could not find theater.lst in your BMS install. Either I done goofed, or your install is probably borked.", "BMS Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            DataContext = this;
        }

        private string BMSInstall;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        private string extractionPath;

        public string CurExtractionPath
        {
            get => extractionPath;
            set
            {
                if (value != extractionPath)
                {
                    extractionPath = value;
                    NotifyPropertyChanged("CurExtractionPath");
                }
            }
        }

        private string updatePath;

        public string CurUpdatePath
        {
            get => updatePath;
            set
            {
                if (value != updatePath)
                {
                    updatePath = value;
                    NotifyPropertyChanged("CurUpdatePath");
                }
            }
        }

        private string buildSource;

        public string BuildSource
        {
            get => buildSource;
            set
            {
                if (value != buildSource)
                {
                    buildSource = value;
                    NotifyPropertyChanged("BuildSource");
                }
            }
        }

        private string buildOutput;

        public string BuildOutput
        {
            get => buildOutput;
            set
            {
                if (value != buildOutput)
                {
                    buildOutput = value;
                    NotifyPropertyChanged("BuildOutput");
                }
            }
        }

        public ObservableCollection<ObjDB> DBsList { get; set; }

        private ObjDB selectedDB;

        public ObjDB SelectedDB
        {
            get => selectedDB;
            set
            {
                if (value != selectedDB)
                {
                    selectedDB = value;
                    NotifyPropertyChanged("SelectedDB");
                }
            }
        }

        public string GUIWorDir
        {
            get => Directory.GetCurrentDirectory();
        }

        private ObjDB GetExistingDB(string dir)
        {
            // do we already have this DB?
            foreach (ObjDB objDB in DBsList)
            {
                if (dir == objDB.DirPath)
                {
                    return objDB;
                }
            }
            return null;
        }

        private void AddDB(string dir)
        {
            SelectedDB = GetExistingDB(dir);
            if (SelectedDB == null)
            {
                ObjDB objDB = new ObjDB { DirPath = dir };
                DBsList.Add(objDB);
                SelectedDB = objDB;
            }
        }

        public string GetFolder(bool returnType, bool isFolder, string windowTitle, string initDir)
        {
            // returnType true: save folder returned in Settings
            // isFolder: for folderpicker instead of file picker
            CommonOpenFileDialog dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = isFolder,
                EnsureReadOnly = true,
                Multiselect = false,
                Title = windowTitle,
                InitialDirectory = initDir
            };

            if (!Directory.Exists(initDir))
            {
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok && Directory.Exists(dialog.FileName))
            {
                if (returnType)
                    Properties.Settings.Default.ObjFolderName = dialog.FileName;
                Properties.Settings.Default.Save();
                return dialog.FileName;
            }
            else
            {
                return null;
            }
        }

        private void OpenFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                Process.Start(folder);
            }
            else
            {
                if (folder == null)
                {
                    MessageBox.Show("Please select a directory to open first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Could not find the directory specified:" + folder, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SourceSelectButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = GetFolder(true, true, "Select Object Folder...", BMSInstall + @"\Data\Terrdata");
            if (dir != null && ObjDB.Exists(dir))
            {
                AddDB(dir);
            }
            if (dir != null && !ObjDB.Exists(dir))
            {
                MessageBox.Show("No database was selected as one could not be located in the selected directory: \n\n" + dir, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            
        }

        private void DestSelectButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = GetFolder(false, true, "Select folder to extract Database to...", CurExtractionPath);
            
            if (dir != null)
            {
                CurExtractionPath = dir;
            }
        }

        private void ExtrButton_Click(object sender, RoutedEventArgs e)
        {
            ExCommand(true, SelectedDB, CurExtractionPath);
            BuildSource = CurExtractionPath;
        }

        private void ListParents(object sender, RoutedEventArgs e)
        {
            ExCommand(false, SelectedDB);
        }

        private void BuildNewButton_Click(object sender, RoutedEventArgs e)
        {
            string Message = "The output of this function is a database which is incompatible with all legacy tools, including LOD Editor. Do you wish to continue?";
            if (MessageBox.Show(Message, "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                BuildCommand(true, BuildSource, BuildOutput);
            }
        }

        private void BuildOldButton_Click(object sender, RoutedEventArgs e)
        {
            BuildCommand(false, BuildSource, BuildOutput);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ExCommand(false, SelectedDB, CurUpdatePath);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            ExCommand(true, SelectedDB);
        }

        private void ExCommand(string args)
        {
            // add a function to check to see if the exe is co-located here?

            using (var process = new Process())
            {
                try
                {
                    ProcessStartInfo startinfo = new ProcessStartInfo("3ddb_builder.exe", args);
                    startinfo.RedirectStandardOutput = true;
                    startinfo.UseShellExecute = false;
                    startinfo.CreateNoWindow = true;
                    process.StartInfo = startinfo;
                    process.Start();
                    string result = process.StandardOutput.ReadToEnd();
                }
                catch (Exception)
                {
                    throw;
                }
            }

            statuslabel.Content = "Success!";
        }

        private void ExCommand(bool IsTest, ObjDB DB)
        {
            if (!DB.IsValid())
            {
                MessageBox.Show("The DB could not be found at " + DB.DirPath, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string args;
            if (IsTest)
            {
                args = @"/objectdir " + "\"" + DB.DirPath + "\"" + @" /test";
                ExCommand(args);

                // then figure out what to do with the test results which chill in the output file
                Process.Start("HeaderChecker.txt");
            }
            else
            {
                args = @"/objectdir " + "\"" + DB.DirPath + "\"" + @" /parents";
                ExCommand(args);

                // then figure out what to do with the newly generated UnusedParents.txt file which currently just chills in the debug dir
                Process.Start("UnusedParents.txt");
            }
        }

        private void BuildCommand(bool IsNew, string source, string dest)
        {
            // check for existence of 256 color BMP file!

            string command;
            if (IsNew)
            {
                command = "/build";
            }
            else
            {
                command = "/build_old";
            }
            if (Directory.Exists(source) && Directory.Exists(dest))
            {
                string args = @"/objectdir " + "\"" + dest + "\" " + command + " \"" + source + "\"";
                ExCommand(args);
            }
        }

        private void ExCommand(bool IsExtract, ObjDB DB, string ExtractedFolders)
        {
            if (!DB.IsValid())
            {
                MessageBox.Show("The selected database could not be found at " + DB.DirPath, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!(Directory.Exists(ExtractedFolders) && ExtractedFolders != null))
            {
                MessageBox.Show("Could not find the specified path: " + ExtractedFolders, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (ExtractedFolders == null)
            {
                MessageBox.Show("Please select an extraction path first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsExtract)
            {
                string args = @"/objectdir " + "\"" + DB.DirPath + "\"" + @" /extract " + "\"" + ExtractedFolders + "\"";
                ExCommand(args);
            }
            else
            {
                string args = @"/objectdir " + "\"" + DB.DirPath + "\"" + @" /update " + "\"" + ExtractedFolders + "\"";
                ExCommand(args);
            }
        }

        private void ResetExtractionDirButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CurExtractionPath = Directory.GetCurrentDirectory();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void OpenExtractionDirButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(CurExtractionPath);
        }

        private void BuildSourceSelectButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = GetFolder(false, true, "Select folder to Build Database from...", BuildSource);

            if (dir != null)
            {
                BuildSource = dir;
            }
        }

        private void BuildDestSelectButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = GetFolder(false, true, "Select folder to save Database to...", BuildOutput);
            if (dir != null)
            {
                BuildOutput = dir;
            }
        }

        private void OpenBuildSourceDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(BuildSource);
        }

        private void OpenBuildOutputDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(BuildOutput);
        }

        private void SelCurUpdatePathButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = GetFolder(false, true, "Select Updates folder", CurUpdatePath);
            if (dir != null)
            {
                CurUpdatePath = dir;
            }
        }

        private void OpenCurUpdatePathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(CurUpdatePath);
        }

        private void ResetCurUpdatePathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CurUpdatePath = Directory.GetCurrentDirectory();
            }
            catch (Exception)
            {

                throw;
            }
        }
    }

    public class ObjDB
    {
        public ObjDB()
        {

        }

        public static bool Exists(String FolderName)
        {
            if (Directory.Exists(FolderName) && File.Exists(FolderName + @"\KoreaOBJ.LOD"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsValid()
        {
            return Exists(DirPath);
        }

        public string DirPath { get; set; }
    }
}
