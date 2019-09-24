using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Models;
using Newtonsoft.Json;

namespace _365WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SportController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        [Route("Matches")]
        public async Task<IEnumerable<Match>> GetMatchesAsync()
        {
            using (StreamReader r = new StreamReader(@"c:\Matches.txt"))
            {
                string json = await r.ReadToEndAsync();
                return JsonConvert.DeserializeObject<IEnumerable<Match>>(json);
            }
        }
    }
}