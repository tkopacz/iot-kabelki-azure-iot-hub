#pragma once
#include <ppl.h>
#include <atomic>
#include <collection.h>
#include <ppltasks.h>
#include <concurrent_vector.h>
#include <Winbase.h>

#include <cvt/wstring>
#include <codecvt>

namespace TK_LCDlcm1602DriverWRC
{
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

	//#define En 0b00000100  // Enable bit
	//#define Rw 0b00000010  // Read/Write bit
	//#define Rs 0b00000001  // Register select bit
	#define En 0x04
	#define Rw 0x02
	#define Rs 0x01
	public ref class LCDI2C sealed
    {
	public:
		LCDI2C(BYTE lcd_Addr, BYTE lcd_cols, BYTE lcd_rows);
		void begin(BYTE cols, BYTE rows, BYTE charsize);
		void clear();
		void home();
		void setCursor(BYTE col, BYTE row);
		void noDisplay();
		void display();
		void noCursor();
		void cursor();
		void noBlink();
		void blink();
		void scrollDisplayLeft();
		void scrollDisplayRight();
		void leftToRight();
		void rightToLeft();
		void autoscroll();
		void noAutoscroll();
		void noBacklight();
		void backlight();

		void createChar(BYTE location, const Platform::Array<BYTE>^ charmap);
		
		//Middleware
		void write(BYTE b);
		void command(BYTE b);

		//Windows IoT specific
		Windows::Foundation::IAsyncOperation<bool>^ InitAsync();

		void printstr(Platform::String^ str);

	private:
		void init_priv();
		void send(BYTE value, BYTE mode);
		void write4bits(BYTE value);
		void expanderWrite(BYTE _data);
		void pulseEnable(BYTE _data);
		
		BYTE _Addr;
		BYTE _displayfunction;
		BYTE _displaycontrol;
		BYTE _displaymode;
		BYTE _numlines;
		BYTE _cols;
		BYTE _rows;
		BYTE _backlightval;

		//Windows IoT
		Windows::Devices::I2c::I2cDevice^ _device;

		Platform::Array<BYTE>^ _arr;

	};
}
