using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestApi.Options;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class TestApiController : ControllerBase
    {
        private readonly IOptionsMonitor<TestApiOptions> _options;

        public TestApiController(IOptionsMonitor<TestApiOptions> options)
        {
            _options = options;
        }

        [HttpGet]
        public TestApiOptions Get()
        {
            return _options.CurrentValue;
        }
    }
}
