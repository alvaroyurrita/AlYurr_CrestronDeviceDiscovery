using System.Diagnostics;

namespace AlYurr_CrestronDeviceDiscovery;
public partial class CrestronDeviceDiscovery
{
    private static void OnUpdateActivity(Stopwatch stopwatch)
    {
        ClassLogger.Information(
            "Timer Update: Devices Discovered: {DevicesDiscovered} Total Time: {TotalTime:#.#} seconds. Elapsed Time: {ElapsedTime:#.#}. Is Discovering: {IsDiscovering}",
            DiscoveredDevicesCount,
            DISCOVERY_TIMEOUT,
            stopwatch.Elapsed.TotalSeconds,
            IsDiscovering
        );
        Activity?.Invoke(
            null,
            new ActivityEventArgs
            {
                DevicesDiscovered = DiscoveredDevicesCount,
                TotalTime = new TimeSpan(0, 0, DISCOVERY_TIMEOUT),
                ElapsedTime = stopwatch.Elapsed,
                IsDiscovering = IsDiscovering,
                Error = _error
            }
        );
    }
}