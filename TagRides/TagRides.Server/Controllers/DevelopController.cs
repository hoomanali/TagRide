using System.Collections.Generic;
using Google.Maps;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TagRides.Server.Controllers
{
    [Route("api/[controller]")]
    public class DevelopController : Controller
    {
        // GET api/develop/errors/get
        [HttpGet("errors/get")]
        public IEnumerable<string> GetErrors()
        {
            return Program.GetErrorMessages();
        }

        // GET api/develop/errors/get-and-reset
        [HttpGet("errors/get-and-reset")]
        public IEnumerable<string> GetAndResetErrors()
        {
            return Program.GetAndResetErrorMessages();
        }

        // POST api/develop/gmaps/api-key
        [HttpPost("gmaps/api-key/{apiKey}")]
        public void PostGoogleMapsApiKey(string apiKey)
        {
            GoogleSigned.AssignAllServices(new GoogleSigned(apiKey));
        }
    }
}
