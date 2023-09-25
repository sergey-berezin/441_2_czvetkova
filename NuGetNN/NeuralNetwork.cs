using BERTTokenizers;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetNN {

    public interface IFileServices
    {
        bool Exists(string NNFileName);
        void DownloadFile(string downloadUrl, string NNFileName);
    }
    public class NeuralNetwork
    {
        private string downloadUrl;
        private string NNFileName;
        private IFileServices fileServices;
        private string error;
        public NeuralNetwork(string downloadUrl, string NNFileName, IFileServices fileServices)
        {
            this.downloadUrl = downloadUrl;
            this.NNFileName = NNFileName;
            this.fileServices = fileServices;
        }
        public string DownloadNN()
        {
            error = "empty";
            int maxAttempts = 3;
            int currentAttempt = 0;
            while (!fileServices.Exists(NNFileName) && currentAttempt < maxAttempts)
            {
                fileServices.DownloadFile(downloadUrl, NNFileName);
                currentAttempt++;
            }
            if (!fileServices.Exists(NNFileName))
            {
                error = "Failed to download neural network after multiple attempts.";
            }   
            return error;
        }
        public string NNAnswer(string question, string hobbit)
        {
            var sentence = ("{\"question\": \"@QSTN\", \"context\": \"@CTX\"}".Replace("@CTX", hobbit)).Replace("@QSTN", question);
            // Console.WriteLine(sentence);

            // Create Tokenizer and tokenize the sentence.
            var tokenizer = new BertUncasedLargeTokenizer();

            // Get the sentence tokens.
            var tokens = tokenizer.Tokenize(sentence);

            // Encode the sentence and pass in the count of the tokens in the sentence.
            var encoded = tokenizer.Encode(tokens.Count(), sentence);

            // Break out encoding to InputIds, AttentionMask and TypeIds from list of (input_id, attention_mask, type_id).
            var bertInput = new BertInput()
            {
                InputIds = encoded.Select(t => t.InputIds).ToArray(),
                AttentionMask = encoded.Select(t => t.AttentionMask).ToArray(),
                TypeIds = encoded.Select(t => t.TokenTypeIds).ToArray(),
            };
            
            // Get path to model to create inference session.
            var modelPath = "bert-large-uncased-whole-word-masking-finetuned-squad.onnx";

            // Create input tensor.
            var input_ids = ConvertToTensor(bertInput.InputIds, bertInput.InputIds.Length);
            var attention_mask = ConvertToTensor(bertInput.AttentionMask, bertInput.InputIds.Length);
            var token_type_ids = ConvertToTensor(bertInput.TypeIds, bertInput.InputIds.Length);
        
            // Create input data for session.
            var input = new List<NamedOnnxValue>() { NamedOnnxValue.CreateFromTensor("input_ids", input_ids), 
                                                    NamedOnnxValue.CreateFromTensor("input_mask", attention_mask), 
                                                    NamedOnnxValue.CreateFromTensor("segment_ids", token_type_ids) };

            // Create an InferenceSession from the Model Path.
            var session = new InferenceSession(modelPath);
        
            // Run session and send the input data in to get inference output. 
            var output = session.Run(input);

            // Call ToList on the output.
            // Get the First and Last item in the list.
            // Get the Value of the item and cast as IEnumerable<float> to get a list result.
            List<float> startLogits = (output.ToList().First().Value as IEnumerable<float>).ToList();
            List<float> endLogits = (output.ToList().Last().Value as IEnumerable<float>).ToList();

            // Get the Index of the Max value from the output lists.
            var startIndex = startLogits.ToList().IndexOf(startLogits.Max()); 
            var endIndex = endLogits.ToList().IndexOf(endLogits.Max());

            // From the list of the original tokens in the sentence
            // Get the tokens between the startIndex and endIndex and convert to the vocabulary from the ID of the token.
            var predictedTokens = tokens
                        .Skip(startIndex)
                        .Take(endIndex + 1 - startIndex)
                        .Select(o => tokenizer.IdToToken((int)o.VocabularyIndex))
                        .ToList();

            string answer = String.Join(" ", predictedTokens);
            return answer;
        }
        public static Tensor<long> ConvertToTensor(long[] inputArray, int inputDimension)
        {
             // Create a tensor with the shape the model is expecting. Here we are sending in 1 batch with the inputDimension as the amount of tokens.
            Tensor<long> input = new DenseTensor<long>(new[] { 1, inputDimension });

            // Loop through the inputArray (InputIds, AttentionMask and TypeIds)
            for (var i = 0; i < inputArray.Length; i++)
            {
                // Add each to the input Tenor result.
                // Set index and array value of each input Tensor.
                input[0,i] = inputArray[i];
            }
            return input;
        }
    }
    
    public class BertInput
    {
        public long[] InputIds { get; set; }
        public long[] AttentionMask { get; set; }
        public long[] TypeIds { get; set; }
    }
}
