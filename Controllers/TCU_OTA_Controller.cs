using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SUMS_Agent.Models;
using ComposeBuilderDotNet.Builders;
using ComposeBuilderDotNet.Enums;
using ComposeBuilderDotNet.Extensions;
using System.Security.Claims;
using Newtonsoft.Json.Linq;

namespace SUMS_Agent.Controllers
{
    [ApiController]
    [Route("OTA/TCU")]
    public class TcuOTAController : ControllerBase
    {

        private readonly ILogger<TcuOTAController> _logger;
        private readonly TCUContext _tcuContext;
        public TcuOTAController(ILogger<TcuOTAController> logger, TCUContext tcuContext)
        {
            _logger = logger;
            _tcuContext = tcuContext;
        }

        [HttpGet("features")]
        [Authorize(Policy = "TCUOnly")]
        public IActionResult ViewFeatures()
        {
            // Get the TCU claims from the current TCU principal
            var claimsIdentity = User.Identity as ClaimsIdentity;
            // TCU is authorized, extract the TCU identifier (Mac address)
            string? tcuMAC = claimsIdentity?.Name;

            if (tcuMAC == null)
                return Unauthorized();

            Tcu? tcu = (from _tcu in _tcuContext.Tcus
                        where _tcu.Mac == tcuMAC
                        select _tcu).FirstOrDefault();
            
            if (tcu == null)
                return NotFound();

            var features = (from _TcuFeature in _tcuContext.Tcufeatures
                            where _TcuFeature.TcuId == tcu.TcuId
                            join _feature in _tcuContext.Features
                            on _TcuFeature.FeatureId equals _feature.FeatureId
                            select _feature).ToList();

            var TcuFeatures = (from _TcuFeature in _tcuContext.Tcufeatures
                                where _TcuFeature.TcuId == tcu.TcuId
                                select _TcuFeature).ToList();

            bool shallUpdate = (from _feature in TcuFeatures
                                where _feature.IsActive == false
                                && _feature.IsUptoDate == false
                                select _feature).Any();

            if (shallUpdate == false)
                return NoContent();
            
            // get the features related to model
            var featuresInfo = (from _modelFeature in _tcuContext.ModelsFeatures
                                where _modelFeature.ModelId == tcu.ModelId
                                join _feature in _tcuContext.Features
                                on _modelFeature.FeatureId equals _feature.FeatureId
                                join _app in _tcuContext.Apps
                                on _feature.AppId equals _app.AppId
                                select
                                Builder.MakeService(_app.Repo)
                                .WithContainerName(_app.Repo)
                                .WithImage("vehicleplus.cloud/" + _app.Repo + ":" + _app.Tag)
                                .WithProperty("privileged", true)
                                .WithRestartPolicy(ERestartMode.Always)
                                .WithEnvironment(_app.EnvVariables)
                                .WithPortMapping(_app.ExposedPorts)
                                .WithVolumes(_app.Volumes)
                                .Build()).ToArray();

            var dockerCompose = Builder.MakeCompose()
                .WithVersion("3")
                .WithServices(featuresInfo)
                .Build();

            var result = dockerCompose.Serialize();

            var byteArray = System.Text.Encoding.UTF8.GetBytes(result);

            _tcuContext.SaveChanges();
            return File(byteArray, "text/yaml", "docker-compose.yml");
        }


        [HttpGet("features/images")]
        [Authorize(Policy = "TCUOnly")]
        public IActionResult GetImagesToUpdate()
        {
            // Get the TCU claims from the current TCU principal
            var claimsIdentity = User.Identity as ClaimsIdentity;
            // TCU is authorized, extract the TCU identifier (Mac address)
            string? tcuMAC = claimsIdentity?.Name;

            if (tcuMAC == null)
                return Unauthorized();

            Tcu? tcu = (from _tcu in _tcuContext.Tcus
                        where _tcu.Mac == tcuMAC
                        select _tcu).FirstOrDefault();

            if (tcu == null)
                return NotFound();

            var features = (from _TcuFeature in _tcuContext.Tcufeatures
                            where _TcuFeature.TcuId == tcu.TcuId
                            && (_TcuFeature.IsUptoDate == false || _TcuFeature.IsActive == false)
                            join _feature in _tcuContext.Features
                            on _TcuFeature.FeatureId equals _feature.FeatureId
                            select _feature.AppId).ToList();

            var TcuFeaturesApps = (from _app in _tcuContext.Apps
                               where features.Contains(_app.AppId)
                               select "vehicleplus.cloud/" + _app.Repo + ":" + _app.Tag).ToList();


            return Ok(new JObject
            {
                new JProperty("images", TcuFeaturesApps)
            });
        }


        [HttpPut("features/ACK")]
        [Authorize(Policy = "TCUOnly")]
        public IActionResult AcknowledgeUpdate([FromBody] JObject featureData)
        {
            // Get the TCU claims from the current TCU principal
            var claimsIdentity = User.Identity as ClaimsIdentity;
            // TCU is authorized, extract the TCU identifier (Mac address)
            string? tcuMAC = claimsIdentity?.Name;

            if (tcuMAC == null)
                return Unauthorized();

            Tcu? tcu = (from _tcu in _tcuContext.Tcus
                        where _tcu.Mac == tcuMAC
                        select _tcu).FirstOrDefault();

            if (tcu == null)
                return NotFound();

            var featureId = featureData["id"]?.Value<long>();

            var action = featureData["action"]?.Value<int>();

            if (featureId == null || action == null)
                return BadRequest();

            var tcuFeature = (from _tcuFeature in _tcuContext.Tcufeatures
                              where _tcuFeature.FeatureId == featureId
                              && _tcuFeature.TcuId == tcu.TcuId
                              select _tcuFeature).FirstOrDefault();

            if (tcuFeature == null)
                return NotFound();

            switch (action)
            {
                case 0:
                    tcuFeature.IsActive = true;
                    tcuFeature.IsUptoDate = true;
                    break;
                case 1:
                    tcuFeature.IsUptoDate = true;
                    break;
                default:
                    return BadRequest();
            }

            _tcuContext.SaveChanges();
            
            return Ok();
        }

    }
}