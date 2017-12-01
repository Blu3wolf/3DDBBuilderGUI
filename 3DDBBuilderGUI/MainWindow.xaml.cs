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
                            MessageBox.Show("Your BMS install is probably borked. Search for Dunc's registry cleaner and use that before reinstalling.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Could not find a BMS install in the registry. You will have to point to the DB you want to use manually.");
                    }
                }
            }

            DBList = new ObservableCollection<ObjDB>();

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
                                if (line.StartsWith("3ddatadir"))
                                {
                                    Match match = objmatch.Match(line);
                                    string lobjdir = match.Result("$1");
                                    string objdir = BMSInstall + @"\Data\" + lobjdir;
                                    AddDBObj(objdir);
                                    DBList.Add(new ObjDB(objdir));
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Could not find theater.lst in your BMS install. You will have to point to the DB you want to use manually.");
                }
                DBBox.SelectedIndex = 0;
                LPDBBox.SelectedIndex = 0;
            }

            extractionPath = "Path to extract database to TEST";

            DataContext = this;
        }

        private string BMSInstall;

        private string extractionPath;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurExtractionPath
        {
            get => extractionPath;
            set
            {
                extractionPath = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ExtractionPath"));
            }
        }

        public ObservableCollection<ObjDB> DBList { get; set; }

        private ObjDB DB;

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

        private void AddDBObj(string dir)
        {
            
            if (DBExists(dir))
            {
                int item = DBBox.Items.Add(dir);
                DBBox.SelectedIndex = item;
                int itemLP = LPDBBox.Items.Add(dir);
                LPDBBox.SelectedIndex = itemLP;
            }
            else
            {
                string file = "Error: Could not find a database at " + dir;
                MessageBox.Show(file);
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

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok && File.Exists(dialog.FileName))
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
            if (dir != null)
            {
                AddDBObj(dir);
            }
        }

        private void DestSelectButton_Click(object sender, RoutedEventArgs e)
        {
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
            if (DBExists(DBBox.Text))
            {
                string command = @"/objectdir " + "\"" + DBBox.Text + "\"" + @" /extract " + "\"" + CurExtractionPath + "\"";
                ExCommand(command);
            }
            else
            {
                MessageBox.Show("The DB could not be found at " + DBBox.Text);
            }
        }

        private void ListParents(object sender, RoutedEventArgs e)
        {
            if (DBExists(LPDBBox.Text))
            {
                string command = @"/objectdir " + "\"" + LPDBBox.Text + "\"" + @" /parents";
                ExCommand(command);
                statuslabel.Content = "Success!";
                // then figure out what to do with the newly generated UnusedParents.txt file which currently just chills in the debug dir
            }
            else
            {
                MessageBox.Show("The DB could not be found at " + LPDBBox.Text);
            }
        }

        private void ExCommand(string command)
        {
            // add a function to check to see if the exe is co-located here?

            using (var process = new System.Diagnostics.Process())
            {
                try
                {
                    string args = @"/c " + command;
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

        private void LPSourceSelectButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = GetFolder(true, true);
            if (dir != null)
            {
                // old method
                AddDBObj(dir);
                // new method
                ObjDB Obj = new ObjDB(dir);
                DBList.Add(Obj);
            }
        }
    }
}
