using Renci.SshNet;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Timer = System.Timers.Timer;

namespace AlYurr_CrestronDeviceDiscovery;
public partial class CrestronDeviceDiscovery
{
    private static readonly CancellationTokenSource StopSearching = new();
    private const string REMOTE_DISCOVERY_REG_PATTERN =
        @"^(?'IpAddress'[0-9\.]*)( :.*? : )(?'Hostname'.*)( :.*? )(?'Description'.*)( @)(?'DeviceId'.*)$";
    /// <summary>
    ///     Gathers Crestron Device Information from a remote processor. The discovery will automatically stop after 8
    ///     seconds.
    /// </summary>
    /// <param name="remoteHost"> IP address or Hostname of the processor </param>
    /// <param name="username"> User Name to log into the processor </param>
    /// <param name="password"> Password to log into the processor </param>
    /// <returns> </returns>
    public static async Task<List<ICrestronDevice>> RemoteDiscovery(string remoteHost,
        string username,
        string password)
    {
        await RunningSemaphore.WaitAsync(StopSearching.Token);
        if (StopSearching.Token.IsCancellationRequested) return new List<ICrestronDevice>();
        IsDiscovering = true;
        var timer = new Timer(1000);
        timer.Start();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        timer.Elapsed += (_, _) =>
        {
            OnUpdateActivity(stopwatch);
            if (!(stopwatch.Elapsed.TotalSeconds >= DISCOVERY_TIMEOUT)) return;
            timer.Stop();
            IsDiscovering = false;
            RunningSemaphore.Release();
            StopSearching.Cancel();
        };
        var keyboardInteractiveMethod = new KeyboardInteractiveAuthenticationMethod(username);
        var passwordAuthenticationMethod = new PasswordAuthenticationMethod(username, password);
        var connectionInfo = new ConnectionInfo(
            remoteHost,
            22,
            username,
            keyboardInteractiveMethod,
            passwordAuthenticationMethod
        ) { Encoding = Encoding.GetEncoding("ISO-8859-1"), Timeout = TimeSpan.FromSeconds(10) };
        var results = new List<ICrestronDevice>();
        keyboardInteractiveMethod.AuthenticationPrompt += (sender, args) =>
        {
            foreach (var prompt in args.Prompts)
                if (prompt.Request.ToLower().Contains("password"))
                    prompt.Response = password;
        };
        using var sshClient = new SshClient(connectionInfo);
        SshCommand command;
        OnUpdateActivity(stopwatch);
        try
        {
            ClassLogger.Debug("Connecting to {RemoteHost}", remoteHost);
            sshClient.Connect();
            var networkInfo = sshClient.RunCommand("ipconfig");
            var selfIpAddress = Regex.Match(networkInfo.Result, @"IP Address ........ : ([0-9\.]*)").Groups[1].Value.Trim();
            var hostnameAnswer = sshClient.RunCommand("hostname");
            var hostname = Regex.Match(hostnameAnswer.Result, @"Host Name: (.*)").Groups[1].Value.Trim();
            var version = sshClient.RunCommand("ver");
            var verMatch = Regex.Match(version.Result, @"(?'Description'.*) @(?'DeviceId'.*)");
            var selfDevice = new CrestronDeviceEventArgs
            {
                IpAddress = selfIpAddress,
                Description = verMatch.Groups["Description"].Value.Replace("Cntrl Eng ", "").Trim(),
                Hostname = hostname,
                DeviceId = verMatch.Groups["DeviceId"].Value.Trim()
            };
            DeviceDiscovered?.Invoke(null, selfDevice);
            results.Add(selfDevice);
            _discoverDevicesCount = results.Count;
            command = sshClient.CreateCommand("autodiscovery query");
            await command.ExecuteAsync();
        }
        catch (Exception ex)
        {
            ClassLogger.Error(ex, "Error Connecting to {RemoteHost}", remoteHost);
            IsDiscovering = false;
            RunningSemaphore.Release();
            StopSearching.Cancel();
            return new List<ICrestronDevice>();
        }
        sshClient.Disconnect();
        var error = command.Error;
        if (!string.IsNullOrEmpty(error))
        {
            ClassLogger.Error("Error retrieving results from {RemoteHost} - {Error}", remoteHost, error);
            return new List<ICrestronDevice>();
        }
        var result = command.Result;
        stopwatch.Stop();
        timer.Stop();
        var parsedResult = Regex.Matches(result, REMOTE_DISCOVERY_REG_PATTERN, RegexOptions.Multiline);
        ClassLogger.Debug(
            "Ending Discovery Processes for Host {RemoteHost}. Found {Number} devices",
            remoteHost,
            parsedResult.Count
        );
        if (parsedResult.Count == 0) return new List<ICrestronDevice>();
        foreach (Match match in parsedResult)
        {
            var device = new CrestronDeviceEventArgs
            {
                IpAddress = match.Groups["IpAddress"].Value,
                Hostname = match.Groups["Hostname"].Value,
                Description = match.Groups["Description"].Value,
                DeviceId = match.Groups["DeviceId"].Value
            };
            DeviceDiscovered?.Invoke(null, device);
            results.Add(device);
        }
        _discoverDevicesCount = results.Count;
        IsDiscovering = false;
        OnUpdateActivity(stopwatch);
        stopwatch.Reset();
        RunningSemaphore.Release();
        return results;
    }
}