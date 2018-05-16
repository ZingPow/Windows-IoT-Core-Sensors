US-100 Ultrasonic Sensor Module
with Windows IoT Core

This is a demo app showing some different method for using a US-100 Ultrasonic Sensor Module with Windows IoT Core and some of the features offered by this sensor.  

A very nice feature of this sensor is that it can offload the measurement finding such that you can call the sensor to do a measurement and it 
will return the actual measurement value which is even corrected for temperature effects (temperature affects the density and hence speed of sound through the ari), you can even get a temperature reading from the module.  This is all done via a serial (UART) interface.  You can use the usual ping/echo measurements if you manually want to get the distance, but the UART method is really much nice and better as its pretty much fire and forget.

NOTE the jump on the back of the US-100, jumper on UART, jumper off ping/echo

US-100 Ultrasonic Sensor Module
 DC 2.4V - 5V
 Temperature Compensation
 Range Distance 450cm (I was able to measure further)
 Output mode: level or UART

NOTE I use WIndows Template Studio https://github.com/Microsoft/WindowsTemplateStudio/ to help me quickly and easily build these sample apps. 

Purchased from AliExpress https://www.aliexpress.com/item/1-pc-US-100-Ultrasonic-Sensor-Module-With-Temperature-Compensation-Range-Distance-450cm-For-Arduino/1670433457.html?spm=a2g0s.9042311.0.0.42f84c4dOG5sZI



