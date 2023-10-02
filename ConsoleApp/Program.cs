using BERTTokenizers;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGetNN;

namespace ConsoleApp
{
    internal class Program
    {
        public class FileService : NuGetNN.IFileServices
        {
            public bool Exists(string NNFileName)
            {
                return File.Exists(NNFileName);
            }
            public void DownloadFile(string downloadUrl, string NNFileName)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                       client.DownloadFile(downloadUrl, NNFileName);
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine("Error downloading neural network: " + ex.Message);
                }
            }
        }
        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Please enter the name of the text file as an argument.");
                return;
            }
            string textFileName = args[0];
            if (!File.Exists(textFileName))
            {
                System.Console.WriteLine("The text file does not exist.");
                return;
            }
            var hobbit = File.ReadAllText(textFileName);

            string downloadUrl = "https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
            string NNFileName = "bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
            var neuralNetwork = new NuGetNN.NeuralNetwork(downloadUrl, NNFileName, new FileService());

            var tasks = new List<Task>();
            var token = new CancellationTokenSource(); 
            Console.Write("Ask a question: ");
            string question = Console.ReadLine();
            
            while (question != "")
            { 
                // NN work + print the result
                var task = NeuralNetwork.NNAnswerAsync(question, hobbit, token.Token).ContinueWith(x => {
                    Console.WriteLine("\nYour question: {0}", question);
                    Console.WriteLine("NN's answer: {0}", x.Result);
                });
                tasks.Add(task);

                question = Console.ReadLine();  
            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error waiting tasks: " + ex.Message);
            }
        }
    }
}
