﻿using System;
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

namespace _3DDBBuilderGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // load previously used Object folder locations
            // to do still

            // load default Object folder location from registry
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Benchmark Sims\\Falcon BMS 4.33 U1"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("baseDir");
                        if (o != null)
                        {
                            BMSInstall = o.ToString();  
                        }
                    }
                }
            }
            catch (Exception)  //just for demonstration...it's always best to handle specific exceptions
            {
                //react appropriately
            }
        }

        private string BMSInstall;

        private void SourceSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.EnsureReadOnly = true;
            dialog.Title = "Select Objects Folder...";
            dialog.InitialDirectory = BMSInstall += "\\Data\\Terrdata";
            CommonFileDialogResult result = dialog.ShowDialog();
        }

        private void DestSelectButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
