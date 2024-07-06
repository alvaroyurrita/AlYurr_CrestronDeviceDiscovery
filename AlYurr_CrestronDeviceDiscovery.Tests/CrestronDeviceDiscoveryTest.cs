using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Serilog;

namespace AlYurr_CrestronDeviceDiscovery.Tests;
[TestFixture]
[TestOf(typeof(CrestronDeviceDiscovery))]
public class CrestronDeviceDiscoveryTest
{
    private readonly ILogger _log = new LoggerConfiguration()
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
        var devices = await CrestronDeviceDiscovery.DiscoverFromAllAdapters();
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        _log.Information("Test ended with {Number} devices found", devices.Count);
    }

    [Test]
    public async Task DiscoverFromAdapter_ShouldShowSomeDevices()
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
        var adapters = CrestronDeviceDiscovery.GetIpV4Adapters();
        var adapter = adapters.Find(a => a.Name == "Video");
        if (adapter == null) return;
        var devices = await CrestronDeviceDiscovery.DiscoverFromAdapter(adapter);
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        _log.Information("Test ended with {Number} devices found", devices.Count);
    }

    [Test]
    public void GetNetworkIpV4Adapters_ShouldShowSomeDevices()
    {
        CrestronDeviceDiscovery.ClassLogger = _log;
        var adapters = CrestronDeviceDiscovery.GetIpV4Adapters();
        Assert.That(adapters.Count, Is.GreaterThan(0));
    }
}