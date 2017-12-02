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
                        MessageBox.Show("Could not find a BMS install in the registry. You will have to point to the DB you want to use manually.", "Could not find a BMS Install", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                    MessageBox.Show("Found " + DBsList.Count + " object databases in your BMS install.", "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Could not find theater.lst in your BMS install. Either I done goofed, or your install is probably borked.", "BMS Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            extractionPath = "Path to extract database to TEST";

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
                    NotifyPropertyChanged("ExtractionPath");
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

        public string GetFolder(bool returnType, bool isFolder)
        {
            // returnType true: save folder returned in Settings
            // isFolder: for folderpicker instead of file picker
            CommonOpenFileDialog dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = isFolder,
                EnsureReadOnly = true,
                Multiselect = false,
                Title = "Select Objects Folder...",
                InitialDirectory = BMSInstall + @"\Data\Terrdata"
            };

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

        private void SourceSelectButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = GetFolder(true, true);
            if (dir != null && ObjDB.Exists(dir))
            {
                AddDB(dir);
            }
            if (!ObjDB.Exists(dir))
            {
                MessageBox.Show("Could not find a DB to select at " + dir);
            }
            
        }

        private void DestSelectButton_Click(object sender, RoutedEventArgs e)
        {
            // tidy this all up
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Multiselect = false;
            dialog.Title = "Select folder to extract Database to...";
            if (Directory.Exists(CurExtractionPath))
            {
                dialog.InitialDirectory = CurExtractionPath;
            }
            else
            {
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var dir = dialog.FileName;
                if (Directory.Exists(dir))
                {
                    CurExtractionPath = dir;
                }
            }
        }

        private void ExtrButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDB.IsValid())
            {
                string command = @"/objectdir " + "\"" + SelectedDB.DirPath + "\"" + @" /extract " + "\"" + CurExtractionPath + "\"";
                ExCommand(command);
            }
            else
            {
                MessageBox.Show("The DB could not be found at " + SelectedDB.DirPath);
            }
        }

        private void ListParents(object sender, RoutedEventArgs e)
        {
            if (SelectedDB.IsValid())
            {
                string command = @"/objectdir " + "\"" + SelectedDB.DirPath + "\"" + @" /parents";
                ExCommand(command);
                statuslabel.Content = "Success!";
                // then figure out what to do with the newly generated UnusedParents.txt file which currently just chills in the debug dir
                System.Diagnostics.Process.Start("UnusedParents.txt");
            }
            else
            {
                MessageBox.Show("The DB could not be found at " + SelectedDB.DirPath);
            }
        }

        private void ExCommand(string args)
        {
            // add a function to check to see if the exe is co-located here?

            using (var process = new System.Diagnostics.Process())
            {
                try
                {
                    System.Diagnostics.ProcessStartInfo startinfo = new System.Diagnostics.ProcessStartInfo("3ddb_builder.exe", args);
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
