using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
using System.IO.Packaging;
using BusinessLogic;
using DataTypes;


namespace rmsis_reporter_project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RMSISBusinessLogic BL;

        public MainWindow()
        {
            InitializeComponent();
            BL = new RMSISBusinessLogic();
            DataContext = BL;
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            OpenTemplateDocument();
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void OpenTemplateDocument()
        {
            BL.GetRMSISProjects();

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name 
            dlg.DefaultExt = ".docx"; // Default file extension 
            dlg.Filter = "Word documents (.docx)|*.docx"; // Filter files by extension 
            dlg.InitialDirectory = Directory.GetCurrentDirectory() + "\\Templates";
            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

          
            if(File.Exists(dlg.FileName))
            {
                WordTemplateFilename.Text = dlg.FileName;
            }

        }
        public void ViewTemplateDoc(object sender, RoutedEventArgs e)
        {
            if (File.Exists(WordTemplateFilename.Text.Trim()))
            {
                // Open document
                System.Diagnostics.Process wordProcess = new System.Diagnostics.Process();
                wordProcess.StartInfo.FileName = WordTemplateFilename.Text.Trim();
                wordProcess.StartInfo.UseShellExecute = true;
                wordProcess.Start();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void ButtonGenerateReport(object sender, RoutedEventArgs e)
        {
            string BaseLine = null;
            if(String.IsNullOrEmpty((string)BaseLineComboBox.SelectedValue))
            {
                BaseLine = "Empty";
            }

            string document = BL.GenerateWordDocument(WordTemplateFilename.Text.Trim(), BaseLine, ProjectComboBox.SelectedValue.ToString().Trim(), LocationTextBox.Text.Trim(), 1);
            
            //Show resulting report

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ProjectComboBox_Initialized(object sender, EventArgs e)
        {
          
        }

        private void ProjectComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ProjectComboBox.SelectedIndex = 2;
        }

        private void WordTemplateFilename_Loaded(object sender, RoutedEventArgs e)
        {
            WordTemplateFilename.Text = "C:\\ACS\\Dropbox\\Private\\rmsis_reporter\\rmsis_reporter_project\\rmsis_reporter_project\\bin\\Debug\\Templates\\SystemRequirementsSpecification.docx";
        }

        private void LocationTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("C:\\Temp\\Outfile.docx"))
            {
                File.Delete("C:\\Temp\\Outfile.docx");
            }
            LocationTextBox.Text = "C:\\Temp\\Outfile.docx";
        }
    }
}
