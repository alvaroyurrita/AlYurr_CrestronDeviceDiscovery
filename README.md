# Crestron Device Discovery

This helper class discovers all Crestron Devices in the network.

## Broadcast Discovery

The discovery process is based on the Crestron Discovery Protocol.  The library will send a UDP broadcast message to the network and will listen for responses.  The responses will be parsed and the devices will be returned.  

The library can be called in different ways. 

To discover from all available adapters use:

```csharp
var devices = async CrestronDeviceDiscovery.DiscoverFromAllAdapters();
```

To direct the discovery to a specific adapter, first gather all adapters on the machine, pick the one(s) needed and invoke the following method(s).

```csharp
var networkAdaptersList = async CrestronDeviceDiscovery.GetIpV4Adapters();
var devices = async CrestronDeviceDiscovery.DiscoverFromAdapter(networkAdaptersList[0]);
```
of use 
   
```csharp
var devices = async CrestronDeviceDiscovery.DiscoverFromAdapters(networkAdaptersSubList);
``` 
where networkAdaptersSubList is a list of adapters

## Remote Discovery

There library can also discover devices by connecting to a processor in the network, ideal when broadcast discovery is not available because the devices sit in a different network.

You will need the IP address or Hostname of a Crestron Device with AutoDiscovery functionality, normally a processor, it's username and password.

Call the following method to start the discovery process:

```csharp
var devices = async CrestronDeviceDiscovery.RemoteDiscovery("MyProcessor", "username", "password");
```

The library will connect to the Processors and wait for 10 seconds to receive the devices.  The library will return the devices found.

## Notes

To get immediate results, and activity reports you can subscribe to the following events:

* DeviceDiscovered
* Activity

The device discovered event will emit a single devices as the library discovers them. The device will have the following properties
* IpAddress
* Hostname
* Description
* DeviceId

The activity event will emit a string with the discovery status with the following elements:
* DevicesDiscovered
* Total Time (seconds)
* IsDiscovering: [True|False]
* Elapsed Time (seconds)
* Error: [Error message if any]


For more information visit the full library API documentation.