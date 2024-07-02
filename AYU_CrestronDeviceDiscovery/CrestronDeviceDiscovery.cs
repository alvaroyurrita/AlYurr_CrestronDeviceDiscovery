using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AYU_CrestronDeviceDiscovery;
public class CrestronDeviceDiscovery
{
    public static event EventHandler<CrestronDeviceEventArgs>? DeviceDiscovered;
    private static readonly Dictionary<string, CrestronDeviceEventArgs> DiscoveredDevices = new();
    private static async Task<List<CrestronDeviceEventArgs>> DiscoverAsync(int retries, string endPoint, string broadcastAddress)
    {
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
        //udpClient.Client.IOControl((IOControlCode)sio_udp_connreset, new byte[] { 0, 0, 0, 0 }, null);
        var autoDiscoverMessage = new List<byte> { 0x14, 0x00, 0x00, 0x00, 0x01, 0x04, 0x00, 0x03, 0x00, 0x00 };
        autoDiscoverMessage = autoDiscoverMessage.Concat(Encoding.ASCII.GetBytes(Dns.GetHostName())).ToList();
        autoDiscoverMessage = autoDiscoverMessage.Concat(new byte[266 - autoDiscoverMessage.Count]).ToList();
        var broadcastEndPoint = new IPEndPoint(IPAddress.Parse(broadcastAddress), port);
        var totalDevices = new List<CrestronDeviceEventArgs>();
        do
        {
            udpClient.Send(autoDiscoverMessage.ToArray(), autoDiscoverMessage.Count, broadcastEndPoint);
            var timeout = DateTime.Now.AddSeconds(3);
            while (DateTime.Now < timeout)
            {
                while (udpClient.Available > 0)
                {
                    try
                    {
                        var result = await udpClient.ReceiveAsync();
                        if (result.Buffer.Length > 0)
                        {
                            if (result.Buffer.Take(autoDiscoverResponse.Length).SequenceEqual(autoDiscoverResponse))
                            {
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
                                if (DiscoveredDevices.TryAdd(device.IpAddress, device))
                                {
                                    DeviceDiscovered?.Invoke(null, device);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    timeout = DateTime.Now.AddSeconds(1);
                }
            }
        } while (--retries > 0);
        udpClient.Dispose();
        return DiscoveredDevices.Select(d=>d.Value).ToList();
    }
    public static async Task<List<CrestronDeviceEventArgs>> DiscoveryLocal()
    {
        return await DiscoverAsync(3, "", "255.255.255.255");
    }
}