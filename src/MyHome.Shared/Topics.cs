namespace MyHome.Shared
{
    public static class TopicNames
    {
        private const string Topic = nameof(Topic);
        public const string MyHomeToLightBulb = nameof(MyHomeToLightBulb) + Topic;
        public const string ThermostatToMyHome = nameof(ThermostatToMyHome) + Topic;
        public const string MyHomeToThermostat = nameof(MyHomeToThermostat) + Topic;
        public const string LightBulbToMyHome = nameof(LightBulbToMyHome) + Topic;
    }
}
