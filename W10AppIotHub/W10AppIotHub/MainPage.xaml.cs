using Microsoft.ApplicationInsights;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
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

namespace W10AppIotHub
{


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //DeviceClient deviceClient = DeviceClient.Create("pltkw3IoT.azure-devices.net", new DeviceAuthenticationWithRegistrySymmetricKey(deviceName, deviceKey));

        static DeviceClient m_deviceClient;

        DispatcherTimer m_timer = new DispatcherTimer();
        static TelemetryClient m_tc = new TelemetryClient();
        IoTTelemetrySource m_telemetrySource;

        private const int LED_PIN = 23;
        private GpioPin pinLED;

        TK_LCDlcm1602DriverWRC.LCDI2C m_lcd = null;

        public MainPage()
        {
            this.InitializeComponent();
            try
            {
                if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.IoT")
                {
                    m_deviceClient = DeviceClient.CreateFromConnectionString("HostName=pltkw3IoT.azure-devices.net;DeviceId=pltkw87Demo;SharedAccessKey=ABC", TransportType.Http1);
                    m_telemetrySource = new IoTTelemetrySourceGen();
                }
                else
                {
                    m_deviceClient = DeviceClient.CreateFromConnectionString("HostName=pltkw3IoT.azure-devices.net;DeviceId=rpi2C;SharedAccessKey=ABC", TransportType.Http1);
                    m_telemetrySource = new IoTTelemetrySourceDevice();
                    //Inaczej null
                    var gpio = GpioController.GetDefault();
                    pinLED = gpio.OpenPin(LED_PIN);
                    pinLED.SetDriveMode(GpioPinDriveMode.Output);
                    m_lcd = new TK_LCDlcm1602DriverWRC.LCDI2C(0x27, 2, 16);
                    m_lcd.InitAsync();
                }
                //LED
                uxToggleLaser.IsOn = true;
                m_telemetrySource.Init();
                m_timer.Tick += M_timer_Tick;
                startTimer();
                ReceiveDataFromAzure(); //Po prostu "task"
            }
            catch (Exception ex)
            {
                m_tc.TrackException(ex);
            }

        }

        bool processing = false;
        private async void M_timer_Tick(object sender, object e)
        {
            if (processing) return;
            processing = true;
            try
            {
                var data = await m_telemetrySource.GetData();
                var messageString = JsonConvert.SerializeObject(data);
                var message = new Message(Encoding.UTF8.GetBytes(messageString));
                await m_deviceClient.SendEventAsync(message);
                uxValues.Text = data.ToString();
                Debug.WriteLine(data.ToString());
                m_lcd?.home();
                m_lcd?.printstr($"{data.Dt:mm:ss} {data.ARDUINO_LIGHT:D3} {data.PCF8591_CH0:D3}");
            }
            catch (Exception ex)
            {
                m_lcd?.home();
                m_lcd?.printstr("ERROR");
            }
            finally
            {
                processing = false;
            }
        }

        //Wysyła wiadomość z odpowiednimi atrybutami
        private async void uxSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = new IoTMessage() { Message = uxCommand.Text };
                data.DeviceName = m_telemetrySource.GetDeviceName();
                var messageString = JsonConvert.SerializeObject(data);
                var message = new Message(Encoding.UTF8.GetBytes(messageString));

                await m_deviceClient.SendEventAsync(message);
                uxValues.Text = data.ToString();
                Debug.WriteLine(data.ToString());
                m_lcd?.home();
                m_lcd?.printstr(data.Message);
            }
            catch (Exception ex)
            {
                m_lcd?.home();
                m_lcd?.printstr("ERROR");
            }

            m_tc.TrackEvent($"uxSend_Click - {uxCommand.Text}");

        }

        private void uxTelemetryDelaySec_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_tc.TrackEvent("uxTelemetryDelaySec_TextChanged");
            startTimer();
        }

        private void startTimer()
        {
            int delay;
            if (int.TryParse(uxTelemetryDelaySec.Text, out delay))
            {
                if (delay == 0)
                {
                    m_timer.Stop();
                }
                else
                {
                    m_timer.Interval = TimeSpan.FromSeconds(delay);
                    m_timer.Start();
                }
            }
            else
            {
                m_timer.Stop();
            }
        }

        private void uxToggleLaser_Toggled(object sender, RoutedEventArgs e)
        {
            if (pinLED != null)
            {
                if (uxToggleLaser.IsOn)
                    pinLED.Write(GpioPinValue.High);
                else
                    pinLED.Write(GpioPinValue.Low);
            }
            m_tc.TrackEvent("uxToggleLaser_Toggled");
        }

        public async Task ReceiveDataFromAzure()
        {

            Message receivedMessage;
            string messageData;

            while (true)
            {
                receivedMessage = await m_deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    m_tc.TrackEvent("ReceiveDataFromAzure - message");
                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    //Wykonanie polecenia
                    if (messageData.Length >= 2)
                    {
                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            int val = 0;
                            if (int.TryParse(messageData.Substring(1), out val))
                            {
                                switch (messageData[0])
                                {
                                    case 'L':
                                        if (val == 0)
                                            uxToggleLaser.IsOn = false;
                                        else
                                            uxToggleLaser.IsOn = true;
                                        break;
                                    case 'T':
                                        uxTelemetryDelaySec.Text = val.ToString();
                                        break;
                                    default:
                                        break;
                                }
                            }
                        });
                    }
                    //await m_deviceClient.RejectAsync(receivedMessage);
                    //await m_deviceClient.AbandonAsync(receivedMessage); - odrzuca, ale komunikat wraca
                    //Potwierdzenie wykonania
                    await m_deviceClient.CompleteAsync(receivedMessage); //potwierdza odebranie
                }
            }
        }
    }
}
