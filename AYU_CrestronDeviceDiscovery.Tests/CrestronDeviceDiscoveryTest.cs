using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AYU_CrestronDeviceDiscovery;
using NUnit.Framework;
using Serilog;
using Serilog.Core;

namespace AYU_CrestronDeviceDiscovery.Tests;
[TestFixture]
[TestOf(typeof(CrestronDeviceDiscovery))]
public class CrestronDeviceDiscoveryTest
{
    private readonly Logger _log = new LoggerConfiguration().WriteTo.NUnitOutput().CreateLogger();
    [Test]
    public async Task DiscoverLocal_ShouldShowSomeDevices()
    {
            _log.Information("Starting Test");

        var devicesDiscovered = new List<CrestronDeviceEventArgs>();
        CrestronDeviceDiscovery.DeviceDiscovered += (sender, args) =>
        {
            devicesDiscovered.Add(args);
            _log.Information("Device Discovered: {Name}",args.Hostname);
        };
        var devices = await CrestronDeviceDiscovery.DiscoveryLocal();
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
            _log.Information("Test ended with {Number} devices found", devices.Count);

    }
}