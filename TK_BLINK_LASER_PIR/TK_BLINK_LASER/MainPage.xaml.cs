using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Gpio;
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

namespace TK_BLINK_LASER
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int LED_PIN = 23;
        private const int PIR_PIN= 13;
        private GpioPin pinLED, pinPIR;
        public MainPage()
        {
            this.InitializeComponent();
            initGPIO();
            burglar.Visibility = Visibility.Collapsed;
            setState(true);
        }

        private bool initGPIO() {
            //Czy jest GPIO?
            if (!Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Devices.DevicesLowLevelContract",1))
                return false;
            if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioController"))
                return false;
            if (!Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.Devices.Gpio.GpioController", "GetDefault"))
                return false;

            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.IoT")
                return false;
            //Windows.System.Profile.AnalyticsInfo.DeviceForm
            var gpio = GpioController.GetDefault();
            if (gpio == null) {
                pinLED = null;
                return false;
            }
            pinLED = gpio.OpenPin(LED_PIN);
            if (pinLED == null) return false;
            pinLED.SetDriveMode(GpioPinDriveMode.Output);
            pinPIR = gpio.OpenPin(PIR_PIN);
            if (pinPIR == null) return false;
            pinPIR.SetDriveMode(GpioPinDriveMode.Input);
            pinPIR.ValueChanged += PinPIR_ValueChanged;

            setState(false); //Zawsze zmienia stan - zapisuje do pin
            return true;
        }

        private async void PinPIR_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (args.Edge == GpioPinEdge.RisingEdge)
                {
                    burglar.Visibility = Visibility.Visible;
                }
                else
                {
                    burglar.Visibility = Visibility.Collapsed;
                }
            });
        }

        /// <summary>
        /// Miganie diodą
        /// </summary>
        private void setState(bool value) {
            if (pinLED != null) {
                if (value)
                    pinLED.Write(GpioPinValue.High);
                else
                    pinLED.Write(GpioPinValue.Low);
            }
        }


        private void uxToggleLaser_Toggled(object sender, RoutedEventArgs e)
        {
            setState(uxToggleLaser.IsOn);
        }
    }
}
