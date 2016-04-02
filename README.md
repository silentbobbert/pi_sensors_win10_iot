# Raspberry PI (2 & 3 & later hopefully) with Windows 10 IoT, communicating with Sensors


## This project is the starter for giving a robotics project eyes so it can sense the physical world around it without destroying it or itself!
So far I have 
* Proximity and Ambient Light Sensor [VCNL4000](https://www.adafruit.com/products/466) - the link is to the later 4010, but should be compatible
* Analogue to Digital Converter (or ADC) [ADS1115](https://www.adafruit.com/products/1085) - to communicate with upto 4 analogue devices, such as IR distances sensors
* [Sharp 2Y0A21 Analogue IR Distance Sensor](https://www.sparkfun.com/products/242) - Raw value provider to be converted by the ADC ADS1115 or other ADC if you have one
* [Raspberry PI Model 3](https://www.raspberrypi.org/products/raspberry-pi-3-model-b/) - Almost credit card sized micro computer capable of running Windows 10 IoT
* [Windows 10 IoT](https://ms-iot.github.io/content/en-US/Downloads.htm) - Cut down version of windows. Can run background service like applications or UWP/XAML GUI applications
* I have plans to implement [HC-SR04 Ultrasonic Range Finder](http://www.instructables.com/id/Simple-Arduino-and-HC-SR04-Example/) to give my projects 3 ways to see!

The HC-SR04 is a really cheap sensor. I decided to get a few after seeing this [Windows 10 IoT Core: UltraSonic Distance Mapper](https://www.hackster.io/AnuragVasanwala/windows-10-iot-core-ultrasonic-distance-mapper-d94d63)

So far my GUI in this project is not important to me, there may not even be a GUI in the project as I get closer to completing it. Its only present to help me see live readings without a companion device
like a PC and for things like burn-in tests, making sure all the sensors collect values reliably for long periods of time.

Eventually I may start batching and streaming data in a more traditional IoT implementation, although the "robot" I am thinking of creating will hopefully
be pretty autonomous so this data may even live and die on the device itself.

The code for this project is C# and developed with Visual Studio 2015

[YouTube video demo, Windows 10 IoT running on Raspberry Pi 3 talking to some sensors](https://www.youtube.com/watch?v=rdkE51hjHU8)

