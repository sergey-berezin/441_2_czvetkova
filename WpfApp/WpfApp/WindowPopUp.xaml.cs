using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for WindowPopUp.xaml
    /// </summary>
    public partial class WindowPopUp : Window
    {
        public WindowPopUp()
        {
            InitializeComponent();
        }
        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Title = "File",
                Filter = "Text Document (*.txt) | *.txt",
                FileName = ""
            };
            if (openFileDialog.ShowDialog() == true)
            {
                MainWindow.textFileName = openFileDialog.FileName;
            }
            Hide();
        }

    }
}

