﻿#pragma once

// commands
#define LCD_CLEARDISPLAY 0x01
#define LCD_RETURNHOME 0x02
#define LCD_ENTRYMODESET 0x04
#define LCD_DISPLAYCONTROL 0x08
#define LCD_CURSORSHIFT 0x10
#define LCD_FUNCTIONSET 0x20
#define LCD_SETCGRAMADDR 0x40
#define LCD_SETDDRAMADDR 0x80

// flags for display entry mode
#define LCD_ENTRYRIGHT 0x00
#define LCD_ENTRYLEFT 0x02
#define LCD_ENTRYSHIFTINCREMENT 0x01
#define LCD_ENTRYSHIFTDECREMENT 0x00

// flags for display on/off control
#define LCD_DISPLAYON 0x04
#define LCD_DISPLAYOFF 0x00
#define LCD_CURSORON 0x02
#define LCD_CURSOROFF 0x00
#define LCD_BLINKON 0x01
#define LCD_BLINKOFF 0x00

// flags for display/cursor shift
#define LCD_DISPLAYMOVE 0x08
#define LCD_CURSORMOVE 0x00
#define LCD_MOVERIGHT 0x04
#define LCD_MOVELEFT 0x00

// flags for function set
#define LCD_8BITMODE 0x10
#define LCD_4BITMODE 0x00
#define LCD_2LINE 0x08
#define LCD_1LINE 0x00
#define LCD_5x10DOTS 0x04
#define LCD_5x8DOTS 0x00

// flags for backlight control
#define LCD_BACKLIGHT 0x08
#define LCD_NOBACKLIGHT 0x00

#define En 0b00000100  // Enable bit
#define Rw 0b00000010  // Read/Write bit
#define Rs 0b00000001  // Register select bit

class LiquidCrystal_I2C {
public:
	LiquidCrystal_I2C(BYTE lcd_Addr, BYTE lcd_cols, BYTE lcd_rows);
	void begin(BYTE cols, BYTE rows, BYTE charsize = LCD_5x8DOTS);
	void clear();
	void home();
	void noDisplay();
	void display();
	void noBlink();
	void blink();
	void noCursor();
	void cursor();
	void scrollDisplayLeft();
	void scrollDisplayRight();
	void printLeft();
	void printRight();
	void leftToRight();
	void rightToLeft();
	void shiftIncrement();
	void shiftDecrement();
	void noBacklight();
	void backlight();
	void autoscroll();
	void noAutoscroll();
	void createChar(BYTE, BYTE[]);
	void setCursor(BYTE, BYTE);
	void write(BYTE);
	void command(BYTE);
	void init();

	////compatibility API function aliases
	void blink_on();						// alias for blink()
	void blink_off();       					// alias for noBlink()
	void cursor_on();      	 					// alias for cursor()
	void cursor_off();      					// alias for noCursor()
	void setBacklight(BYTE new_val);				// alias for backlight() and nobacklight()
	void load_custom_character(BYTE char_num, BYTE *rows);	// alias for createChar()
	void printstr(const char[]);

private:
	void init_priv();
	void send(BYTE, BYTE);
	void write4bits(BYTE);
	void expanderWrite(BYTE);
	void pulseEnable(BYTE);
	BYTE _Addr;
	BYTE _displayfunction;
	BYTE _displaycontrol;
	BYTE _displaymode;
	BYTE _numlines;
	BYTE _cols;
	BYTE _rows;
	BYTE _backlightval;
	void delay(int);
	void delayMicroseconds(int);
};
