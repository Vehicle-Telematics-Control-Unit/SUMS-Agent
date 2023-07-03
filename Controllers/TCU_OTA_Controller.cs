using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SUMS_Agent.Models;
using ComposeBuilderDotNet.Builders;
using ComposeBuilderDotNet.Enums;
using ComposeBuilderDotNet.Extensions;
using System.Security.Claims;

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
        [AllowAnonymous]
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
            
            return File(byteArray, "text/yaml", "docker-compose.yml");
        }
    }
}