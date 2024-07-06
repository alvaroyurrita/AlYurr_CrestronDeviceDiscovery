using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AlYurr_CrestronDeviceDiscovery;

public partial class CrestronDeviceDiscovery
{
    /// <summary>
    /// Gets all the IpV4 Adapters that are active in the computer
    /// </summary>
    /// <returns>A list of all IpV4 Network Adapters in the IpV4NetworkAdapter class</returns>
    public static List<IpV4NetworkAdapter> GetIpV4Adapters()
    {
        var adapters = NetworkInterface.GetAllNetworkInterfaces().ToList();
        var ipV4Adapters = adapters.FindAll(a => a.Supports(NetworkInterfaceComponent.IPv4));
        var operationalAdapters = ipV4Adapters.FindAll(a => a.OperationalStatus == OperationalStatus.Up && a.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        var ipv4NetworkAdapters = operationalAdapters.Select(a =>
        {
            var aUnicastAddress = a.GetIPProperties().UnicastAddresses.First(ad => ad.Address.AddressFamily == AddressFamily.InterNetwork);
            if (aUnicastAddress == null) return null;
            var aMask = aUnicastAddress.IPv4Mask;
            var aBroadcastAddress = GetBroadcastAddress(aUnicastAddress.Address, aMask);
            return new IpV4NetworkAdapter
            {
                Id = a.Id,
                Name = a.Name,
                IPAddress = aUnicastAddress.Address,
                BroadcastAddress = aBroadcastAddress,

            };
        }).ToList();
        return ipv4NetworkAdapters!;
    }
}