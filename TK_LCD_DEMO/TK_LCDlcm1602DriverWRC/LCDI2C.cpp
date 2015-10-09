#include "pch.h"
#include "LCDI2C.h"


using namespace TK_LCDlcm1602DriverWRC;
using namespace Windows::Foundation;
using namespace Windows::Devices::Enumeration;
using namespace Windows::Devices::I2c;

using namespace concurrency;
using namespace std;

using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;


//Based on I2CTestTool
class wexception
{
public:
	explicit wexception(const std::wstring &msg) : msg_(msg) { }
	virtual ~wexception() { /*empty*/ }

	virtual const wchar_t *wwhat() const
	{
		return msg_.c_str();
	}

private:
	std::wstring msg_;
};

// Based on the work by LiquidCrystal I2C

LCDI2C::LCDI2C(BYTE lcd_Addr, BYTE lcd_cols, BYTE lcd_rows)
{
	_Addr = lcd_Addr;
	_cols = lcd_cols;
	_rows = lcd_rows;
	_backlightval = LCD_NOBACKLIGHT;
	_arr = ref new Platform::Array<BYTE>(1);
}



Windows::Foundation::IAsyncOperation<bool>^ LCDI2C::InitAsync() {
	//Init I2C
	return create_async([this]() {
		String^ aqs;
		aqs = I2cDevice::GetDeviceSelector();
		//Get devices based on device selector string
		return concurrency::create_task(DeviceInformation::FindAllAsync(aqs))
			.then([this](task<Windows::Devices::Enumeration::DeviceInformationCollection^> dev) {
			auto dis = dev.get();
			if (dis->Size != 1) {
				throw wexception(L"I2C bus not found");
			}
			String^ id = dis->GetAt(0)->Id;
			//Get I2cDevice for LCD
			return concurrency::create_task(I2cDevice::FromIdAsync(id, ref new I2cConnectionSettings(this->_Addr)))
				.then([id, this](task<Windows::Devices::I2c::I2cDevice^> d) {
				auto device = d.get();
				if (!device) {
					std::wostringstream msg;
					msg << L"Slave address 0x" << std::hex << this->_Addr << L" on bus " << id->Data() << L" is in use. Please ensure that no other applications are using I2C.";
					throw wexception(msg.str());
				}
				this->_device = device;
				init_priv();
				return true;
			});
		}
		);
	});
}

void LCDI2C::init_priv()
{
	//Wire.begin();
	_displayfunction = LCD_4BITMODE | LCD_1LINE | LCD_5x8DOTS;
	begin(_cols, _rows, LCD_5x8DOTS);
	backlight(); //TK: Włączam backlight
}

void LCDI2C::begin(BYTE cols, BYTE lines, BYTE dotsize) {
	if (lines > 1) {
		_displayfunction |= LCD_2LINE;
	}
	_numlines = lines;

	// for some 1 line displays you can select a 10 pixel high font
	if ((dotsize != 0) && (lines == 1)) {
		_displayfunction |= LCD_5x10DOTS;
	}

	// SEE PAGE 45/46 FOR INITIALIZATION SPECIFICATION!
	// according to datasheet, we need at least 40ms after power rises above 2.7V
	// before sending commands. Arduino can turn on way befer 4.5V so we'll wait 50
	CPPHelper::ClockUtil::DelayMilisecond(50);

	// Now we pull both RS and R/W low to begin commands
	expanderWrite(_backlightval);	// reset expanderand turn backlight off (Bit 8 =1)
	CPPHelper::ClockUtil::DelayMilisecond(1000);

	//put the LCD into 4 bit mode
	// this is according to the hitachi HD44780 datasheet
	// figure 24, pg 46

	// we start in 8bit mode, try to set 4 bit mode
	write4bits(0x03 << 4);
	CPPHelper::ClockUtil::DelayMicrosecond(4500); // wait min 4.1ms

							 // second try
	write4bits(0x03 << 4);
	CPPHelper::ClockUtil::DelayMicrosecond(4500); // wait min 4.1ms

							 // third go!
	write4bits(0x03 << 4);
	CPPHelper::ClockUtil::DelayMicrosecond(150);

	// finally, set to 4-bit interface
	write4bits(0x02 << 4);


	// set # lines, font size, etc.
	command(LCD_FUNCTIONSET | _displayfunction);

	// turn the display on with no cursor or blinking default
	_displaycontrol = LCD_DISPLAYON | LCD_CURSOROFF | LCD_BLINKOFF;
	display();

	// clear it off
	clear();

	// Initialize to default text direction (for roman languages)
	_displaymode = LCD_ENTRYLEFT | LCD_ENTRYSHIFTDECREMENT;

	// set the entry mode
	command(LCD_ENTRYMODESET | _displaymode);

	home();

}

/********** high level commands, for the user! */
void LCDI2C::clear() {
	command(LCD_CLEARDISPLAY);// clear display, set cursor position to zero
	CPPHelper::ClockUtil::DelayMicrosecond(2000);  // this command takes a long time!
}

