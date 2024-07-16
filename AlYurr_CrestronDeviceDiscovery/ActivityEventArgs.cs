namespace AlYurr_CrestronDeviceDiscovery;
/// <summary> Class emitted every second to notify the discovery process status. </summary>
public class ActivityEventArgs : EventArgs
{
    /// <summary> Seconds since the discovery process started. </summary>
    public TimeSpan ElapsedTime { get; set; }
    /// <summary> Total seconds discovery will listen for devices. </summary>
    /// <remarks> The total of seconds might increase depending on the size of the network </remarks>
    public TimeSpan TotalTime { get; set; }
    /// <summary> Total devices discovered so far. </summary>
    public int DevicesDiscovered { get; set; }
    /// <summary> Discovery Status. </summary>
    public bool IsDiscovering { get; set; }
}