using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Serilog;

namespace AlYurr_CrestronDeviceDiscovery.Tests;
[TestFixture]
[TestOf(typeof(CrestronDeviceDiscovery))]
public class CrestronDeviceDiscoveryTest
{
    private readonly  ILogger _log = new LoggerConfiguration()
        .MinimumLevel
        .Verbose()
        .WriteTo
        .NUnitOutput(
            outputTemplate: "[{SourceContext} - {Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();
    [Test]
    public async Task DiscoverLocal_ShouldShowSomeDevices()
    {
        CrestronDeviceDiscovery.ClassLogger = _log;
        _log.Information("Starting Test");
        var devicesDiscovered = new List<CrestronDeviceEventArgs>();
        CrestronDeviceDiscovery.DeviceDiscovered += (sender, args) =>
        {
            devicesDiscovered.Add(args);
            _log.Information("Device Discovered: {@Name}", args);
        };
        CrestronDeviceDiscovery.Activity += (sender, args) => { _log.Information("Activity: {@Name}", args); };
        var devices = await CrestronDeviceDiscovery.DiscoveryLocal();
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        _log.Information("Test ended with {Number} devices found", devices.Count);
    }
}