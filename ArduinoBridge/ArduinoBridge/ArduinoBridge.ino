#include <Wire.h>

#define MyAddress 0x40
#define trigPin 12
#define echoPin 11
#define onBoardLED 13

//#define  RESULTS_SIZE 4 // A long takes 4 bytes
//#define  COMMAND_REGISTER_SIZE 1 //8-bit command register

/********* Global  Variables  ***********/

//byte resultRegister[RESULTS_SIZE];
//byte commandRegister[COMMAND_REGISTER_SIZE];
volatile unsigned short Value_Duration;

void setup()
{
	// initialize digital pin 13 as an output.
	pinMode(onBoardLED, OUTPUT);
  
	/* Initialize I2C Slave & assign call-back function 'onReceive' */
	Wire.begin(MyAddress);
	//Wire.onReceive(receiveEvent);
	Wire.onRequest(requestEvent);
}

void loop()
{
	Value_Duration = Ping();
	ReactToDistance(Value_Duration);
  
  // Wait for 50 ms
  //delay(50);
  
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
	byte resultArray[2];
	unsigned short duration = (unsigned short)Value_Duration;

	resultArray[0] = (byte)((duration >> 8) & 0xff);
	resultArray[1] = (byte)(duration & 0xff);
	
	Wire.write(resultArray, 2);
	ReactToDistance(duration);
}

long Ping()
{
	unsigned short duration;
  
	digitalWrite(trigPin, LOW); 
	delayMicroseconds(2);
	digitalWrite(trigPin, HIGH);
	delayMicroseconds(10);
	digitalWrite(trigPin, LOW);
	duration = pulseIn(echoPin, HIGH);
	if(duration < 60000)
	{
	return duration;
	}
	return 60000;
}

void ReactToDistance(long duration)
{
	double distance = (duration/2) / 29.1;
	if (distance < 5) 
	{
		digitalWrite(onBoardLED, HIGH);   // turn the LED on (HIGH is the voltage level)
	}
	else 
	{
		digitalWrite(onBoardLED, LOW);   // turn the LED on (HIGH is the voltage level)
	}
}


