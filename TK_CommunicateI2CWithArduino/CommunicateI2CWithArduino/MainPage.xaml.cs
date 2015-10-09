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

namespace CommunicateI2CWithArduino
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string I2C_CONTROLLER_NAME = "I2C1"; //specific to RPI2
        private const byte I2C_ARDUINO = 18;

        public MainPage()
        {
            this.InitializeComponent();
            initI2C();
        }

        private async void initI2C()
        {
            try
            {
                var i2cSettings = new I2cConnectionSettings(I2C_ARDUINO);
                i2cSettings.BusSpeed = I2cBusSpeed.FastMode;
                string deviceSelector = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);
                var i2cDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);
                var i2cdev = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
                byte[] wbuffer = new byte[] { 1, 2 };
                byte[] rbuffer = new byte[8];
                //var resutl = i2cdev.WriteReadPartial(wbuffer, rbuffer);
                //Debug.WriteLine(resutl.Status);
                //i2cdev.WriteRead(wbuffer, rbuffer);
                i2cdev.Write(wbuffer);
                await Task.Delay(10000);
                var resutl = i2cdev.ReadPartial(rbuffer);
                Debug.WriteLine(rbuffer[0]);

            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
