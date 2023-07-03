using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SUMS_Agent.Data;
using SUMS_Agent.Models;

namespace SUMS_Agent.Controllers
{
    [ApiController]
    [Route("OTA")]
    public class OTAController : ControllerBase
    {

        private readonly ILogger<OTAController> _logger;
        private readonly TCUContext _tcuContext;
        public OTAController(ILogger<OTAController> logger, TCUContext tcuContext)
        {
            _logger = logger;
            _tcuContext = tcuContext;
        }

        [HttpPost("notify")]
        [AllowAnonymous]
        public IActionResult NotifyUpdate(JObject input)
        {

            List<RegistryEvent> events = RegistryEvent.ParseNotification(input, _tcuContext);
            foreach (RegistryEvent registryEvent in events)
            {
                Console.WriteLine(registryEvent.ToString());
                switch (registryEvent.action)
                {
                    case "push":
                        App newApp = new()
                        {
                            Repo = registryEvent.appName,
                            Tag = registryEvent.versionTag,
                            HexDigest = registryEvent.appHexDigest,
                            ReleaseDate = registryEvent.timeStamp,
                            LatestUpdate = registryEvent.timeStamp
                        };
                        _tcuContext.Add(newApp);
                        _tcuContext.SaveChanges();
                        return Ok();
                    case "update":
                        App? updatedApp = (from _app in _tcuContext.Apps
                                           where _app.Repo == registryEvent.appName
                                           && _app.Tag == registryEvent.versionTag
                                           select _app).FirstOrDefault();
                        if (updatedApp == null)
                            return BadRequest();
                        updatedApp.LatestUpdate = registryEvent.timeStamp;
                        _tcuContext.SaveChanges();
                        return Ok();
                }
            }
            return Ok();
        }
    }
}