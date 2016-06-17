using System.Threading.Tasks;
using System.Web.Http;
using HeadlessRobot.DTOs;

namespace RemoteService.Controllers
{
    [RoutePrefix("api/PiListener")]
    public class PiListenerController : ApiController
    {
        // GET api/PiListener
        [HttpGet, Route(""), Route("ping")]
        public async Task<IHttpActionResult> Ping()
        {
            return Ok();
        }

        // GET api/PiListener/5
        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> Get(int id)
        {
            return Ok(new { Message = $"Hello World, you entered {id}" });
        }

        // POST api/PiListener
        [HttpPost, Route("message")]
        public async Task<IHttpActionResult> Post([FromBody]DataChanged value)
        {
            return Ok(new { Message = $"Your message was received."});
        }

        // PUT api/PiListener/5
        [HttpPut, Route("{id:int}")]
        public async Task<IHttpActionResult> Put(int id, [FromBody]string value)
        {
            return Ok();
        }

        // DELETE api/PiListener/5
        [HttpDelete, Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            return Ok();
        }
    }
}
