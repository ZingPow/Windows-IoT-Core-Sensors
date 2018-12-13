Infrared Flame Sensor
with Windows IoT Core

This is a demo app showing how to use a 4 Pin Infrared Flame Sensor with Windows IoT Core.  This sensor is built using a wide-voltage LM393 comparator and this setup is used for a number of different sensors.  The difference between 3 pin and 4 pin is 3 pin only has a digital pin (ie think switch) whereas the 4 pin includes an analog pin measuring the voltage level.  You can set the sensitivity of the digital pin with the potentiometer adjustment so you can raise an event when the flame status changes. 

Given I'm using a Raspberry Pi 2 running Windows IoT Core, and the Raspberry Pi doesn't have analog pins, I'm also using a ADS1115 ADC to read the voltage values.  The ADS1115 is an I2C device.

I purchased these Flame Sensors as I was thinking of using them to alert concerning flameouts on my BBQ (oh ya I have Raspberry Pi running IoT Core on just about everything in my house), but they work best with flames that emit IR in the wavelength range of 760 nm to 1100 nm and in my quick tests, a Natural Gas or Propane blue flame doesn't emit a lot of IR in this frequency range, where as a yellow flame wood fire does, so I not convinced they are suitable for what I wanted to do with them but given they cost $0.65 each, I bought some for testing rather then spending my time researching IR signature frequencies of various burning materials.  They can also be sensitive to some forms of light which can impact the accuracy of the desired measurement, for example for the screen shots I didn't have a flame but a halogen desk lamp and it was very sensitive to it.

4pin Flame Sensor Fire Detection Module Ignition Source Detects Infrared Receiver
use:
All kinds of flame, fire source detection

Module Features:
1, can detect the flame or the wavelength of 760 nm to 1100 nm range of light, lighter
Test flame distance of 80cm, the larger the flame, the test the farther away
2, the detection angle of about 60 degrees, particularly sensitive to the flame spectrum
3, the sensitivity adjustable (figure blue digital potentiometer adjustment)
4, the comparator output, the signal clean, good waveform, driving ability, more than 15mA
5, with adjustable precision potentiometer adjustment sensitivity
6, the working voltage of 3.3V-5V
7, the output form: DO digital switch output (0 and 1) and AO analog voltage output
8, with a fixed bolt hole, easy to install
9, small board PCB size: 3.2cm x 1.4cm
10. Use a wide-voltage LM393 comparator

Product wiring instructions:

1, VCC then the power supply positive 3.3-5V
2, GND connected to the negative power supply
3, DO TTL switch signal output
4, AO small board analog signal output (voltage signal)

Module Instructions:
1, the flame sensor is most sensitive to the flame, is also a response to ordinary light, generally used for fire alarm purposes.
2, small board output interface can be directly connected with the microcontroller IO port
3, the sensor and the flame to maintain a certain distance, so as to avoid damage to the sensor temperature, the flame test flame distance of 80cm, the flame the greater the test the farther away
4, small board analog output and AD conversion processing, you can get a higher accuracy
5, when the sensor detects a flame, sun, or infrared light, to reach the potentiometer settings threshold, the green indicator will light, DO and output low (0-0.1V or so), the green light is not Light, the DO output voltage of about 3v high

https://www.aliexpress.com/item/4pin-Flame-Sensor-Fire-Detection-Module-Ignition-Source-Detects-Infrared-Receiver/32760879607.html?spm=a2g0s.9042311.0.0.70d34c4dtnnEhr

I2C ADS1115 16 Bit ADC 4 channel Module for Arduino with Programmable Gain Amplifier 2.0V to 5.5V 

WIDE SUPPLY RANGE: 2.0V to 5.5V
LOW CURRENT CONSUMPTION: Continuous Mode: Only 150A Single-Shot Mode: Auto Shut-Down
PROGRAMMABLE DATA RATE: 8SPS to 860SPS
INTERNAL LOW-DRIFT VOLTAGE REFERENCE
INTERNAL OSCILLATOR
INTERNAL PGA
I2C INTERFACE: Pin-Selectable Addresses
FOUR SINGLE-ENDED OR TWO DIFFERENTIAL INPUTS
PROGRAMMABLE COMPARATOR
This board/chip uses I2C 7-bit addresses between 0x48-0x4B, selectable with jumpers.

https://www.aliexpress.com/item/ADS1115-16-Bit-ADC-4-Channel-Channel-Analog-to-Digital-Conversion-Module-ADJUSTABLE/32787199580.html?spm=a2g0s.9042311.0.0.70d34c4dtnnEhr

NOTE I use Windows Template Studio https://github.com/Microsoft/WindowsTemplateStudio/ to help me quickly and easily build these sample apps. 





