using Microsoft.AspNetCore.Mvc;
using System;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "de pé";
        }

        // GET api/values
        [HttpGet("retry")]
        public ActionResult Retry(int id)
        {
            if (id == 5)
                return Ok();

            throw new Exception();
        }
    }
}
