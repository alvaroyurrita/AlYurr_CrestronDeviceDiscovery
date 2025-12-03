// See https://aka.ms/new-console-template for more information
using AlYurr_CrestronDeviceDiscovery;
using static System.Runtime.InteropServices.JavaScript.JSType;

string choice = "";
do
{
    Console.WriteLine("Select an option:");
    Console.WriteLine("1. Discover Crestron Devices from a specific adapter");
    Console.WriteLine("2. Discover Crestron Devices from all adapters");
    Console.WriteLine("3. Discover Crestron Devices from a remote Console");
    Console.WriteLine("X. Stop");

    choice = Console.ReadLine() ?? "x";
    switch (choice)
    {
        case "1":
            var adapteters = CrestronDeviceDiscovery.GetIpV4Adapters();
            Console.WriteLine("Available Adapters:");
            int i = 0;
            foreach (var adapter in adapteters)
            {
                Console.WriteLine($"{i++} - {adapter.IPAddress} - {adapter.Name}");
            }
            var adapterNumber = Console.ReadLine();
            if (int.TryParse(adapterNumber, out int number) && number <= adapteters.Count)
            {
                Console.WriteLine($"Looking for Crestron Devices from adapter {adapteters[number].IPAddress}");
                var devices = await CrestronDeviceDiscovery.DiscoverFromAdapter(adapteters[number]);
                if (devices.Count == 0)
                {
                    Console.WriteLine("No Crestron devices found.");
                    break;
                }
                foreach (var device in devices)
                {
                    Console.WriteLine("Hostname: " + device.Hostname);
                    Console.WriteLine("IP Address: " + device.IpAddress);
                    Console.WriteLine("Description: " + device.Description);
                    Console.WriteLine("Device Id: " + device.DeviceId);
                    Console.WriteLine("----------------------------------------------");
                }
                Console.WriteLine("Found " + devices.Count + " devices.");

            }
            else
            {
                Console.WriteLine("Invalid adapter number.");
            }
            break;
        case "2":
            Console.WriteLine("Looking for Crestron Devices from all adapters");
            var devices2 = await CrestronDeviceDiscovery.DiscoverFromAllAdapters();
            if (devices2.Count == 0)
            {
                Console.WriteLine("No Crestron devices found.");
                break;
            }
            foreach (var device in devices2)
            {
                Console.WriteLine("Hostname: " + device.Hostname);
                Console.WriteLine("IP Address: " + device.IpAddress);
                Console.WriteLine("Description: " + device.Description);
                Console.WriteLine("Device Id: " + device.DeviceId);
                Console.WriteLine("----------------------------------------------");
            }
                Console.WriteLine("Found " + devices2.Count + " devices.");
            break;
        case "3":
            Console.WriteLine("Enter the IP address of the remote console:");
            var ipAddress = Console.ReadLine();
            Console.WriteLine("Username:");
            var username = Console.ReadLine();
            Console.WriteLine("Password:");
            var password = GetPassword();
            Console.WriteLine($"Looking for Crestron Devices from remote console at {ipAddress}");
            if (ipAddress == null || username == null || password == null)
            {
                Console.WriteLine("IP address, username, and password cannot be null.");
                break;
            }
            var devices3 = await CrestronDeviceDiscovery.RemoteDiscovery(ipAddress, username, password);
            if (devices3.Count == 0)
            {
                Console.WriteLine("No Crestron devices found.");
                break;
            }
            foreach (var device in devices3)
            {
                Console.WriteLine("Hostname: " + device.Hostname);
                Console.WriteLine("IP Address: " + device.IpAddress);
                Console.WriteLine("Description: " + device.Description);
                Console.WriteLine("Device Id: " + device.DeviceId);
                Console.WriteLine("----------------------------------------------");
            }
                Console.WriteLine("Found " + devices3.Count + " devices.");
            break;
        case "X":
        case "x":
            break;
        default:
            Console.WriteLine("Invalid choice.");
            return;
    }
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine();
} while (choice.ToLower() != "x");


static string GetPassword()
{
    string password = "";
    ConsoleKeyInfo key;
    do
    {
        key = Console.ReadKey(true);
        // Backspace Should Not Work
        if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
        {
            password += key.KeyChar;
            Console.Write("*");
        }
        else
        {
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, (password.Length - 1));
                Console.Write("\b \b");
            }
        }
    }
    // Stops Receving Keys Once Enter is Pressed
    while (key.Key != ConsoleKey.Enter);
    Console.WriteLine();
    return password;
}

