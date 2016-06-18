#include <Wire.h>
#include "ADS1015.h"

#define MyAddress 0x41
#define trigPin 9
#define echoPin 8
#define onBoardLED 13

//#define  RESULTS_SIZE 4 // A long takes 4 bytes
//#define  COMMAND_REGISTER_SIZE 1 //8-bit command register

/********* Global  Variables  ***********/

//byte resultRegister[RESULTS_SIZE];
//byte commandRegister[COMMAND_REGISTER_SIZE];
volatile unsigned short SonarPulseLength;
volatile int16_t IRValue;

Adafruit_ADS1115 ads;

void setup()
{
	// initialize digital pin 13 as an output.
	pinMode(onBoardLED, OUTPUT);
  
	/* Initialize I2C Slave & assign call-back function 'onReceive' */
	Wire.begin(MyAddress);
	//Wire.onReceive(receiveEvent);
	Wire.onRequest(requestEvent);

	ads.begin();
}

void loop()
{
	SonarPulseLength = Ping();
	ReactToDistance(SonarPulseLength);
	IRValue = TakeIRReading();
}

int16_t TakeIRReading()
{
	int16_t adc0;
	adc0 = ads.readADC_SingleEnded(0);
	return adc0;
}

/*Win10 IoT can send Command Bytes to a command register etc*/
/*
void receiveEvent(int numberOfBytes)
{
  for (int a = 0; a < numberOfBytes; a++)
  {
	if ( a < COMMAND_REGISTER_SIZE)
	{
	  commandRegister[a] = Wire.read();
	}
	else
	{
	  Wire.read();  // if we receive more data then allowed just throw it away
	}
  }
}
*/
/*Win10 IoT can read data from this slave. Can use information in the Command to vary response*/
void requestEvent()
{
	byte sonarArray[2];
	unsigned short duration = (unsigned short)SonarPulseLength;

	sonarArray[0] = (byte)((duration >> 8) & 0xff);
	sonarArray[1] = (byte)(duration & 0xff);
	
	Wire.write(sonarArray, 2);
	ReactToDistance(duration);

  byte irArray[2];
  int16_t raw = (int16_t)IRValue;

  irArray[0] = (byte)((raw >> 8) & 0xff);
  irArray[1] = (byte)(raw & 0xff);
  Wire.write(irArray, 2);  
}

unsigned short Ping()
{
	long duration;
  
	digitalWrite(trigPin, LOW); 
	delayMicroseconds(2);
	digitalWrite(trigPin, HIGH);
	delayMicroseconds(10);
	digitalWrite(trigPin, LOW);
	duration = pulseIn(echoPin, HIGH);
	if(duration < 60000)
	{
		return (unsigned short)duration;
	}
	return 60000;
}

void ReactToDistance(unsigned short duration)
{
	double distance = (duration/2) / 29.1;
	if (distance < 5) 
	{
		digitalWrite(onBoardLED, HIGH);   // turn the LED on (HIGH is the voltage level)
	}
	else 
	{
		digitalWrite(onBoardLED, LOW);   // turn the LED on (LOW is the voltage level)
	}
}

