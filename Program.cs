using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

class Device
{
    public DeviceInformation Info { get; set; }
    public BluetoothLEAdvertisementReceivedEventArgs Advert { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsAlive { get; set; }
    public string PairingStatus { get; set; }
    public MiTemp? MiTemp { get; set; }
}

class MiTemp
{
    public decimal Temperature { get; set; }
    public decimal Humidity { get; set; }
    public decimal BatteryPercent { get; set; }
    public decimal BatteryMv { get; set; }
}

class Program
{
    static Dictionary<string, Device> devices = new Dictionary<string, Device>();

    static void Main(string[] args)
    {

        var advWatcher = CreateAdvertisementWatcher();
        var devWatcher = CreateDeviceWatcher();

        advWatcher.Start();
        devWatcher.Start();

        Console.ReadLine();
    }

    private static BluetoothLEAdvertisementWatcher CreateAdvertisementWatcher()
    {
        var watcher = new BluetoothLEAdvertisementWatcher();
        // watcher.SignalStrengthFilter.InRangeThresholdInDBm = -70;
        // watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -75;
        // watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000);
        watcher.AllowExtendedAdvertisements = true;
        watcher.ScanningMode = BluetoothLEScanningMode.Active;

        watcher.Received += (w, eventArgs) =>
        {
            Task.Run(async () =>
            {
                var dev = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress, eventArgs.BluetoothAddressType);
                if (dev == null) return;
                lock (devices)
                {
                    if (!devices.ContainsKey(dev.DeviceId))
                    {
                        devices.Add(dev.DeviceId, new Device()
                        {
                            Advert = eventArgs
                        });
                    }
                    else
                    {
                        devices[dev.DeviceId].Advert = eventArgs;
                    }
                }

                var mitemp = ParseAdvert(eventArgs.Advertisement);
                if (mitemp != null) devices[dev.DeviceId].MiTemp = mitemp;

                PrintDevice(dev.DeviceId, devices[dev.DeviceId]);
            });
            //Console.WriteLine(JsonConvert.SerializeObject(eventArgs));
            //Console.WriteLine($"ADV data: {dataString}");
            //Console.WriteLine($"ADV manufacturer data: {manufacturerDataString}");
        };
        watcher.Stopped += (w, e) =>
        {
            System.Console.WriteLine("WATCHER IS DEAD:");
            System.Console.WriteLine(e.Error);
        };

        return watcher;
    }

    private static MiTemp? ParseAdvert(BluetoothLEAdvertisement advertisement)
    {
        var sections = advertisement.DataSections;
        foreach (var section in sections)
        {
            if (section.DataType == 22)
            {
                var data = new byte[section.Data.Length];
                using var reader = DataReader.FromBuffer(section.Data);
                reader.ReadBytes(data);

                if (data.Length >= 17 && data[0] == 0x1A && data[1] == 0x18)
                {
                    var dataStr = BitConverter.ToString(data);
                    // custom pvvx format
                    var tempInt = (data[9] << 8) | data[10];
                    var temp = (decimal)tempInt / 100;

                    var humInt = (data[11] << 8) | data[12];
                    var hum = (decimal)humInt / 100;

                    var batMv = data[13];
                    var batLvl = data[14];

                    return new MiTemp()
                    {
                        BatteryMv = batMv,
                        BatteryPercent = batLvl,
                        Humidity = hum,
                        Temperature = temp
                    };
                };
            }
        }

        return null;
    }

    private static async Task Pair(Device dev)
    {
        System.Console.WriteLine($"pairing with {dev.Info.Id}");
        DevicePairingResult result = await dev.Info.Pairing.PairAsync(DevicePairingProtectionLevel.None);
        System.Console.WriteLine($"pairing status [{dev.Info.Id}]: {result.Status}");

        dev.PairingStatus = result.ToString();
    }

