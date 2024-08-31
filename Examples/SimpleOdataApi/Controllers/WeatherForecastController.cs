using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleOdataApi.Controllers
{
    [Route("odata")]
    public class WeatherForecastController() : ODataController
    {
        private static readonly string[] Summaries =
        [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

        [HttpGet("WeatherForecasts")]
        public IEnumerable<WeatherForecast> GetForecasts()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Id = index,
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)],
            })
            .ToArray();
        }

        [HttpGet("WeatherForecasts({id})")]
        public WeatherForecast? GetForecastById(int id)
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Id = index,
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)],
            })
             .FirstOrDefault(w => w.Id == id);
        }
    }
}
