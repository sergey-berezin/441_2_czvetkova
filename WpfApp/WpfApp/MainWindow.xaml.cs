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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Markup;
using System.Reflection;

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
        public string jsonDataFileName = "data.json";
        public string jsonHistoryFileName = "history.json";
        public string jsonDataBackupFileName = "data_backup.json";
        public string jsonHistoryBackupFileName = "history_backup.json";
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
                AddToChat("Text:\n" + hobbit);
            }
            else 
            {
                UploadTextBlock.Text = "Upload the text file in order to ask questions.";
                SendButton.IsEnabled = false;
                QuestionTextBox.IsEnabled = false;
            }

            if (File.Exists(jsonHistoryFileName))
            {
                var json = JToken.Parse(File.ReadAllText(jsonHistoryFileName));
                int index = -1;
                for (int i = 0; i < json.Count(); i++)
                {
                    if (json[i]["TextName"].ToString() == textFileName)
                        index = i;
                }
                if (index != -1)
                {
                    for (int i = 0; i < json[index]["TextAskedQuestions"].Count(); i++)
                    {
                        AddToChat("Question: " + json[index]["TextAskedQuestions"][i]["Question"].ToString());
                        AddToChat("NN's answer: " + json[index]["TextAskedQuestions"][i]["Answer"].ToString());
                    }
                }
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
                AddToChat("Text:\n" + hobbit);
                SendButton.IsEnabled = true;
                QuestionTextBox.IsEnabled = true;
                if (File.Exists(jsonHistoryFileName))
                {
                    var json = JToken.Parse(File.ReadAllText(jsonHistoryFileName));
                    int index = -1;
                    for (int i = 0; i < json.Count(); i++)
                    {
                        if (json[i]["TextName"].ToString() == textFileName)
                            index = i;
                    }
                    if (index != -1)
                    {
                        for (int i = 0; i < json[index]["TextAskedQuestions"].Count(); i++)
                        {
                            AddToChat("Question: " + json[index]["TextAskedQuestions"][i]["Question"].ToString());
                            AddToChat("NN's answer: " + json[index]["TextAskedQuestions"][i]["Answer"].ToString());
                        }
                    }
                }
            }
            else
            {
                UploadTextBlock.Text = "Upload the text file in order to ask questions.";
                SendButton.IsEnabled = false;
                QuestionTextBox.IsEnabled = false;
            }
        }
        private async void QuestionSend_Click(object sender, RoutedEventArgs e)
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
                try 
                { 
                    string jsonAnswer = null;
                    question = question.Replace("?", " ").Trim().ToLower();
                    if (File.Exists(jsonDataFileName))
                    {
                        File.Copy(jsonDataFileName, jsonDataBackupFileName);
                        File.Copy(jsonHistoryFileName, jsonHistoryBackupFileName);
                        var json = JToken.Parse(File.ReadAllText(jsonDataFileName));
                        int index = -1;
                        for (int i = 0; i < json.Count(); i++)
                        {
                            if (json[i]["TextName"].ToString() == textFileName)
                                index = i;
                        }
                        if (index != -1)
                        {
                            for (int i = 0; i < json[index]["TextAskedQuestions"].Count(); i++)
                            {
                                if (json[index]["TextAskedQuestions"][i]["Question"].ToString() == question)
                                {
                                    jsonAnswer = json[index]["TextAskedQuestions"][i]["Answer"].ToString();
                                }
                            }
                        }
                    }
                    if (jsonAnswer != null)
                    {
                        var jsonHistoryList = new List<Text>();
                        if (File.Exists(jsonHistoryFileName))
                        {
                            var jsonData = File.ReadAllText(jsonHistoryFileName);
                            jsonHistoryList = JsonConvert.DeserializeObject<List<Text>>(jsonData);
                        }
                        var newData = new AskedQuestions()
                        {
                            Question = question,
                            Answer = jsonAnswer
                        };
                        int index = -1;
                        for (int i = 0; i < jsonHistoryList.Count; i++)
                        {
                            if (jsonHistoryList[i].TextName == textFileName)
                                index = i;
                        }
                        if (index == -1)
                        {
                            var curText = new Text();
                            curText.TextName = textFileName;
                            curText.TextAskedQuestions = new List<AskedQuestions>();
                            index = jsonHistoryList.Count;
                            jsonHistoryList.Add(curText);
                        }
                        jsonHistoryList[index].TextAskedQuestions.Add(newData);
                        var newJsonData = JsonConvert.SerializeObject(jsonHistoryList);
                        File.WriteAllText(jsonHistoryFileName, newJsonData);
                        AddToChat("NN's answer (from json): " + jsonAnswer);
                    }
                    else
                    {
                        var answer = await NuGetNN.NeuralNetwork.NNAnswerAsync(question, hobbit, token.Token);
                        if (answer == null)
                        {
                            AddToChat("The NN model was not downloaded. Please ask again.");
                        }
                        else
                        {
                            var newData = new AskedQuestions()
                            {
                                Question = question,
                                Answer = answer
                            };

                            var jsonDataList = new List<Text>();
                            if (File.Exists(jsonHistoryFileName))
                            {
                                var jsonData = File.ReadAllText(jsonDataFileName);
                                jsonDataList = JsonConvert.DeserializeObject<List<Text>>(jsonData);
                            }
                            int index = -1;
                            for (int i = 0; i < jsonDataList.Count; i++)
                            {
                                if (jsonDataList[i].TextName == textFileName)
                                    index = i;
                            }
                            if (index == -1)
                            {
                                var curText = new Text();
                                curText.TextName = textFileName;
                                curText.TextAskedQuestions = new List<AskedQuestions>();
                                index = jsonDataList.Count;
                                jsonDataList.Add(curText);
                            }
                            jsonDataList[index].TextAskedQuestions.Add(newData);
                            var newJsonData = JsonConvert.SerializeObject(jsonDataList);
                            File.WriteAllText(jsonDataFileName, newJsonData);

                            var jsonHistoryList = new List<Text>();
                            if (File.Exists(jsonHistoryFileName))
                            {
                                var jsonData = File.ReadAllText(jsonHistoryFileName);
                                jsonHistoryList = JsonConvert.DeserializeObject<List<Text>>(jsonData);
                            }
                            index = -1;
                            for (int i = 0; i < jsonHistoryList.Count; i++)
                            {
                                if (jsonHistoryList[i].TextName == textFileName)
                                    index = i;
                            }
                            if (index == -1)
                            {
                                var curText = new Text();
                                curText.TextName = textFileName;
                                curText.TextAskedQuestions = new List<AskedQuestions>();
                                index = jsonHistoryList.Count;
                                jsonHistoryList.Add(curText);
                            }
                            jsonHistoryList[index].TextAskedQuestions.Add(newData);
                            newJsonData = JsonConvert.SerializeObject(jsonHistoryList);
                            File.WriteAllText(jsonHistoryFileName, newJsonData);

                            AddToChat("NN's answer: " + answer);
                        }
                    }
                }
                catch (Exception ex)
                {
                    File.Copy(jsonDataBackupFileName, jsonDataFileName);
                    File.Copy(jsonHistoryBackupFileName, jsonHistoryFileName);
                    MessageBox.Show($"Error while updating json: {ex.Message}");
                }
                File.Delete(jsonDataBackupFileName);
                File.Delete(jsonHistoryBackupFileName);
            }
            SendButton.IsEnabled = true;
            QuestionTextBox.IsEnabled = true;   
        }
        private void ClearChatHistory_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(jsonHistoryFileName))
            {
                File.Delete(jsonHistoryFileName);
                File.Delete(jsonDataFileName);
                ChatListView.Items.Clear();
                if (hobbit !=  null)
                {
                    AddToChat("Text:\n" + hobbit);
                }
                ChatScrollViewer.ScrollToBottom();
            }

        }
        public class Text
        {
            public string TextName { get; set; }
            public List<AskedQuestions> TextAskedQuestions;
        }
        public class AskedQuestions
        {
            public string Question { get; set; }

            public string Answer { get; set; }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            token.Cancel();
        }
        private void AddToChat(string message)
        {
            ChatListView.Items.Add(new ListViewItem { Content = message });
            ChatScrollViewer.ScrollToBottom();
        }
        
        public async Task DownloadModelFileAsync(string downloadUrl, string NNFileName)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                   
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        currentMainWindow.Dispatcher.BeginInvoke(() => 
                        {
                            currentMainWindow.DownloadProgressBar.Value = e.ProgressPercentage;
                            currentMainWindow.DownloadPercentageLabel.Content = e.ProgressPercentage + "%";
                            currentMainWindow.SendButton.IsEnabled = false;
                            currentMainWindow.QuestionTextBox.IsEnabled = false;
                        });
                    };

                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        currentMainWindow.Dispatcher.BeginInvoke(() =>
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
                        });
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
            public async Task DownloadFile(string downloadUrl, string NNFileName)
            {
                try
                {
                    currentMainWindow.SendButton.IsEnabled = false;
                    currentMainWindow.QuestionTextBox.IsEnabled = false;
                    currentMainWindow.DownloadPercentageLabel.Content = "0%";
                    currentMainWindow.DownloadProgressBar.Value = 0;
                    currentMainWindow.DownloadTextBlock.Text = "The NN model is being downloaded now. Please wait.";
                    await currentMainWindow.DownloadModelFileAsync(downloadUrl, NNFileName);
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
