using Microsoft.AspNetCore.Mvc;
using Webscraper.AJVKT.API.Service;

namespace Webscraper.AJVKT.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LeviorProductController : ControllerBase
    {
        public readonly IWebsiteScrapingService _websiteScrapingService;
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<LeviorProductController> _logger;

        public LeviorProductController(ILogger<LeviorProductController> logger, IWebsiteScrapingService websiteScrapingService)
        {
            _logger = logger;
            _websiteScrapingService = websiteScrapingService;
        }

        [HttpPost(Name = "GetAJVKT Levior Products")]
        public async Task<IActionResult> GenerateExcelFileAsync()
        {
            try
            {
                await _websiteScrapingService.ScrapeWebsite();
                return Ok("AJVKT CSV was successfully generated");
            }
            catch(Exception ex)
            {
                _logger.LogError($"Exception: {ex}");
                return BadRequest("Invalid Request");
            }
        }


        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}