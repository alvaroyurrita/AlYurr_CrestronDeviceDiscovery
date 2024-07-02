namespace AYU_CrestronDeviceDiscovery;
public class CrestronDeviceEventArgs : EventArgs
{
    public string IpAddress { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string Description { get; set; } = "";
    public string DeviceId { get; set; } = "";
}