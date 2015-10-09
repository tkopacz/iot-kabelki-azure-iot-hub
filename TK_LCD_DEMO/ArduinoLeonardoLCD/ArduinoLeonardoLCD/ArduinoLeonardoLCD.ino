/*
 Name:		ArduinoLeonardoLCD.ino
 Created:	9/6/2015 9:26:40 PM
 Author:	tkopacz
*/

// the setup function runs once when you press reset or power the board
#include <Wire.h>
#include <LiquidCrystal_I2C.h>

#if defined(ARDUINO) && ARDUINO >= 100
#define printByte(args)  write(args);
#else
#define printByte(args)  print(args,BYTE);
#endif

uint8_t bell[8] = { 0x4,0xe,0xe,0xe,0x1f,0x0,0x4 };
uint8_t note[8] = { 0x2,0x3,0x2,0xe,0x1e,0xc,0x0 };
uint8_t clock[8] = { 0x0,0xe,0x15,0x17,0x11,0xe,0x0 };
uint8_t heart[8] = { 0x0,0xa,0x1f,0x1f,0xe,0x4,0x0 };
uint8_t duck[8] = { 0x0,0xc,0x1d,0xf,0xf,0x6,0x0 };
/*
00000000
00001100
00011101
00001111
00001111
00000110
00000000
*/
uint8_t check[8] = { 0x0,0x1,0x3,0x16,0x1c,0x8,0x0 };
uint8_t cross[8] = { 0x0,0x1b,0xe,0x4,0xe,0x1b,0x0 };
uint8_t retarrow[8] = { 0x1,0x1,0x5,0x9,0x1f,0x8,0x4 };


LiquidCrystal_I2C lcd(0x27, 2, 16);

void setup() {
	Serial.begin(115200);
	lcd.init();                      // initialize the lcd 
	lcd.backlight();
	//lcd.noBacklight();
	lcd.setCursor(0, 0);

	lcd.createChar(0, bell);
	lcd.createChar(1, note);
	lcd.createChar(2, clock);
	lcd.createChar(3, heart);
	lcd.createChar(4, duck);
	lcd.createChar(5, check);
	lcd.createChar(6, cross);
	lcd.createChar(7, retarrow);

	lcd.print("ABC");
	Serial.println("powinno by? ABC");
	//lcd.print(1);
}

// the loop function runs over and over again until power down or reset
uint8_t bk = 0;
void loop() {
	uint8_t i = 0;
	while (1) {
		lcd.clear();
		lcd.print("Codes 0x"); lcd.print(i, HEX);
		lcd.print("-0x"); lcd.print(i + 16, HEX);
		lcd.setCursor(0, 1);
		for (int j = 0; j<16; j++) {
			lcd.write(i + j);
		}
		//i += 16;

		delay(1000);
	}
}
