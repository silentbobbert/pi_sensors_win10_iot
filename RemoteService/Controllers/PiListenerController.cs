using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RemoteService.DTOs;

namespace RemoteService.Controllers
{
    [Route("api/[controller]")]
    public class PiListenerController : Controller
    {
        // GET api/PiListener
        [HttpGet(""), HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            return Ok();
        }

        // GET api/PiListener/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            return Ok("value");
        }

        // POST api/PiListener
        [HttpPost("message")]
        public async Task<IActionResult> Post([FromBody]SimpleDTO value)
        {
            return Ok();
        }

        // PUT api/PiListener/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody]string value)
        {
            return Ok();
        }

        // DELETE api/PiListener/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok();
        }
    }
}
