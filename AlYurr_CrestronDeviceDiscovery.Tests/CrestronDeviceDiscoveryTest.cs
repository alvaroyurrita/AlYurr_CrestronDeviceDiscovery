using NUnit.Framework;
using Serilog.Core;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AlYurr_CrestronDeviceDiscovery.Tests;

[TestFixture]
[TestOf(typeof(CrestronDeviceDiscovery))]
public class CrestronDeviceDiscoveryTest
{
    private readonly Logger _log = new LoggerConfiguration()
        .MinimumLevel
        .Verbose()
        .WriteTo
        .NUnitOutput(
            outputTemplate: "[{SourceContext} - {Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        )
        .CreateLogger();
    [Test]
    public async Task DiscoverFromAllAdapters_ShouldShowSomeDevices()
    {
        CrestronDeviceDiscovery.ClassLogger = _log;
        _log.Information("Starting Test");
        var devicesDiscovered = new List<CrestronDeviceEventArgs>();
        CrestronDeviceDiscovery.DeviceDiscovered += (_, args) =>
        {
            devicesDiscovered.Add(args);
            _log.Information("Device Discovered: {@Name}", args);
        };
        CrestronDeviceDiscovery.Activity += (_, args) => { _log.Information("Activity: {@Name}", args); };
        var devices = await CrestronDeviceDiscovery.DiscoverFromAllAdapters();
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        Assert.That(devices.Count, Is.GreaterThan(0));
        _log.Information("Test ended with {Number} devices found", devices.Count);
    }
    [Test]
    public async Task DiscoverFromSomeAdapters_ShouldShowSomeDevices()
    {
        CrestronDeviceDiscovery.ClassLogger = _log;
        _log.Information("Starting Test");
        var devicesDiscovered = new List<CrestronDeviceEventArgs>();
        CrestronDeviceDiscovery.DeviceDiscovered += (_, args) =>
        {
            devicesDiscovered.Add(args);
            _log.Information("Device Discovered: {@Name}", args);
        };
        CrestronDeviceDiscovery.Activity += (_, args) => { _log.Information("Activity: {@Name}", args); };
        var adapters = CrestronDeviceDiscovery.GetIpV4Adapters();
        var someAdapters = adapters.Slice(0, 2);
        var devices = await CrestronDeviceDiscovery.DiscoverFromAdapters(someAdapters);
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        Assert.That(devices.Count, Is.GreaterThan(0));
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
        CrestronDeviceDiscovery.Activity += (_, args) => { _log.Information("Activity: {@Name}", args); };
        var adapters = CrestronDeviceDiscovery.GetIpV4Adapters();
        var adapter = adapters.Find(a => a.IPAddress.ToString().Contains("192"));
        if (adapter == null)
        {
            throw new AssertionException("No adapter in the 192 range was found. Adjust the test to use an available adapter on this machine.");
        }
        ;
        var devices = await CrestronDeviceDiscovery.DiscoverFromAdapter(adapter);
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        Assert.That(devices.Count, Is.GreaterThan(0));
        _log.Information("Test ended with {Number} devices found", devices.Count);
    }

    [Test]
    public void GetNetworkIpV4Adapters_ShouldShowSomeDevices()
    {
        CrestronDeviceDiscovery.ClassLogger = _log;
        var adapters = CrestronDeviceDiscovery.GetIpV4Adapters();
        Assert.That(adapters.Count, Is.GreaterThan(0));
    }
    [Test]
    public async Task RemoteDiscover_ShouldShowSomeDevices()
    {
        CrestronDeviceDiscovery.ClassLogger = _log;
        _log.Information("Starting Test");
        if (File.Exists("env.json") == false) Assert.Fail();
        var connectionInfo = JsonNode.Parse(await File.ReadAllTextAsync("env.json"));
        var devicesDiscovered = new List<CrestronDeviceEventArgs>();
        CrestronDeviceDiscovery.DeviceDiscovered += (_, args) =>
        {
            devicesDiscovered.Add(args);
            _log.Information("Device Discovered: {@Name}", args);
        };
        CrestronDeviceDiscovery.Activity += (_, args) => { _log.Information("Activity: {@Name}", args); };
        var devices = await CrestronDeviceDiscovery.RemoteDiscovery(
            connectionInfo?["Hostname"]?.ToString() ?? string.Empty,
            connectionInfo?["Username"]?.ToString() ?? string.Empty,
            connectionInfo?["Password"]?.ToString() ?? string.Empty
        );
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        Assert.That(devices.Count, Is.GreaterThan(0));
        _log.Information("Test ended with {Number} devices found", devices.Count);
    }
    [Test]
    public async Task RemoteDiscover_BadPassword_ShouldShowNoDevices()
    {
        CrestronDeviceDiscovery.ClassLogger = _log;
        _log.Information("Starting Test");
        if (File.Exists("env.json") == false) Assert.Fail();
        var connectionInfo = JsonNode.Parse(await File.ReadAllTextAsync("env.json"));
        var devicesDiscovered = new List<CrestronDeviceEventArgs>();
        CrestronDeviceDiscovery.DeviceDiscovered += (_, args) =>
        {
            devicesDiscovered.Add(args);
            _log.Information("Device Discovered: {@Name}", args);
        };
        CrestronDeviceDiscovery.Activity += (_, args) => { _log.Information("Activity: {@Name}", args); };
        var devices = await CrestronDeviceDiscovery.RemoteDiscovery(
            connectionInfo?["Hostname"]?.ToString() ?? string.Empty,
            connectionInfo?["Username"]?.ToString() ?? string.Empty,
            "badPassword"
        );
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        Assert.That(devices.Count, Is.EqualTo(0));
        _log.Information("Test ended with {Number} devices found", devices.Count);
    }
    [Test]
    public async Task RemoteDiscover_BadPassword_AndThenGoodOne_ShouldShowSomeDevices()
    {
        CrestronDeviceDiscovery.ClassLogger = _log;
        _log.Information("Starting Test");
        if (File.Exists("env.json") == false) Assert.Fail();
        var connectionInfo = JsonNode.Parse(await File.ReadAllTextAsync("env.json"));
        var devicesDiscovered = new List<CrestronDeviceEventArgs>();
        CrestronDeviceDiscovery.DeviceDiscovered += (_, args) =>
        {
            devicesDiscovered.Add(args);
            _log.Information("Device Discovered: {@Name}", args);
        };
        CrestronDeviceDiscovery.Activity += (_, args) => { _log.Information("Activity: {@Name}", args); };
        var devices = await CrestronDeviceDiscovery.RemoteDiscovery(
            connectionInfo?["Hostname"]?.ToString() ?? string.Empty,
            connectionInfo?["Username"]?.ToString() ?? string.Empty,
            "badPassword"
        );
        devices = await CrestronDeviceDiscovery.RemoteDiscovery(
            connectionInfo?["Hostname"]?.ToString() ?? string.Empty,
            connectionInfo?["Username"]?.ToString() ?? string.Empty,
            connectionInfo?["Password"]?.ToString() ?? string.Empty
        );
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        Assert.That(devices.Count, Is.GreaterThan(0));
        _log.Information("Test ended with {Number} devices found", devices.Count);
    }
    [Test]
    public async Task RemoteDiscover_BadEverything_AndThenGoodOne_ShouldShowSomeDevices()
    {
        CrestronDeviceDiscovery.ClassLogger = _log;
        _log.Information("Starting Test");
        if (File.Exists("env.json") == false) Assert.Fail();
        var connectionInfo = JsonNode.Parse(await File.ReadAllTextAsync("env.json"));
        var devicesDiscovered = new List<CrestronDeviceEventArgs>();
        CrestronDeviceDiscovery.DeviceDiscovered += (_, args) =>
        {
            devicesDiscovered.Add(args);
            _log.Information("Device Discovered: {@Name}", args);
        };
        CrestronDeviceDiscovery.Activity += (_, args) => { _log.Information("Activity: {@Name}", args); };
        var devices = await CrestronDeviceDiscovery.RemoteDiscovery(
            "asdf",
            "asdf",
            "asdf"
        );
        devices = await CrestronDeviceDiscovery.RemoteDiscovery(
            connectionInfo?["Hostname"]?.ToString() ?? string.Empty,
            connectionInfo?["Username"]?.ToString() ?? string.Empty,
            connectionInfo?["Password"]?.ToString() ?? string.Empty
        );
        Assert.That(devices.Count, Is.EqualTo(devicesDiscovered.Count));
        Assert.That(devices.Count, Is.GreaterThan(0));
        _log.Information("Test ended with {Number} devices found", devices.Count);
    }
}