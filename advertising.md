Advertising format:

https://github.com/pvvx/ATC_MiThermometer#advertising-format-of-the-custom-firmware

The custom firmware sends every minute an update of advertising data on the UUID 0x181A with the Tempereature, Humidity and Battery data.

The format of the advertising data is as follow:

Byte 5-10 MAC in correct order

Byte 11-12 Temperature in int16

Byte 13 Humidity in percent

Byte 14 Battery in percent

Byte 15-16 Battery in mV uint16_t

Byte 17 frame packet counter

Example: 0x0e, 0x16, 0x1a, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xaa, 0xaa, 0xbb, 0xcc, 0xdd, 0xdd, 0x00


01 23 45 67 89 01 23 45 67 89
41-54-43-5F-4A-45-52-45-4D-49

98 76 54 32 10 98 76 54 32 10


BluetoothLE#BluetoothLE5c:f3:70:95:06:f7-a4:c1:38:06:46:04 LYWSD03MMC isPaired: False advert: 9: 4C-59-57-53-44-30-33-4D-4D-43 | 
BluetoothLE#BluetoothLE5c:f3:70:95:06:f7-a4:c1:38:60:76:62 LYWSD03MMC isPaired: True advert: 9: 4C-59-57-53-44-30-33-4D-4D-43 | 
BluetoothLE#BluetoothLE5c:f3:70:95:06:f7-a4:c1:38:f1:8a:4a ATC_JEREMI isPaired: False advert: 9: 41-54-43-5F-4A-45-52-45-4D-49 | 

uint8_t     size;   // = 19
uint8_t     uid;    // = 0x16, 16-bit UUID
uint16_t    UUID;   // = 0x181A, GATT Service 0x181A Environmental Sensing
uint8_t     MAC[6]; // [0] - lo, .. [6] - hi digits
int16_t     temperature;    // x 0.01 degree
uint16_t    humidity;       // x 0.01 %
uint16_t    battery_mv;     // mV
uint8_t     battery_level;  // 0..100 %
uint8_t     counter;        // measurement count
uint8_t     flags;  // GPIO_TRG pin (marking "reset" on circuit board) flags: 
                    // bit0: Reed Switch, input
                    // bit1: GPIO_TRG pin output value (pull Up/Down)
                    // bit2: Output GPIO_TRG pin is controlled according to the set parameters
                    // bit3: Temperature trigger event
                    // bit4: Humidity trigger event

01 23 45 67 89 01 23 45 67 89 01 23 45 67 89 01 23
181A |MAC                 |TEMP |HUM  |mv|lv|ct|fg|
--------------------------------------------------
1A-18-4A-8A-F1-38-C1-A4-3A-08-5F-0C-95-0B-55-37-04
1A-18-4A-8A-F1-38-C1-A4-B2-08-A3-0B-94-0B-54-3F-04


01 23 45 67 89 01 23 45 67 89
-----------------------------
41-54-43-5F-50-49-45-54-52-4F
41-54-43-5F-4A-45-52-45-4D-49
41-54-43-5F-50-49-45-54-52-4F