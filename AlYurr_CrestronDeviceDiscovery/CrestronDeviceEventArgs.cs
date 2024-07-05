﻿namespace AlYurr_CrestronDeviceDiscovery;
/// <summary> Class emmited every time a device is discovered</summary>
public class CrestronDeviceEventArgs : EventArgs
{
    /// <summary>  Device IP Address. </summary>
    public string IpAddress { get; set; } = "";
    /// <summary>  Device Hostname. </summary>
    public string Hostname { get; set; } = "";
    /// <summary>  Device Description. </summary>
    public string Description { get; set; } = "";
    /// <summary>  Device Information. </summary>
    public string DeviceId { get; set; } = "";
}