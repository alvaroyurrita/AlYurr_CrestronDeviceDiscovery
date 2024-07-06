using System.Net;

namespace AlYurr_CrestronDeviceDiscovery;

/// <summary>
/// Class that describes an Active IpV4 Adapter
/// </summary>
public class IpV4NetworkAdapter
{
    /// <summary> Adapter IP Address </summary>
    public IPAddress IPAddress { get; set; } = IPAddress.None;
    /// <summary> Adapter Broadcast Address </summary>
    public IPAddress BroadcastAddress {get; set;}= IPAddress.None;
    /// <summary> Adapter Name Address </summary>
    public string Name { get; set; } = "";
    /// <summary> Adapter ID </summary>
    public string  Id { get; set; } = "";
}