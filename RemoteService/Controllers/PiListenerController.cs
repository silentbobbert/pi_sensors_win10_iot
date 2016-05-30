using System.Threading.Tasks;
using System.Web.Http;
using RemoteService.Attributes;
using RemoteService.ViewModels;

namespace RemoteService.Controllers
{
    [RoutePrefix("api/PiListener")]
    [JsonFormatter]
    public class PiListenerController : ApiController
    {
        // GET api/values
        [HttpGet, Route("ping"), Route("")]
        public async Task<IHttpActionResult> Ping()
        {
            return Ok();
        }

        // GET api/PiListener/5
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> Get(int id)
        {
            return Ok("value");
        }

        // POST api/PiListener
        [HttpPost, Route("message")]
        public async Task<IHttpActionResult> Post([FromBody]SimpleDTO value)
        {
            return Ok();
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
