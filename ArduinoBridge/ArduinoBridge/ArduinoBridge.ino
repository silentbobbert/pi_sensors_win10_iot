#include <Wire.h>
#include "ADS1015.h"

#define MyAddress 0x07
#define trigPin 9
#define echoPin 8
#define onBoardLED 13

//#define  RESULTS_SIZE 4 // A long takes 4 bytes
//#define  COMMAND_REGISTER_SIZE 1 //8-bit command register

/********* Global  Variables  ***********/

//byte resultRegister[RESULTS_SIZE];
//byte commandRegister[COMMAND_REGISTER_SIZE];
volatile unsigned short SonarPulseLength;
volatile int16_t IRValues[4];
bool _enableSerial;
Adafruit_ADS1115 ads;

void setup()
{
  _enableSerial = true;
  // initialize digital pin 13 as an output.
  pinMode(onBoardLED, OUTPUT);

  /* Initialize I2C Slave & assign call-back function 'onReceive' */
  Wire.begin(MyAddress);
  //Wire.onReceive(receiveEvent);
  Wire.onRequest(requestEvent);
  if (_enableSerial)
  {
    Serial.begin(19200);
  }
  //ads.begin();
}

void loop()
{
  SonarPulseLength = Ping();
  ReactToDistance(SonarPulseLength);
  //TakeIRReading(IRValues);
}

void TakeIRReading(volatile int16_t *buffer)
{
  buffer[0] = ads.readADC_SingleEnded(0);
  buffer[1] = ads.readADC_SingleEnded(1);
  buffer[2] = ads.readADC_SingleEnded(2);
  buffer[3] = ads.readADC_SingleEnded(3);
  if(_enableSerial)
  {
    SendIRResultToSerial(buffer);
  }
}

void SendIRResultToSerial(volatile int16_t *buffer)
{
  Serial.print("ADC 1: ");
  Serial.println(buffer[0]);
  Serial.print("ADC 2: ");
  Serial.println(buffer[1]);
  Serial.print("ADC 3: ");
  Serial.println(buffer[2]);
  Serial.print("ADC 4: ");
  Serial.println(buffer[3]);
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
  byte resultsArray[10];
  unsigned short duration = (unsigned short)SonarPulseLength;

  resultsArray[0] = (byte)((duration >> 8) & 0xff);
  resultsArray[1] = (byte)(duration & 0xff);

/*
  resultsArray[2] = (byte)((IRValues[0] >> 8) & 0xff);
  resultsArray[3] = (byte)(IRValues[0] & 0xff);

  resultsArray[4] = (byte)((IRValues[1] >> 8) & 0xff);
  resultsArray[5] = (byte)(IRValues[1] & 0xff);

  resultsArray[6] = (byte)((IRValues[2] >> 8) & 0xff);
  resultsArray[7] = (byte)(IRValues[2] & 0xff);

  resultsArray[8] = (byte)((IRValues[3] >> 8) & 0xff);
  resultsArray[9] = (byte)(IRValues[3] & 0xff);
*/

  //Wire.write(resultsArray, 10);
  Wire.write(resultsArray, 2);
  ReactToDistance(duration);
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
  if (duration < 60000)
  {
    if(_enableSerial)
    {
      Serial.print("Duration: ");
      Serial.println(duration);  
    }
    return (unsigned short)duration;
  }
  return 60000;
}

void ReactToDistance(unsigned short duration)
{
  double distance = (duration / 2) / 29.1;
  if (distance < 5)
  {
    digitalWrite(onBoardLED, HIGH);   // turn the LED on (HIGH is the voltage level)
  }
  else
  {
    digitalWrite(onBoardLED, LOW);   // turn the LED on (LOW is the voltage level)
  }
}

