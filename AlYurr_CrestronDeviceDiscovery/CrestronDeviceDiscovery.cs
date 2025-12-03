using Serilog;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Timer = System.Timers.Timer;

namespace AlYurr_CrestronDeviceDiscovery;
/// <summary> Crestron Device Discovery Class </summary>
public partial class CrestronDeviceDiscovery
{
    private const int DISCOVERY_TIMEOUT = 8;
    /// <summary> Event triggered every time a device is discovered </summary>
    public static event EventHandler<CrestronDeviceEventArgs>? DeviceDiscovered;
    private static ILogger _classLogger = Log.Logger.ForContext<CrestronDeviceDiscovery>();
    /// <summary> Serilog Logger Injection </summary>
    public static ILogger ClassLogger
    {
        get => _classLogger;
        set =>
            _classLogger = value == null
                ? Log.Logger.ForContext<CrestronDeviceDiscovery>()
                : value.ForContext<CrestronDeviceDiscovery>();
    }
    /// <summary> Event triggered every second to notify the discovery process status. </summary>
    public static event EventHandler<ActivityEventArgs>? Activity;
    private static int _discoverDevicesCount;
    /// <summary> Quantity of Discovered Devices </summary>
    private static int DiscoveredDevicesCount => _discoverDevicesCount;
    private static SemaphoreSlim EventSemaphore { get; } = new(1, 1);
    private static bool IsDiscovering { get; set; }
    private static string _error = "";

    private static async Task<List<ICrestronDevice>> DiscoverAsync(IpV4NetworkAdapter endPoint)
    {
        _error = string.Empty; ;
        var discoveredDevices = new Dictionary<string, ICrestronDevice>();
        var udpClient = new UdpClient();
        const int port = 41794;
        const uint iocIn = 0x80000000;
        const uint iocVendor = 0x18000000;
        const uint sioUdpConnectionReset = iocIn | iocVendor | 12;
        var autoDiscoverResponse = new byte[] { 0x15, 0x00, 0x00, 0x00 };
        const string autoDiscoveryMessagePattern =
            @"(?<hostname>[\w-]*)\x00+(?<description>[\x20-\x7E]*\])(\s*\@(?<devid>[\x20-\x7E]*))?";
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(endPoint.IPAddress, port));
        udpClient.Client.ReceiveTimeout = 4000;
        udpClient.Client.ReceiveBufferSize = 65535;
        udpClient.EnableBroadcast = true;
        udpClient.Client.IOControl((IOControlCode)sioUdpConnectionReset, new byte[] { 0, 0, 0, 0 }, null);
        var autoDiscoverMessage = new List<byte>
        {
            0x14,
            0x00,
            0x00,
            0x00,
            0x01,
            0x04,
            0x00,
            0x03,
            0x00,
            0x00
        };
        autoDiscoverMessage = autoDiscoverMessage.Concat(Encoding.ASCII.GetBytes(Dns.GetHostName())).ToList();
        autoDiscoverMessage = autoDiscoverMessage.Concat(new byte[266 - autoDiscoverMessage.Count]).ToList();
        var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
        ClassLogger.Debug(
            "Starting Discovery Process for for interface {Interface} for {Time} seconds",
            endPoint.IPAddress,
            DISCOVERY_TIMEOUT
        );
        new Thread(
            () =>
            {
                for (var i = 0; i < 3; i++)
                {
                    try
                    {
                        ClassLogger.Debug("Sending Discovery Message No {No}", i + 1);
                        udpClient.Send(autoDiscoverMessage.ToArray(), autoDiscoverMessage.Count, broadcastEndPoint);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error while sending discovery UDP Packet: {Error}", ex.Message);
                        _error = ex.Message;
                    }
                    Thread.Sleep(500);
                }
            }
        ).Start();
        var timeout = DateTime.Now.AddSeconds(DISCOVERY_TIMEOUT);
        while (DateTime.Now < timeout)
        {
            while (udpClient.Available > 0)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync();
                    if (result.Buffer.Length <= 0) continue;
                    if (!result.Buffer.Take(autoDiscoverResponse.Length).SequenceEqual(autoDiscoverResponse))
                        continue;
                    var receivedMessage =
                        Encoding.ASCII.GetString(result.Buffer.Skip(autoDiscoverResponse.Length).ToArray());
                    var match = Regex.Match(
                        receivedMessage,
                        autoDiscoveryMessagePattern
                    );
                    if (match.Groups.Count < 4) continue;
                    var device = new CrestronDeviceEventArgs
                    {
                        Hostname = match.Groups["hostname"].Value,
                        Description = match.Groups["description"].Value,
                        DeviceId = match.Groups["devid"].Value,
                        IpAddress = result.RemoteEndPoint.Address.ToString()
                    };
                    if (!discoveredDevices.TryAdd(device.DeviceId, device)) continue;
                    Interlocked.Increment(ref _discoverDevicesCount);
                    await EventSemaphore.WaitAsync();
                    DeviceDiscovered?.Invoke(null, device);
                    EventSemaphore.Release();
                }
                catch (Exception ex)
                {
                    Log.Error("Error while receiving UDP Packet: {Error}", ex.Message);
                    _error = ex.Message;
                }
            }
        }
        udpClient.Dispose();
        ClassLogger.Debug(
            "Ending Discovery Processes for Interface. {Devices}. Found {Number} devices",
            endPoint.IPAddress,
            discoveredDevices.Count
        );
        // Semaphore.Release();
        return [.. discoveredDevices.Select(d => d.Value)];
    }
    /// <summary> Discovers Crestron Devices on the local network </summary>
    /// <returns> List of Devices Discovered </returns>
    public static async Task<List<ICrestronDevice>> DiscoverFromAllAdapters()
    {
        var validAdapters = GetIpV4Adapters();
        return await DiscoverFromAdapters(validAdapters);
    }

    /// <summary> Discovers Crestron Devices on the local network </summary>
    /// <param name="adapters"> List of PC adapters to listen from </param>
    /// <returns> List of Devices Discovered </returns>
    public static async Task<List<ICrestronDevice>> DiscoverFromAdapters(List<IpV4NetworkAdapter> adapters)
    {
        IsDiscovering = true;
        var timer = new Timer(1000);
        timer.Start();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        timer.Elapsed += (_, _) => { OnUpdateActivity(stopwatch); };
        var tasks = new List<Task<List<ICrestronDevice>>>();
        foreach (var adapter in adapters) tasks.Add(Task.Run(()=>DiscoverAsync(adapter))); //makes sure that tasks are run in parallel by offloading CPU Bound work
        var individualResults = await Task.WhenAll(tasks);
        var results = new List<ICrestronDevice>();
        foreach (var individualResult in individualResults) results.AddRange(individualResult);
        IsDiscovering = false;
        timer.Stop();
        OnUpdateActivity(stopwatch);
        stopwatch.Stop();
        stopwatch.Reset();
        return results;
    }

    /// <summary> Discovers Crestron Devices from a single Netwrok Adapter </summary>
    /// <param name="adapter"> The PC adapter to listen from </param>
    /// <returns> List of Devices Discovered </returns>
    public static async Task<List<ICrestronDevice>> DiscoverFromAdapter(IpV4NetworkAdapter adapter) =>
        await DiscoverFromAdapters(new List<IpV4NetworkAdapter> { adapter });
}