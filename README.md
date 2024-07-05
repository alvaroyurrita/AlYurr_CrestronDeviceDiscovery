# Crestron Device Discovery

This helper class discovers all Crestron Devices in the network.

It consists of an Async Process that can be called

```csharp
var devices = async CrestronDeviceDiscovery.DiscoverDevicesAsync();
```

or a sync process that can be called

```csharp
var devices = CrestronDeviceDiscovery.DiscoverDevices();
```
the last call is a non-blocking call.  You should first subscribe to two events:

* DeviceDiscovered
* Activity

The device discovered event will emit a single devices as the library discovers them. The device will have the following properties
* IpAddress
* Hostname
* Description
* DeviceId

The activity event will emit a string with the discvery status with the following elements:
* DevicesDiscovered
* Elapsed Time (seconds)
* Status: Discovering, Completed, Error
* Remaining Time (seconds

Note that the remaining time will vary if there are a lot of devices in the network.

For more information visit the full library manual