void LCDI2C::home() {
	command(LCD_RETURNHOME);  // set cursor position to zero
	CPPHelper::ClockUtil::DelayMicrosecond(2000);  // this command takes a long time!
}

void LCDI2C::setCursor(BYTE col, BYTE row) {
	int row_offsets[] = { 0x00, 0x40, 0x14, 0x54 };
	if (row > _numlines) {
		row = _numlines - 1;    // we count rows starting w/0
	}
	command(LCD_SETDDRAMADDR | (col + row_offsets[row]));
}

// Turn the display on/off (quickly)
void LCDI2C::noDisplay() {
	_displaycontrol &= ~LCD_DISPLAYON;
	command(LCD_DISPLAYCONTROL | _displaycontrol);
}
void LCDI2C::display() {
	_displaycontrol |= LCD_DISPLAYON;
	command(LCD_DISPLAYCONTROL | _displaycontrol);
}

// Turns the underline cursor on/off
void LCDI2C::noCursor() {
	_displaycontrol &= ~LCD_CURSORON;
	command(LCD_DISPLAYCONTROL | _displaycontrol);
}
void LCDI2C::cursor() {
	_displaycontrol |= LCD_CURSORON;
	command(LCD_DISPLAYCONTROL | _displaycontrol);
}

// Turn on and off the blinking cursor
void LCDI2C::noBlink() {
	_displaycontrol &= ~LCD_BLINKON;
	command(LCD_DISPLAYCONTROL | _displaycontrol);
}
void LCDI2C::blink() {
	_displaycontrol |= LCD_BLINKON;
	command(LCD_DISPLAYCONTROL | _displaycontrol);
}

// These commands scroll the display without changing the RAM
void LCDI2C::scrollDisplayLeft(void) {
	command(LCD_CURSORSHIFT | LCD_DISPLAYMOVE | LCD_MOVELEFT);
}
void LCDI2C::scrollDisplayRight(void) {
	command(LCD_CURSORSHIFT | LCD_DISPLAYMOVE | LCD_MOVERIGHT);
}

// This is for text that flows Left to Right
void LCDI2C::leftToRight(void) {
	_displaymode |= LCD_ENTRYLEFT;
	command(LCD_ENTRYMODESET | _displaymode);
}

// This is for text that flows Right to Left
void LCDI2C::rightToLeft(void) {
	_displaymode &= ~LCD_ENTRYLEFT;
	command(LCD_ENTRYMODESET | _displaymode);
}

// This will 'right justify' text from the cursor
void LCDI2C::autoscroll(void) {
	_displaymode |= LCD_ENTRYSHIFTINCREMENT;
	command(LCD_ENTRYMODESET | _displaymode);
}

// This will 'left justify' text from the cursor
void LCDI2C::noAutoscroll(void) {
	_displaymode &= ~LCD_ENTRYSHIFTINCREMENT;
	command(LCD_ENTRYMODESET | _displaymode);
}

// Allows us to fill the first 8 CGRAM locations
// with custom characters
void LCDI2C::createChar(BYTE location, const Platform::Array<BYTE>^ charmap) {
	location &= 0x7; // we only have 8 locations 0-7
	command(LCD_SETCGRAMADDR | (location << 3));
	for (UINT32 i = 0; i<charmap->Length; i++) {
		write(charmap[i]);
	}
}

// Turn the (optional) backlight off/on
void LCDI2C::noBacklight(void) {
	_backlightval = LCD_NOBACKLIGHT;
	expanderWrite(0);
}

void LCDI2C::backlight(void) {
	_backlightval = LCD_BACKLIGHT;
	expanderWrite(0);
}



/*********** mid level commands, for sending data/cmds */

//send Command
inline void LCDI2C::command(BYTE value) {
	send(value, 0);
}

//Send text (byte) to display
void LCDI2C::LCDI2C::write(BYTE b)
{
	send(b, Rs);
}

/************ low level data pushing commands **********/

// write either command or data
void LCDI2C::send(BYTE value, BYTE mode) {
	BYTE highnib = value & 0xf0;
	BYTE lownib = (value << 4) & 0xf0;
	write4bits((highnib) | mode);
	write4bits((lownib) | mode);
}

void LCDI2C::write4bits(BYTE value) {
	expanderWrite(value);
	pulseEnable(value);
}

void LCDI2C::expanderWrite(BYTE _data) {
	_data = _data | _backlightval;
	_arr->set(0, _data);
	_device->Write(_arr); //1:1 with Arduino "style" - can be optimized!
}

void LCDI2C::pulseEnable(BYTE _data) {
	expanderWrite(_data | En);	// En high
	CPPHelper::ClockUtil::DelayMicrosecond(1);		// enable pulse must be >450ns

	expanderWrite(_data & ~En);	// En low
	CPPHelper::ClockUtil::DelayMicrosecond(50);		// commands need > 37us to settle
}

void LCDI2C::printstr(Platform::String^ str) {
	stdext::cvt::wstring_convert<std::codecvt_utf8<wchar_t>> convert;
	std::string stringUtf8 = convert.to_bytes(str->Data());
	for (char & c : stringUtf8)
	{
		write(c);
	}
}


