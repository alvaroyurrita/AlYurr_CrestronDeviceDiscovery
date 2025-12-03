# Crestron Device Discovery - AI Coding Agent Instructions

## Project Overview
A .NET library for discovering Crestron devices on networks via UDP broadcast (local) or SSH remote discovery. Targets net8.0 and net10.0, published as a NuGet package.

**Key Components:**
- `AlYurr_CrestronDeviceDiscovery/` - Main library (class library)
- `AlYurr_CrestronDeviceDiscovery.Tests/` - NUnit test project
- `CrestronDeviceDiscoveryConsoleApp/` - Example console application
- `Docs/` - DocFX API documentation

## Architecture Patterns

### Partial Class Design
The `CrestronDeviceDiscovery` class is split across multiple files using `partial class`:
- `CrestronDeviceDiscovery.cs` - Core broadcast discovery logic, events, main entry points
- `RemoteDiscover.cs` - SSH-based remote discovery via Crestron processor
- `GetIpV4Interfaces.cs` - Network adapter enumeration
- `GetBroadcastAddress.cs` - Broadcast address calculation
- `UpdateActivitiy.cs` - Activity event emission

**When modifying:** Ensure changes to static state (like `_discoverDevicesCount`, `IsDiscovering`, `_error`) are thread-safe across all partial files.

### Event-Driven Discovery
The library uses two event patterns:
```csharp
// Emits individual devices as discovered (real-time)
CrestronDeviceDiscovery.DeviceDiscovered += (_, args) => { };

// Emits status updates every second during discovery
CrestronDeviceDiscovery.Activity += (_, args) => { };
```
**Pattern:** Subscribe to events BEFORE calling discovery methods to capture all devices.

### Async Parallelization
Discovery from multiple adapters runs in parallel using `Task.Run()` + `Task.WhenAll()`:
```csharp
foreach (var adapter in adapters) 
    tasks.Add(Task.Run(() => DiscoverAsync(adapter)));
var results = await Task.WhenAll(tasks);
```
**Why:** Offloads CPU-bound UDP work to thread pool for true parallelism.

## Critical Developer Workflows

### Building & Testing
```powershell
# Build solution
dotnet build AlYurr_CrestronDeviceDiscovery.sln

# Run tests (requires network with Crestron devices or env.json for remote tests)
dotnet test

# Build NuGet package
dotnet pack AlYurr_CrestronDeviceDiscovery/AlYurr_CrestronDeviceDiscovery.csproj -c Release
```

### Documentation Generation
Uses DocFX to generate API docs from XML comments:
```powershell
# Generate and serve docs locally
docfx Docs\docfx.json --serve
```
**Note:** DocFX config targets net10.0 framework. Ensure XML documentation is enabled in both Debug and Release (see .csproj).

### Running Console App
```powershell
dotnet run --project CrestronDeviceDiscoveryConsoleApp
```
Interactive menu: (1) single adapter, (2) all adapters, (3) remote discovery.

## Project-Specific Conventions

### Logging Pattern
Serilog logger injection via static property:
```csharp
CrestronDeviceDiscovery.ClassLogger = myLogger;
```
**Default:** Falls back to `Log.Logger.ForContext<CrestronDeviceDiscovery>()` if not set.

**Test Pattern:** See `CrestronDeviceDiscoveryTest.cs` - configure NUnit output sink:
```csharp
var _log = new LoggerConfiguration()
    .WriteTo.NUnitOutput(outputTemplate: "[{SourceContext} - {Timestamp:HH:mm:ss:fff} {Level:u3}]...")
    .CreateLogger();
CrestronDeviceDiscovery.ClassLogger = _log;
```

### Test Environment Setup
Remote discovery tests require `env.json` in test output directory:
```json
{
  "ipAddress": "processor-ip",
  "username": "admin",
  "password": "password"
}
```
**CopyToOutputDirectory:** Set to `Always` in Tests.csproj.

### Discovery Protocol Constants
- UDP Port: `41794`
- Broadcast timeout: `8 seconds` (DISCOVERY_TIMEOUT)
- Remote discovery timeout: `10 seconds` (REMOTE_DISCOVERY_TIMEOUT)
- SSH connection timeout: `5 seconds` (CONNECTION_TIMEOUT)

**Do not change** these without understanding the Crestron Discovery Protocol specification.

## Data Flow

1. **Broadcast Discovery:** UDP broadcast → 8s listen loop → Regex parse responses → emit `DeviceDiscovered` events → return aggregated list
2. **Remote Discovery:** SSH connect → query self device info → run `autodiscovery query` → parse streaming output → emit events → return list

**Thread Safety:** `EventSemaphore` (SemaphoreSlim) guards event invocations. `Interlocked.Increment` protects `_discoverDevicesCount`.

## Dependencies
- **Serilog** (4.0.0) - Structured logging
- **SSH.NET** (2024.1.0) - Remote discovery via SSH
- **NUnit** (4.1.0) - Testing framework

## Common Pitfalls
- Tests fail if no Crestron devices on network or `env.json` missing for remote tests
- Discovery returns empty list if firewall blocks UDP 41794
- Adapter selection in tests may fail if hardcoded IP ranges (e.g., "192") don't exist on machine
- DocFX may fail if wrong TargetFramework specified in docfx.json
