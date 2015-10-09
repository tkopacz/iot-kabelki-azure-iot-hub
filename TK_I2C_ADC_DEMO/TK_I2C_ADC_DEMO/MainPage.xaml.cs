using Amqp;
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

namespace RPI2_I2C
{
    ///Endpoint=sb://mvafy15.servicebus.windows.net/;SharedAccessKeyName=sender;SharedAccessKey=+QVhVeai+B1SbI44/TtN4dPelog8QeE5EQ041wXp+60=
    public class SBHelper{
        // update the following with valid Service Bus namespace and SAS key info
        public string Namespace = "mvafy15.servicebus.windows.net";
        public string KeyName = "sender";
        public string KeyValue = "+QVhVeai+B1SbI44/TtN4dPelog8QeE5EQ041wXp+60=";
        public string Entity = "mvafy15eh"; //Event Hub Name

        public Address GetAddress() {
            return new Address(this.Namespace, 5671, this.KeyName, this.KeyValue);
        }

        public Connection GetConnection() {
            return new Connection(GetAddress());
        }
        public Session GetSession() {
            return new Session(GetConnection());
        }

        public SenderLink GetSenderLink(string name) {
            return new SenderLink(GetSession(), name, Entity);
        }
    }

    public struct DataItem {
        Int64 TimeStamp;
        byte[] Data;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DataItem[] m_buf1 = new DataItem[2000];
        int m_buf1Pos = 0;
        DataItem[] m_buf2 = new DataItem[2000];
        int m_buf2Pos = 0;
        /// <summary>
        /// Which buffer
        /// </summary>
        int m_bufInd = 1;
        bool m_sending = false;

        private const byte I2C_ADDR_PCF8591 = 0x48; //Bo 127 bitów, 0x90 >> 1 = 0x48
        private const byte I2C_ADDR_MCP23008 = 0x20; // 7-bit I2C address of the port expander
        private const byte I2C_ADDR_ARDUINO = 17; // Adres Arduino

        I2cDevice i2cPCF8591 = null;


        DispatcherTimer m_timer;


        /*
        PCF8591:
        A0, A1, A2, Vss do GND
        Vdd, Vref do + 5V
        AGND - do GND
        EXT - zostawic puste!!!
        SCL, SDA - I2C

        Adres I2C: 0x90 >> 1
        Lepiej: użyć gotowej płytki (mniejszcze zakłócenia + kondensatory)
        */

        public MainPage()
        {
            this.InitializeComponent();
            Init();
        }
        private async void Init() {
            // Inicjalizacja I2C - urządzenie z RPI2
            string deviceSelector = I2cDevice.GetDeviceSelector();
            var i2cDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);
            if (i2cDeviceControllers.Count == 0) {
                return;
            }

            //Ustawienia dla PCF8591
            var i2cSettings = new I2cConnectionSettings(I2C_ADDR_PCF8591);
            i2cSettings.BusSpeed = I2cBusSpeed.StandardMode;
            i2cPCF8591 = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
            if (i2cPCF8591 == null) {
                return;
            }


            m_timer = new DispatcherTimer();
            m_timer.Interval = TimeSpan.FromSeconds(10);
            m_timer.Tick += M_timer_Tick;
            m_timer.Start();
            m_sw = Stopwatch.StartNew();

            Trace.TraceLevel = TraceLevel.Information;
            Trace.TraceListener = (f, a) => Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a));



            Task.Run(() => ReadI2C());
            //await ;
            //await WritePCF8591();
        }


        private void M_timer_Tick(object sender, object e) {
            double ms = m_count / ( m_sw.ElapsedMilliseconds / 1000) ;
            Debug.WriteLine($"{ms}");
        }

        int m_count = 0;
        private Stopwatch m_sw;

        /// <summary>
        /// Szybkość próbkowania
        /// RPI2: 1178, 1114, 1118, 1121 (co 10 sec)
        /// MBM:  818
        /// MBM, odczyt 4 661, 660, 659, 658
        /// RPI2: 878, 875, 874, 870 (co 1 min)
        ///       1054, 1044, 1038
        /// MBM:  804, 802
        /// RPI2: 1056, 1057, 1053, 1040 (co 30 sec);
        /// </summary>
        /// <returns></returns>

        private async Task ReadPCF8591() {
            byte[] readBuf = new byte[1];
            await Task.Run(() => {
                ReadI2C();
            }).AsAsyncAction();
        }

        private void ReadI2C() {
            byte[] cmd = new byte[1] { 0x04 };
            byte[] buf5 = new byte[5];

            SBHelper help = new SBHelper();
            var sl = help.GetSenderLink("ReadI2C");

            int m_bufSendThreshold = (m_buf1.Length * 3 / 4);
            //TODO: jeden bufor gromadzi, drugi jest wysyłany
            
            while (true) {
                i2cPCF8591.Write(cmd);
                i2cPCF8591.Read(buf5);
                //if (m_bufInd==1) {
                //    if (m_buf1Pos> m_bufSendThreshold && !m_sending) {
                //        //Send

                //    }
                //} else if (m_bufInd==2) {

                //}
                m_count++;
                Message msg = new Message(buf5);
                sl.Send(msg);
                //{buf5[0]:D3} - to powtórka
                Debug.WriteLine($"{buf5[1]:D3} - {buf5[2]:D3} - {buf5[3]:D3} - {buf5[4]:D3}");
            }
        }

        private async Task WritePCF8591() {
            byte[] readBuf = new byte[1];
            await Task.Run(() => {
                while (true) {
                    for (int i = 0; i <= 255; i++) {
                        i2cPCF8591.Write(new byte[] { 0x40, (byte)i });//Włączamy DAC
                    }
                    for (int i = 255; i >= 0; i--) {
                        i2cPCF8591.Write(new byte[] { 0x40, (byte)i });//Włączamy DAC
                    }
                }
            }).AsAsyncAction();
        }
    }
}
