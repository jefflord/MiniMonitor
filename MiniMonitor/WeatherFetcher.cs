using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MiniMonitor
{

    public class WeatherFetcher
    {
        private const string WeatherBaseUrl = "https://api.openweathermap.org/data/2.5/weather";
        private const string GeoBaseUrl = "https://api.openweathermap.org/geo/1.0/zip";

        private string ApiKey;

        private CityInfo location;

        public WeatherFetcher(string api)
        {
            ApiKey = api;
        }

        public async Task<WeatherData> GetCurrentWeather(string zip)
        {
            using var client = new HttpClient();
            //http://api.openweathermap.org/geo/1.0/zip&appid={API key}

            try
            {
                if (location == null)
                {
                    await GetLocation(zip, client);
                }

                var url = $"{WeatherBaseUrl}?lat={location.Lat}&lon={location.Lon}&appid={ApiKey}&units=imperial";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Throw exception for non-2xx status codes

                var weatherData = await response.Content.ReadFromJsonAsync<OpenWeatherMapResponse>();
                return new WeatherData
                {
                    DataType = "WeatherData",
                    City = weatherData.Main.City,
                    Temperature = weatherData.Main.Temp,
                    Description = weatherData.Weather[0].Description
                };
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP errors (e.g., network issues)
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        private async Task GetLocation(string zip, HttpClient client)
        {
            var country = "US";
            var url = $"{GeoBaseUrl}?zip={zip},{country}&appid={ApiKey}"; // Build URL with parameters
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode(); // Throw exception for non-2xx status codes
            location = await response.Content.ReadFromJsonAsync<CityInfo>();
        }
    }

    public class OpenWeatherMapResponse
    {
        public Main Main { get; set; }
        public List<Weather> Weather { get; set; }
    }

    public class CityInfo
    {
        public string Zip { get; set; }
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Country { get; set; }
    }

    public class Weather
    {
        public string Description { get; set; }
    }

    public class Main
    {
        public string City { get; set; }
        public double Temp { get; set; }
    }

    public class WeatherData
    {
        public string DataType { get; set; }
        public string City { get; set; }
        public double Temperature { get; set; }
        public string Description { get; set; }
    }
}
