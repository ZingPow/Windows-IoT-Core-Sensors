ML8511 UVB UV Rays Sensor
with Windows IoT Core

This is a demo app showing how to use a ML8511 UVB UV Rays Sensor with Windows IoT Core and some of the features offered by this sensor.  Since the ML8511 is an Analog
device you will need an ADC (Analog to Digital Converter) in order to connect it to a Raspberry Pi given the Raspberry Pi only has Digital IO pins.  For this demo I use
a MCP3008 ADC chip to connect the HL8511 to the Raspberry Pi (specifically a Raspberry Pi 2, but any this code should work on any Windows IoT Core device).  One of the features that this board offers is enable pin where if the pin is low the chip goes into
low power sleep mode (note not all ML8511 breakout boards I've seen have this feature).

NOTE I use WIndows Template Studio https://github.com/Microsoft/WindowsTemplateStudio/ to help me quickly and easily build these sample apps. 

Purchased from AliExpress https://www.aliexpress.com/item/Analog-output-module-GY-ML8511-UV-UV-sensor-Sensor-Breakout/32533518448.html?spm=a2g0s.9042311.0.0.XDtojp

ML8511 Description:
The ML8511 breakout is an easy to use ultraviolet light sensor. The MP8511 UV (ultraviolet) Sensor works by outputting an analog signal in relation to the amount of UV light that's detected. This breakout can be very handy in creating devices that warn the user of sunburn or detect the UV index as it relates to weather conditions.
This sensor detects 280-390nm light most effectively. This is categorized as part of the UVB (burning rays) spectrum and most of the UVA (tanning rays) spectrum. It outputs a analog voltage that is linearly related to the measured UV intensity (mW/cm2). If your microcontroller can do an analog to digital signal conversion then you can detect the level of UV!
Oki (OKI) launched the built-in amplifier of the ultraviolet (UV) sensor IC--ML8511. The products used on the insulation cover Silicon (SOI)-CMOS, the company's first analog voltage output, filter the UV sensor. 
OKI high UV sensor IC thanks to easy integration of SOI-CMOS technology, suitable for digital and analog circuits. OKI said, the company future will flexible using this a specialist, strengthening and connection microprocessor of digital output circuit, then and sense measuring type brightness control sensor (Ambient Light Sensor) constitute single chip of commodity lineup; future of products hope not only can do on day of UV volume at a glance, and and at any time can master UV dangerous degrees of decorative sex volume measuring equipment, and will application promotion to appliances equipment, and can carrying type equipment field.
ML8511 analog voltage is proportional to the amount of UV light output. Because of the output voltage, so you can directly connect the built-in a/d d/a converter of the MCU, no photoelectric conversion circuit. And the use of small and thin package, suitable for use in portable equipment.

