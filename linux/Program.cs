using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;
using Newtonsoft.Json;

TimeSpan timeout = TimeSpan.FromSeconds(15);

if (args.Length < 1 || args.Length > 2 || args[0].ToLowerInvariant() == "-h" || !int.TryParse(args[0], out int scanSeconds))
{
    Console.WriteLine("Usage: scan <SecondsToScan> [adapterName]");
    Console.WriteLine("Example: scan 15 hci0");
    return;
}

IAdapter1 adapter;
if (args.Length > 1)
{
    Console.WriteLine($"Getting adapter {args[1]}...");
    adapter = await BlueZManager.GetAdapterAsync(args[1]);
}
else
{
    Console.WriteLine("Getting any adapter...");
    var adapters = await BlueZManager.GetAdaptersAsync();
    if (adapters.Count == 0)
    {
        throw new Exception("No Bluetooth adapters found.");
    }

    adapter = adapters.First();
}

var adapterPath = adapter.ObjectPath.ToString();
var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);
Console.WriteLine($"Using Bluetooth adapter {adapterName}");

Console.WriteLine("Getting known devices...");
// Print out the devices we already know about.
var devices = await adapter.GetDevicesAsync();
foreach (var device in devices)
{
    string deviceDescription = await GetDeviceDescriptionAsync(device);
    Console.WriteLine(deviceDescription);
}
Console.WriteLine($"{devices.Count} device(s) found ahead of scan.");

Console.WriteLine();

// Scan for more devices.
Console.WriteLine($"Scanning for {scanSeconds} seconds...");

int newDevices = 0;
using (await adapter.WatchDevicesAddedAsync(async device => {
    newDevices++;
    // Write a message when we detect new devices during the scan.
    string deviceDescription = await GetDeviceDescriptionAsync(device);
    Console.WriteLine($"[NEW] {deviceDescription}");
}))
{
    await adapter.StartDiscoveryAsync();
    await Task.Delay(TimeSpan.FromSeconds(scanSeconds));
    await adapter.StopDiscoveryAsync();
}
Console.WriteLine($"Scan complete. {newDevices} new device(s) found.");


static async Task<string> GetDeviceDescriptionAsync(IDevice1 device)
{
    var deviceProperties = await device.GetAllAsync();
    var enviromentalService = "0000181a-0000-1000-8000-00805f9b34fb";
    var sensorData = deviceProperties.ServiceData.ContainsKey(enviromentalService) ? deviceProperties.ServiceData[enviromentalService] as byte[] : new byte[0];
    return $"{deviceProperties.Alias} (Address: {deviceProperties.Address}, RSSI: {deviceProperties.RSSI} data: {JsonConvert.SerializeObject(deviceProperties.ServiceData)} sensor: {BitConverter.ToString(sensorData)}";
}