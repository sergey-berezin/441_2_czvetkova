using Microsoft.AspNetCore.Mvc;
using WebApp;
using NuGetNN;
using System.Net;
using System.Text;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebAppController : ControllerBase
    {
        public static Dictionary<string, string> TextStorage = new Dictionary<string, string>();
        public CancellationTokenSource token = new CancellationTokenSource();
        public string downloadUrl = "https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
        public string NNFileName = "bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
        public static NeuralNetwork neuralNetwork = null;
        public WebAppController()
        {
            neuralNetwork = new NeuralNetwork(downloadUrl, NNFileName, new FileService());
        }
       
        // POST api/<ValuesController>
        [HttpPost]
        public ActionResult<string> Post([FromBody] string text)
        {      
            string uniqueIdentifier = Guid.NewGuid().ToString();

            TextStorage.Add(uniqueIdentifier, text);
            return Ok(uniqueIdentifier);
        }

        [HttpGet]
        public async Task<IActionResult> Get(string textId, [FromQuery] string question)
        {
            if (!TextStorage.ContainsKey(textId))
            {
                return NotFound();
            }
            string text = TextStorage[textId];

            var answer = await NeuralNetwork.NNAnswerAsync(text, question, token.Token);
            if (answer == null)
            {
                return Ok("The NN model was not downloaded. Please ask again.");
            }
            return Ok(answer.ToString());
        }
    }
    public class FileService : NuGetNN.IFileServices
    {

        public bool Exists(string NNFileName)
        {
            return File.Exists(NNFileName);
        }
        public async Task DownloadFile(string downloadUrl, string NNFileName)
        {
            try
            {
                using (WebClient client = new WebClient())
                    await client.DownloadFileTaskAsync(new Uri(downloadUrl), NNFileName);
            }
            catch (WebException ex)
            {
                Console.WriteLine("Error downloading neural network: " + ex.Message);
            }
        }

    }
}





