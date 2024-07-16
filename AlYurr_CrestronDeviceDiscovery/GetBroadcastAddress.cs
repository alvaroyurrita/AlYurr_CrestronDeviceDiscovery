using System.Net;

namespace AlYurr_CrestronDeviceDiscovery;
public partial class CrestronDeviceDiscovery
{
    private static IPAddress GetBroadcastAddress(IPAddress iPAddress, IPAddress mask)
    {
        if (iPAddress == null) return IPAddress.Broadcast;
        var longMulticastAddress = BitConverter.ToUInt32(iPAddress.GetAddressBytes(), 0)
                                   | ~BitConverter.ToUInt32(mask.GetAddressBytes());
        return new IPAddress(longMulticastAddress);
    }
}