PLANTOWER PMSx003 Laser PM2.5 DUST SENSOR 
with Windows IoT Core

This is a demo app showing my hopefully easy to use Windows IoT Core C# driver for PLANTOWER PMSx003 Laser Dust Sensors.  This driver should work with 5003, 6003, 7003 sensors.

These sensors use UART to send their data, in two different modes.  Active Mode sends a constant stream of data, whereas Passive Mode only sends data when requested using the Passive Read command.  There is also a software method to put the sensor into a lower power sleep mode and a software wake command. You can also use the 'Set' pin to do the same by setting it low for sleep and high for active and there is also a reset pin for resetting the device.

NOTE the jump on the back of the US-100, jumper on UART, jumper off ping/echo

PLANTOWER PMSx003 Laser PM2.5 DUST SENSOR 
 Requires 5 v DC to drive the air sampling fans, but pins all use 3.3 v DC so for example its safe to use with a Raspberry Pi.

NOTE I use Windows Template Studio https://github.com/Microsoft/WindowsTemplateStudio/ to help me quickly and easily build these sample apps. 

Purchased from AliExpress https://www.aliexpress.com/item/1-set-Laser-PM2-5-PMS7003-G7-High-precision-laser-dust-concentration-sensor-digital-dust-particles/32826370928.html?spm=a2g0s.9042311.0.0.96224c4dRWnn5V



