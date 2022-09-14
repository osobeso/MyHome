using Microsoft.Extensions.Logging;

namespace MyHome.Simulator.LightBulb
{
    public class LightBulbHandler
    {
        private readonly ILogger<LightBulbHandler> Log;
        private bool On = false;
        public LightBulbHandler(ILogger<LightBulbHandler> logger)
        {
            Log = logger;
        }

        public void SwitchState()
        {
            On = !On;
            var state = On ? "On" : "Off";
            Log.LogInformation("State has been switched to: {state}", state);
        }

        public bool GetState()
        {
            return On;
        }
    }
}
