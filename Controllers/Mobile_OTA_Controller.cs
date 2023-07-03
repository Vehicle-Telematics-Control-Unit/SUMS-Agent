using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SUMS_Agent.Data;
using SUMS_Agent.Models;

namespace SUMS_Agent.Controllers
{
    [ApiController]
    [Route("OTA/mobile")]
    public class MobileOTAController : ControllerBase
    {

        private readonly ILogger<MobileOTAController> _logger;
        private readonly TCUContext _tcuContext;
        public MobileOTAController(ILogger<MobileOTAController> logger, TCUContext tcuContext)
        {
            _logger = logger;
            _tcuContext = tcuContext;
        }

        [HttpGet("features")]
        [Authorize(Policy = "MobileOnly")]
        public IActionResult ViewFeatures()
        {

            string? deviceId = (from _claim in User.Claims
                                where _claim.Type == "deviceId"
                                select _claim.Value).FirstOrDefault();

            if (deviceId == null)
                return Unauthorized();

            var device = (from _device in _tcuContext.Devices
                          where _device.DeviceId == deviceId
                          select _device).FirstOrDefault();

            if (device == null)
                return Unauthorized();

            // get the TCU model
            long? tcuId = (from _deviceTCU in _tcuContext.DevicesTcus
                           where _deviceTCU.DeviceId == device.DeviceId
                           select _deviceTCU.TcuId).FirstOrDefault();

            if (tcuId == null)
                return NotFound();

            // get the tcu model Id
            Tcu tcu = (from _tcu in _tcuContext.Tcus
                            where _tcu.TcuId == tcuId
                            select _tcu).First();
            var features = (from _TcuFeature in _tcuContext.Tcufeatures
                            where _TcuFeature.TcuId == tcuId
                            join _feature in _tcuContext.Features
                            on _TcuFeature.FeatureId equals _feature.FeatureId
                            select _feature).ToList(); 
            // get the features related to model
            var featuresInfo = (from _modelFeature in _tcuContext.ModelsFeatures
                                where _modelFeature.ModelId == tcu.ModelId
                                join _feature in _tcuContext.Features
                                on _modelFeature.FeatureId equals _feature.FeatureId
                                select new JObject
                                {
                                    new JProperty("id", _feature.FeatureId),
                                    new JProperty("name", _feature.FeatureName),
                                    new JProperty("description", _feature.Description),
                                    new JProperty("isActive", features.Contains(_feature))
                            }).ToList();

            return Ok(featuresInfo);
        }

        [HttpPut("features")]
        [Authorize(Policy = "MobileOnly")]
        public IActionResult ActivateFeature([FromBody] FeatureCommand featureCommand)
        {
            string? deviceId = (from _claim in User.Claims
                                where _claim.Type == "deviceId"
                                select _claim.Value).FirstOrDefault();

            if (deviceId == null)
                return Unauthorized();

            var device = (from _device in _tcuContext.Devices
                          where _device.DeviceId == deviceId
                          select _device).FirstOrDefault();

            if (device == null)
                return Unauthorized();

            // get the TCU model
            long? tcuId = (from _deviceTCU in _tcuContext.DevicesTcus
                           where _deviceTCU.DeviceId == device.DeviceId
                           select _deviceTCU.TcuId).FirstOrDefault();

            if (tcuId == null)
                return NotFound();

            long modelId = (from _tcu in _tcuContext.Tcus
                            where _tcu.TcuId == tcuId
                            select _tcu.ModelId).First();
            
            var featureId = featureCommand.FeatureId;

            if (featureId == null)
                return BadRequest();

            bool isComapatible = (from _ModelFeature in _tcuContext.ModelsFeatures
                                  where _ModelFeature.ModelId == modelId
                                  && _ModelFeature.FeatureId == featureId
                                  select _ModelFeature).Any();

            if(isComapatible == false)
            {
                return Ok(new
                {
                    code = "This feature is not compitable with your vehicle"
                });
            }

            _tcuContext.Tcufeatures.Add(new Tcufeature
            {
                FeatureId = (long)featureId,
                TcuId = (long)tcuId
            });

            _tcuContext.SaveChanges();
            return Ok(new
            {
                code = "Feature will be installed on the next vehicle wake-up"
            });
        }

        [HttpDelete("features")]
        [Authorize(Policy = "MobileOnly")]
        public IActionResult DeactivateFeature([FromBody] FeatureCommand featureCommand)
        {
            string? deviceId = (from _claim in User.Claims
                                where _claim.Type == "deviceId"
                                select _claim.Value).FirstOrDefault();

            if (deviceId == null)
                return Unauthorized();

            var device = (from _device in _tcuContext.Devices
                          where _device.DeviceId == deviceId
                          select _device).FirstOrDefault();

            if (device == null)
                return Unauthorized();

            // get the TCU model
            long? tcuId = (from _deviceTCU in _tcuContext.DevicesTcus
                           where _deviceTCU.DeviceId == device.DeviceId
                           select _deviceTCU.TcuId).FirstOrDefault();

            if (tcuId == null)
                return NotFound();

            var featureId = featureCommand.FeatureId;

            if (featureId == null)
                return BadRequest();

            var tcuFeature = (from _tcuFeature in _tcuContext.Tcufeatures
                              where _tcuFeature.FeatureId == featureId
                              && _tcuFeature.TcuId == tcuId
                              select _tcuFeature).FirstOrDefault();
            if (tcuFeature != null)
            {
                tcuFeature.IsActive = false;
                _tcuContext.SaveChanges();
            }

            // check if feature is distributed on tcu or not
            return Ok(new
            {
                code = "Feature will be removed on the next vehicle wake-up"
            });
        }
    }
}