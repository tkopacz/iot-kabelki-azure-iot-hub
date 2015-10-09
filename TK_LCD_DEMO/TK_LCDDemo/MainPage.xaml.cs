using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TK_LCDDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

        }

        byte[] bell = new byte[] { 0x4, 0xe, 0xe, 0xe, 0x1f, 0x0, 0x4,0x0 };
        byte[] note = new byte[] { 0x2, 0x3, 0x2, 0xe, 0x1e, 0xc, 0x0, 0x0 };
        byte[] clock = new byte[] { 0x0, 0xe, 0x15, 0x17, 0x11, 0xe, 0x0, 0x0 };
        byte[] heart = new byte[] { 0x0, 0xa, 0x1f, 0x1f, 0xe, 0x4, 0x0, 0x0 };
        byte[] duck = new byte[] { 0x0, 0xc, 0x1d, 0xf, 0xf, 0x6, 0x0, 0x0 };
        byte[] check = new byte[] { 0x0, 0x1, 0x3, 0x16, 0x1c, 0x8, 0x0, 0x0 };
        byte[] cross = new byte[] { 0x0, 0x1b, 0xe, 0x4, 0xe, 0x1b, 0x0, 0x0 };
        byte[] retarrow = new byte[] { 0x1, 0x1, 0x5, 0x9, 0x1f, 0x8, 0x4, 0x0 };


        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            runDemo();

        }

        private async Task runDemo()
        {
            TK_LCDlcm1602DriverWRC.LCDI2C l = new TK_LCDlcm1602DriverWRC.LCDI2C(0x27, 2, 16);
            await l.InitAsync();
            while (true)
            {
                l.noBacklight();
                await Task.Delay(1000);
                l.setCursor(0, 0);
                await Task.Delay(1000);
                l.backlight();
                await Task.Delay(1000);
                l.printstr("OK!");
                await Task.Delay(1000);
                l.blink();
                await Task.Delay(1000);
                l.noBlink();
                await Task.Delay(1000);
                l.scrollDisplayLeft();
                await Task.Delay(1000);
                l.scrollDisplayRight();
                await Task.Delay(1000);
                l.cursor();
                await Task.Delay(1000);
                l.setCursor(2, 1);
                await Task.Delay(1000);
                l.printstr("Windows IoT");
                l.noCursor();
                await Task.Delay(1000);
                l.createChar(0, bell);
                l.createChar(1, note);
                l.createChar(2, clock);
                l.createChar(3, heart);
                l.createChar(4, duck);
                l.createChar(5, check);
                l.createChar(6, cross);
                l.createChar(7, retarrow);
                await Task.Delay(1000);
                l.clear();
                l.home();
                for (byte i = 0; i <= 7; i++)
                {
                    l.write(i);
                }
                await Task.Delay(1000);
                l.clear();
            }
        }

        private static async Task testIfWorking() {
            string deviceSelector = I2cDevice.GetDeviceSelector();
            var i2cDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);
            if (i2cDeviceControllers.Count == 0) {
                return;
            }

            //Ustawienia dla PCF8591
            var i2cSettings = new I2cConnectionSettings(0x27);
            var i2c = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
            if (i2c == null) {
                return;
            }

            //Init and write "A"
            i2c.Write(new byte[] { 0x30 });
            i2c.Write(new byte[] { 0x34 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x30 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x30 });
            i2c.Write(new byte[] { 0x34 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x30 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x30 });
            i2c.Write(new byte[] { 0x34 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x30 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x20 });
            i2c.Write(new byte[] { 0x24 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x20 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x20 });
            i2c.Write(new byte[] { 0x24 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x20 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x80 });
            i2c.Write(new byte[] { 0x84 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x80 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x0 });
            i2c.Write(new byte[] { 0x4 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x0 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0xC0 });
            i2c.Write(new byte[] { 0xC4 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0xC0 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x0 });
            i2c.Write(new byte[] { 0x4 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x0 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x10 });
            i2c.Write(new byte[] { 0x14 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x10 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x0 });
            i2c.Write(new byte[] { 0x4 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x0 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x60 });
            i2c.Write(new byte[] { 0x64 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x60 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x0 });
            i2c.Write(new byte[] { 0x4 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x0 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x20 });
            i2c.Write(new byte[] { 0x24 });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x20 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x8 });
            i2c.Write(new byte[] { 0x88 });
            i2c.Write(new byte[] { 0x8C });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x88 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x8 });
            i2c.Write(new byte[] { 0xC });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x8 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x49 });
            i2c.Write(new byte[] { 0x4D });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x49 });
            await Task.Delay(2);
            i2c.Write(new byte[] { 0x19 });
            i2c.Write(new byte[] { 0x1D });
            await Task.Delay(1);
            i2c.Write(new byte[] { 0x19 });
            await Task.Delay(2);
        }
    }
}
