HC-SR04 Ultrasonic Sensor Module
with Windows IoT Core

This is a demo app showing some different method for using a HC-SR04 Ultrasonic Sensor Module with Windows IoT Core.  The best option is to use GpioChangeReader as then the OS collects the pin change events with accurate relative timings, so you don't have to poll or used pin change events (not recommneded as the accuracy is poor).

NOTE GpioChangeReader requires a minimum of Windows 10 Creators Update (v10.0.15063.0)
https://docs.microsoft.com/en-us/uwp/api/windows.devices.gpio.gpiochangereader

HC-SR04 Ultrasonic Sensor Module
 DC 5V
Needs a voltage divider on the Echo pin to drop the voltage from 5v to 3.3v for the Raspberry Pi

NOTE I use Windows Template Studio https://github.com/Microsoft/WindowsTemplateStudio/ to help me quickly and easily build these sample apps. 

I'm using a HC-SR04 which is part of a Gadgeteer Module (I loved Gadgeteer) made by GHI Electronics as the nice thing about this is the voltage divider is built into the module.



