using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
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

namespace TK_SPI_MCP3002_Potencjometrv2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Podłączenia MCP3002 
        /*
        VSS - do GND
        VDD / VREF - do +5V, próba: do +3.3V
        */

        /*RaspBerry Pi2  Parameters*/
        private const string SPI_CONTROLLER_NAME = "SPI0";  /* For Raspberry Pi 2, use SPI0                             */
        private const Int32 SPI_CHIP_SELECT_LINE = 0;       /* Line 0 maps to physical pin number 24 on the Rpi2        */

        byte[] m_readBuffer = new byte[2]; /*this is defined to hold the output data*/
        byte[] m_writeBufferCH0 = new byte[] { 0x68, 0x00 }; //0 1 10 1 000
        byte[] m_writeBufferCH1 = new byte[] { 0x70, 0x00 }; //0 1 11 0 000
                                                             //
        int resCH0, resCH1;
        SpiDevice m_spiDev;
        DispatcherTimer m_timer;
        public MainPage()
        {
            this.InitializeComponent();
            m_timer = new DispatcherTimer();
            m_timer.Interval = TimeSpan.FromSeconds(10); //100, 500
            m_timer.Tick += M_timer_Tick; // dispatcher_timer_Tick; ;
            m_sw = Stopwatch.StartNew();

            InitSPIAndTimer();
        }
        private async void InitSPIAndTimer() {
            try {
                //if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Devices.WiFi.WiFiAdapter"))
                //    Application.Current.Exit();//No bo co zrobić??
                //if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Devices.Spi.Devices"))
                //    Application.Current.Exit();//No bo co zrobić??
                if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.IoT")
                    Application.Current.Exit();//No bo co zrobić??

                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 8000000;// 4000000, 3200000;3000000;500000
                settings.Mode = SpiMode.Mode0;

                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                m_spiDev = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);
                m_timer.Start();
                ReadSPIAsync();
            } catch (Exception) {
                m_spiDev = null;
            }
        }

        private async Task ReadSPIAsync() {
            await Task.Run(() => {
                ReadSPI();
            }).AsAsyncAction();
        }
        int m_count=0;
        private Stopwatch m_sw;

        //RPI2, 10s:3927, 3950, 3977, 3993, 4004, 4028, 4031 , dla domyślnej prędkości 4000000
        //RPI2, 10s:2517,2547 dla 1000000
        //RPI2, 10s: 3638,3707,3699,3722,3752 500000
        //RPI2, 10s: 2572,2625 dla 3200000
        //RPI2, 10s: 4283, 3950, 3982, 4058, 4043, 4039, 4054, 4074, 4100, 4116, 4115 dla 6000000
        //RPI2, 10s: 4421, 4423, 4419, 4423, 4422, 4426, 4427, 4429, 4432, 4432, 4434, 4432, 4435 dla 8000000
        //Wystarczająca do odczytu heart rate
        private void ReadSPI() {
            while (true) {
                m_spiDev.TransferFullDuplex(m_writeBufferCH0, m_readBuffer);
                resCH0 = convertToInt(m_readBuffer);
                m_spiDev.TransferFullDuplex(m_writeBufferCH1, m_readBuffer);
                resCH1 = convertToInt(m_readBuffer);
                Debug.WriteLine($"CH0: {resCH0:D4} CH1: {resCH1:D4}");
                //Debug.WriteLine($"CH1: {m_readBuffer[0]:X2}|{m_readBuffer[1]:X2}:{resCH0:D4}");
                m_count++;
            }
        }

        private void M_timer_Tick(object sender, object e) {
            double ms = m_count / (m_sw.ElapsedMilliseconds / 1000);
            //Debug.WriteLine($"{ms}");
        }

        private void dispatcher_timer_Tick(object sender, object e) {
            StringBuilder sb = new StringBuilder();
            m_spiDev.TransferFullDuplex(m_writeBufferCH0, m_readBuffer);
            resCH0 = convertToInt(m_readBuffer);
            sb.Append($"CH0: {m_readBuffer[0]:X2}|{m_readBuffer[1]:X2}:{resCH0:D4}");
            m_spiDev.TransferFullDuplex(m_writeBufferCH1, m_readBuffer);
            sb.Append($"  CH1: {m_readBuffer[0]:X2}|{m_readBuffer[1]:X2}:{resCH1:D4}");

            resCH1 = convertToInt(m_readBuffer);
            //uxBytes.Text = sb.ToString();
            Debug.WriteLine(sb.ToString());
            //uxChannel0.Value = resCH0; //Można - bo własciwy wątek (UI)
            //uxChannel1.Value = resCH1;
        }
        public int convertToInt(byte[] data) {
            //10 bitowe wejście, czyli 2 + 8 bitów
            int result = data[0] & 0x03;
            result <<= 8;
            result += data[1];
            return result; //0 - 1023

        }

    }
}
