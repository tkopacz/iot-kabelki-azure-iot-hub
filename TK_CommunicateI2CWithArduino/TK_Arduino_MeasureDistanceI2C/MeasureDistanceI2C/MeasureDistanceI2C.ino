/*
 Name:		MeasureDistanceI2C.ino
 Created:	10/3/2015 7:05:47 PM
 Author:	tkopacz
*/

#include <Wire.h>


#define trigPin 6
#define echoPin 7
#define sensorPin 0
// the setup function runs once when you press reset or power the board
void setup() {
	//Serial.begin(9600);
	pinMode(trigPin, OUTPUT);
	pinMode(echoPin, INPUT);
  Wire.onReceive(receiveEvent); //Zgłaszane przy wysyłaniu (nadawca: Write)
  Wire.onRequest(requestEvent); //Zgłaszane przy żądaniu odczytu (nadawca: Read)
  Wire.begin(18); //Jesteśmy slave numer 18
  //Serial.println("Start");

}

long duration, distance,sensorValue; //Tylko ostatnie

// the loop function runs over and over again until power down or reset
void loop() {
  sensorValue = analogRead(sensorPin);
	digitalWrite(trigPin, HIGH);
	delayMicroseconds(1000);
	digitalWrite(trigPin, LOW);
	duration = pulseIn(echoPin, HIGH);
	distance = (((long)duration * 10) / 2) / 29.1; //To zależy od wilgotności, temperatury itp... No ale...
  //Serial.print(sensorValue);
  //Serial.print(", ");
	//Serial.print(duration);
	//Serial.print(", ");
	//Serial.print(distance);
	//Serial.println(" mm");
	//delay(500);
}



byte x = 0;
byte arr[2 + 3 * 2];
int sum=0;

/*
 * Sprawdzać kabelki
 * RPI2: 3.3V 
 * RPI2: SDA1 -> Uno: A4 (zielony)
 * RPI2: SCL1 -> Uno: A5 (biały)
 * GND (czarny)
 * Leonardo:
 */
 
void receiveEvent(int howMany) {  //Odebranie I2C, howMany - ile bajtów
    sum = 0;
    while (Wire.available() > 0){
        byte b = Wire.read();
        //Serial.print(b, DEC);
        sum+=b;
    }

}

void requestEvent() {
    arr[0] = sum & 0xFF;
    arr[1] = (sum>>8)& 0xFF; 

    arr[2] = (duration) & 0xFF;
    arr[3] = (duration>>8) & 0xFF;

    arr[4] = (distance) & 0xFF;
    arr[5] = (distance>>8) & 0xFF;

    arr[6] = (sensorValue) & 0xFF;
    arr[7] = (sensorValue>>8) & 0xFF;
    Wire.write(arr,sizeof(arr));
}


