using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Timers;
using Serilog;
using Timer = System.Timers.Timer;

namespace AlYurr_CrestronDeviceDiscovery;
/// <summary>
/// Crestron Device Discovery Class
/// </summary>
public class CrestronDeviceDiscovery
{
    private const int INITIAL_DISCOVERY_TIMEOUT = 8;
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
    private static readonly Timer Timer = new(1000);
    private static readonly Dictionary<string, CrestronDeviceEventArgs> DiscoveredDevices = new();
    private static SemaphoreSlim Semaphore { get; } = new(1, 1);
    private static TimeSpan TotalTime { get; set; }
    private static readonly Stopwatch Stopwatch = new();
    private static bool IsDiscovering { get; set; }
    static CrestronDeviceDiscovery()
    {
        Timer.Elapsed += (_, _) => { OnUpdateActivity(); };
    }
    private static void OnUpdateActivity()
    {
        ClassLogger.Information(
            "Timer Update: Devices Discovered: {DevicesDiscovered} Total Time: {TotalTime:#.#} seconds. Elapsed Time: {ElapsedTime:#.#}. Is Discovering: {IsDiscovering}",
            DiscoveredDevices.Count, TotalTime.TotalSeconds, Stopwatch.Elapsed.TotalSeconds, IsDiscovering);
        Activity?.Invoke(null, new ActivityEventArgs
        {
            DevicesDiscovered = DiscoveredDevices.Count,
            TotalTime = TotalTime,
            ElapsedTime = Stopwatch.Elapsed,
            IsDiscovering = IsDiscovering,
        });
    }
    private static async Task<List<CrestronDeviceEventArgs>> DiscoverAsync(string endPoint,
        string broadcastAddress)
    {
        await Semaphore.WaitAsync();
        var udpClient = new UdpClient();
        DiscoveredDevices.Clear();
        var port = 41794;
        var ioc_in = 0x80000000;
        var ioc_vendor = 0x18000000;
        var sio_udp_connreset = ioc_in | ioc_vendor | 12;
        var autoDiscoverResponse = new byte[] { 0x15, 0x00, 0x00, 0x00 };
        var autoDiscoveryMessagePattern =
            @"(?<hostname>[\w-]*)\x00+(?<description>[\x20-\x7E]*\])(\s*\@(?<devid>[\x20-\x7E]*))?";
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
        udpClient.Client.ReceiveTimeout = 4000;
        udpClient.Client.ReceiveBufferSize = 1024;
        udpClient.EnableBroadcast = true;
        udpClient.Client.IOControl((IOControlCode)sio_udp_connreset, new byte[] { 0, 0, 0, 0 }, null);
        var autoDiscoverMessage = new List<byte> { 0x14, 0x00, 0x00, 0x00, 0x01, 0x04, 0x00, 0x03, 0x00, 0x00 };
        autoDiscoverMessage = autoDiscoverMessage.Concat(Encoding.ASCII.GetBytes(Dns.GetHostName())).ToList();
        autoDiscoverMessage = autoDiscoverMessage.Concat(new byte[266 - autoDiscoverMessage.Count]).ToList();
        var broadcastEndPoint = new IPEndPoint(IPAddress.Parse(broadcastAddress), port);
        var startTime = DateTime.Now;
        var estimatedEndTime = startTime.AddSeconds(INITIAL_DISCOVERY_TIMEOUT);
        TotalTime = estimatedEndTime - startTime;
        Stopwatch.Start();
        Timer.Start();
        IsDiscovering = true;
        ClassLogger.Debug("Starting Discovery Process for {TotalTime:#.#} seconds. ", TotalTime.TotalSeconds);
        OnUpdateActivity();
        Task.Run(async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                ClassLogger.Debug("Sending Discovery Message No {No}", i+1);
                await udpClient.SendAsync(autoDiscoverMessage.ToArray(), autoDiscoverMessage.Count, broadcastEndPoint);
                await Task.Delay(500);
            }
        });
        var timeout = DateTime.Now.AddSeconds(INITIAL_DISCOVERY_TIMEOUT);
        while (DateTime.Now < timeout)
        {
            var devicesFound = false;
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
                    var match = System.Text.RegularExpressions.Regex.Match(receivedMessage,
                        autoDiscoveryMessagePattern);
                    if (match.Groups.Count < 4) continue;
                    var device = new CrestronDeviceEventArgs
                    {
                        Hostname = match.Groups["hostname"].Value,
                        Description = match.Groups["description"].Value,
                        DeviceId = match.Groups["devid"].Value,
                        IpAddress = result.RemoteEndPoint.Address.ToString()
                    };
                    if (!DiscoveredDevices.TryAdd(device.IpAddress, device)) continue;
                    devicesFound = true;
                    DeviceDiscovered?.Invoke(null, device);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            if (!devicesFound) continue;
            timeout = timeout.AddSeconds(1);
            TotalTime += TimeSpan.FromSeconds(1);
            ClassLogger.Debug(
                "Devices Discovered - Incrementing Timeout for 1 Second. New Total Time {TotalTime::#.#} Seconds",
                TotalTime.TotalSeconds);
        }
        IsDiscovering = false;
        Timer.Stop();
        OnUpdateActivity();
        Stopwatch.Reset();
        Stopwatch.Stop();
        udpClient.Dispose();
        ClassLogger.Debug("Ending Discovery Processes after {TotalTime:#.#} seconds. {Devices} Found ",
            TotalTime.TotalSeconds, DiscoveredDevices.Count);
        Semaphore.Release();
        return DiscoveredDevices.Select(d => d.Value).ToList();
    }
    /// <summary>
    /// Discovers Crestron Devices on the local network
    /// </summary>
    /// <returns> List of Devices Discovered</returns>
    public static async Task<List<CrestronDeviceEventArgs>> DiscoveryLocal()
    {
        return await DiscoverAsync("", "255.255.255.255");
        // return await DiscoverAsync(3, "", "192.168.31.255");
    }
}