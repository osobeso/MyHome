using Microsoft.AspNetCore.Mvc;
using MyHome.Core.Devices;
using MyHome.Web.Shared;

namespace MyHome.Web.Server.Controllers
{
    [ApiController]
    [Route("device")]
    public class DeviceController : ControllerBase
    {
        private readonly ILogger<DeviceController> _logger;
        private readonly IDeviceRegistration _deviceRegistration;

        public DeviceController(ILogger<DeviceController> logger, IDeviceRegistration deviceRegistration)
        {
            _logger = logger;
            _deviceRegistration = deviceRegistration;
        }

        [HttpGet]
        public GetDeviceResponse Get()
        {
            if(_deviceRegistration.Device == null)
            {
                return new GetDeviceResponse
                {
                    Registered = false,
                    Device = null,
                };
            }
            var device = _deviceRegistration.Device;
            return new GetDeviceResponse
            {
                Registered = true,
                Device = new DeviceModel()
                {
                    ModelId = device.ModelId,
                    DeviceId = device.DeviceId,
                    DpsEndpointName = device.DpsEndpointName,
                    DpsIdScope = device.DpsIdScope,
                    SymmetricKey = device.SymmetricKey,
                }
            };
        }

        [HttpPost]
        public void Create(DeviceModel model)
        {
            _deviceRegistration.RegisterDevice(
                model.ModelId,
                model.DeviceId,
                model.DpsIdScope,
                model.DpsEndpointName,
                model.SymmetricKey);
        }
    }
}