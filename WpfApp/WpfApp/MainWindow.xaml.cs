using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Net;
using NuGetNN;
using System.Threading;
using static System.Collections.Specialized.BitVector32;
using System.ComponentModel;
using System.Security.Policy;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow currentMainWindow = null;
        public static string textFileName = null;
        public static NuGetNN.NeuralNetwork neuralNetwork = null;
        CancellationTokenSource token = new CancellationTokenSource();
        string hobbit = null;
        public MainWindow()
        {
            InitializeComponent();
            currentMainWindow = this;

            WindowPopUp windowPopOut = new WindowPopUp();
            windowPopOut.ShowDialog();

            if (textFileName != null)
            {
                UploadTextBlock.Text = "The text file " + textFileName.Substring(textFileName.LastIndexOf('\\') + 1) + " is selected.";
                hobbit = File.ReadAllText(textFileName);
            }
            else 
            {
                UploadTextBlock.Text = "Upload the text file in order to ask questions.";
                SendButton.IsEnabled = false;
                QuestionTextBox.IsEnabled = false;
            }

            string downloadUrl = "https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
            string NNFileName = "bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
            neuralNetwork = new NuGetNN.NeuralNetwork(downloadUrl, NNFileName, new FileService());
        }
        private void UploadTextFile_Click(object sender, RoutedEventArgs e)
        {
            new WindowPopUp().ShowDialog();

            if (textFileName != null)
            {
                UploadTextBlock.Text = "The text file " + textFileName.Substring(textFileName.LastIndexOf('\\') + 1) + " is selected.";
                hobbit = File.ReadAllText(textFileName);
                SendButton.IsEnabled = true;
                QuestionTextBox.IsEnabled = true;
            }
            else
            {
                UploadTextBlock.Text = "Upload the text file in order to ask questions.";
                SendButton.IsEnabled = false;
                QuestionTextBox.IsEnabled = false;
            }
        }
        private void QuestionSend_Click(object sender, RoutedEventArgs e)
        {
            string question = QuestionTextBox.Text;
            QuestionTextBox.Clear();
            AddToChat("Your question: " + question);
            SendButton.IsEnabled = false;
            QuestionTextBox.IsEnabled = false;
            if (question.StartsWith("/load"))
            {
                var openFileDialog = new OpenFileDialog()
                {
                    Title = "File",
                    Filter = "Text Document (*.txt) | *.txt",
                    FileName = ""
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    string fileContent = File.ReadAllText(openFileDialog.FileName);
                    AddToChat("NN's answer (from the file): " + fileContent);
                }
            }
            else
            {
                var answer = NuGetNN.NeuralNetwork.NNAnswerAsync(question, hobbit, token.Token).Result;
                if (answer.Contains("system error number 13"))
                {
                    AddToChat("The NN model was not downloaded. Ask again when the download is finished.");
                }
                else
                {
                    AddToChat("NN's answer: " + answer);
                }
            }
            SendButton.IsEnabled = true;
            QuestionTextBox.IsEnabled = true;   
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            token.Cancel();
        }
        private void AddToChat(string message)
        {
            ChatListView.Items.Add(new ListViewItem { Content = message });
        }
        
        public async void DownloadModelFileAsync(string downloadUrl, string NNFileName)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        currentMainWindow.DownloadProgressBar.Value = e.ProgressPercentage;
                        currentMainWindow.DownloadPercentageLabel.Content = e.ProgressPercentage + "%";
                        currentMainWindow.SendButton.IsEnabled = false;
                        currentMainWindow.QuestionTextBox.IsEnabled = false;
                    };

                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        if (e.Error == null)
                        {
                            currentMainWindow.DownloadTextBlock.Text = "The NN model is downloaded successfully.";
                            currentMainWindow.SendButton.IsEnabled = true;
                            currentMainWindow.QuestionTextBox.IsEnabled = true;
                        }
                        else
                        {
                            currentMainWindow.DownloadTextBlock.Text = "Failed to download NN model.";
                        }
                    };
                    await client.DownloadFileTaskAsync(new Uri(downloadUrl), NNFileName);
                }
                catch (Exception ex)
                {
                    currentMainWindow.DownloadTextBlock.Text = "Error downloading neural network: " + ex.Message;
                }
            }
        }
        public class FileService : NuGetNN.IFileServices
        {
            
            public bool Exists(string NNFileName)
            {
                if (!File.Exists(NNFileName))
                {
                    currentMainWindow.DownloadPercentageLabel.Content = "0%";
                    currentMainWindow.DownloadProgressBar.Value = 0;
                    currentMainWindow.DownloadTextBlock.Text = "Send your question to download the NN model.";
                }
                return File.Exists(NNFileName);
            }
            public void DownloadFile(string downloadUrl, string NNFileName)
            {
                try
                {
                    currentMainWindow.SendButton.IsEnabled = false;
                    currentMainWindow.QuestionTextBox.IsEnabled = false;
                    currentMainWindow.DownloadPercentageLabel.Content = "0%";
                    currentMainWindow.DownloadProgressBar.Value = 0;
                    currentMainWindow.DownloadTextBlock.Text = "The NN model is being downloaded now. Please wait.";
                    currentMainWindow.DownloadModelFileAsync(downloadUrl, NNFileName);
                }
                catch (WebException ex)
                {
                    MessageBox.Show("Error downloading neural network: " + ex.Message);
                    // Console.WriteLine("Error downloading neural network: " + ex.Message);
                }
            }
          
        }
    }
}
