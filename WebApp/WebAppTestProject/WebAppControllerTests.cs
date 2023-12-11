using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace WebAppTestProject
{
    public class WebAppControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> factory;
        private string text = "Test Text";
        public WebAppControllerTests(WebApplicationFactory<Program> factory)
        {
            this.factory = factory;
            _client = factory.CreateClient(); 
        }

        [Fact]
        public async Task Post_ReturnsUniqueId()
        {
            var response = await _client.PostAsJsonAsync("http://localhost:5247/api/WebApp", text);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(responseContent));
        }

        [Fact]
        public async Task Get_ReturnsAnswer()
        {
            var postResponse = await _client.PostAsJsonAsync("http://localhost:5247/api/WebApp", text);
            string uniqueId = await postResponse.Content.ReadAsStringAsync();
            var response = await _client.GetAsync($"http://localhost:5247/api/WebApp?textId={uniqueId}&question=Test Question");

            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }

        [Fact]
        public async Task Get_ReturnsNotFound()
        {
            var response = await _client.GetAsync("http://localhost:5247/api/WebApp?textId=InvalidId&question=Test Question");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
