dotnet publish ../src/MyHome.Web/Server/MyHome.Web.Server.csproj -c Release -o ./publish
dotnet publish ../src/MyHome.Simulator.Thermostat/MyHome.Simulator.Thermostat.csproj -c Release -o ./Thermostat/publish
dotnet publish ../src/MyHome.Simulator.LightBulb/MyHome.Simulator.LightBulb.csproj -c Release -o ./Lightbulb/publish