    private static DeviceWatcher CreateDeviceWatcher()
    {
        // Additional properties we would like about the device.
        // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
        string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

        // BT_Code: Example showing paired and non-paired in a single query.
        string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

        var deviceWatcher =
                DeviceInformation.CreateWatcher(
                    aqsAllBluetoothLEDevices,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint);

        // Register event handlers before starting the watcher.
        deviceWatcher.Added += (w, info) =>
        {
            //Console.WriteLine($"Added: {info.Id} {info.Name} {JsonConvert.SerializeObject(info)}");

            lock (devices)
            {
                if (!devices.ContainsKey(info.Id))
                {
                    var dev = new Device()
                    {
                        IsAlive = true,
                        Info = info
                    };
                    if (!string.IsNullOrEmpty(info.Name))
                    {
                        devices.Add(info.Id, dev);
                        //Task.Run(() => Pair(dev));
                        //PrintDevices(advertOnly: true);
                    }
                }
                else
                {
                    devices[info.Id].Info = info;
                }
            }
        };
        deviceWatcher.Updated += (w, info) =>
        {
            // if (info.Kind == DeviceInformationKind.AssociationEndpoint)
            // {
            //     return;
            // }

            // Console.WriteLine($"Updated: {info.Id} {info.Kind} {JsonConvert.SerializeObject(info)}");
        };
        deviceWatcher.Removed += (w, d) =>
        {
            //Console.WriteLine($"Removed: {d.Id}");

            if (devices.ContainsKey(d.Id))
            {
                devices[d.Id].IsAlive = false;
            }
        };
        deviceWatcher.EnumerationCompleted += (w, o) =>
        {
            Console.WriteLine("Enumeration completed");
        };

        return deviceWatcher;
    }


    private static void PrintDevices(bool advertOnly)
    {
        System.Console.WriteLine("== DEVICES: ==");
        foreach (var dev in devices)
        {
            if (advertOnly && dev.Value.Advert == null) continue;
            PrintDevice(dev.Key, dev.Value);
        }
        System.Console.WriteLine("==============");
    }

    private static void PrintDevice(string id, Device? dev)
    {
        System.Console.WriteLine($"{id} {dev?.Info?.Name} isPaired: {dev?.Info?.Pairing?.IsPaired} advert: {AsString(dev?.Advert)}");
        if (dev?.MiTemp != null)
        {
            System.Console.WriteLine($"[{dev.Info.Name}] temp: {dev.MiTemp.Temperature} hum: {dev.MiTemp.Humidity} bat: {dev.MiTemp.BatteryPercent}% ({dev.MiTemp.BatteryMv}mV)");
        }
    }

    private static string AsString(BluetoothLEAdvertisementReceivedEventArgs? advert)
    {

        return $"[{string.Join(",", advert.Advertisement.ServiceUuids)}]" + AsString(advert.Advertisement.DataSections) + " manufacturer: " + AsString(advert.Advertisement.ManufacturerData);
    }

    private static string AsString(IReadOnlyDictionary<string, object> properties)
    {
        var sb = new StringBuilder();

        foreach (var kvp in properties)
        {
            sb.AppendLine($"{kvp.Key}: {kvp.Value}");
        }

        return sb.ToString();
    }

    private static string AsString(IList<BluetoothLEManufacturerData> manufacturerSection)
    {
        string manufacturerDataString = "";
        foreach (var section in manufacturerSection)
        {
            // Only print the first one of the list
            var data = new byte[section.Data.Length];
            using (var reader = DataReader.FromBuffer(section.Data))
            {
                reader.ReadBytes(data);
            }
            // Print the company ID + the raw data in hex format
            manufacturerDataString += $"{section.CompanyId}: {BitConverter.ToString(data)} | ";
        }

        return manufacturerDataString;
    }

    private static string AsString(IList<BluetoothLEAdvertisementDataSection> dataSection)
    {
        string dataString = "";
        foreach (var section in dataSection)
        {
            // Only print the first one of the list
            var data = new byte[section.Data.Length];
            using (var reader = DataReader.FromBuffer(section.Data))
            {
                reader.ReadBytes(data);
            }
            // Print the company ID + the raw data in hex format
            dataString += $"{section.DataType}: {BitConverter.ToString(data)} | ";
        }

        return dataString;
    }
}
