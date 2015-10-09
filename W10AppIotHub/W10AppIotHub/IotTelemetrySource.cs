using Microsoft.ApplicationInsights;
using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.Spi;

namespace W10AppIotHub
{
    public class IoTBase
    {
        public DateTime Dt { get; } = DateTime.Now;
        public string MsgType { get; protected set; }
        public string DeviceName { get; set; }
    }

    public class IoTTelemetry : IoTBase
    {
        public IoTTelemetry() : base() { MsgType = "T"; }
        public bool PIR { get; set; }
        public int MCP3002_CH0 { get; set; }
        public int MCP3002_CH1 { get; set; }
        /// <summary>
        /// Światło
        /// </summary>
        public int PCF8591_CH0 { get; set; }
        public int PCF8591_CH1 { get; set; }
        public int PCF8591_CH2 { get; set; }
        /// <summary>
        /// Potencjometr
        /// </summary>
        public int PCF8591_CH3 { get; set; }
        public double GY_652_H { get; set; }
        public int ARDUINO_LIGHT { get; set; }
        public int ARDUINO_DIST { get; set; }

        public override string ToString()
        {
            return $"DT:{Dt:hh:mm.ss.fff}, PIR:{PIR}, MCP3002_CH0:{MCP3002_CH0:0000},MCP3002_CH0:{MCP3002_CH1:0000},PCF8591_CH0:{PCF8591_CH0:0000},PCF8591_CH0:{PCF8591_CH3:0000},GY_652_H:{GY_652_H:000.00},ARDUINO_LIGHT:{ARDUINO_LIGHT:0000},ARDUINO_DIST:{ARDUINO_DIST:00000}";
        }
    }

    public class IoTMessage : IoTBase
    {
        public IoTMessage() : base() { MsgType = "M"; }
        public string Message { get; set; }
        public override string ToString()
        {
            return $"DT:{Dt:hh:mm.ss.fff}: {Message}";
        }
    }

    internal interface IoTTelemetrySource
    {
        Task<bool> Init();
        string GetDeviceName();
        Task<IoTTelemetry> GetData();
    }

    public class IoTTelemetrySourceGen : IoTTelemetrySource
    {
        static Random m_rnd = new Random();
        public Task<IoTTelemetry> GetData()
        {
            IoTTelemetry data = new IoTTelemetry();
            data.ARDUINO_DIST = m_rnd.Next(1024);
            data.ARDUINO_LIGHT = m_rnd.Next(1024);
            data.GY_652_H = m_rnd.NextDouble() * 360;
            data.MCP3002_CH0 = m_rnd.Next(1024);
            data.MCP3002_CH1 = m_rnd.Next(1024);
            data.PCF8591_CH0 = m_rnd.Next(1024);
            data.PCF8591_CH1 = m_rnd.Next(1024);
            data.PCF8591_CH2 = m_rnd.Next(1024);
            data.PCF8591_CH3 = m_rnd.Next(1024);
            data.DeviceName = GetDeviceName() ;
            data.PIR = (m_rnd.Next(2) == 1) ? true : false;
            return Task.FromResult<IoTTelemetry>(data);
        }

        public string GetDeviceName()
        {
            return "pltkw87";
        }

        public Task<bool> Init()
        {
            return Task.FromResult<bool>(true);
        }
    }

    /// <summary>
    /// Odczytuje SPI, I2C, coś z Arduino
    /// </summary>
    public class IoTTelemetrySourceDevice : IoTTelemetrySource
    {
        bool m_init = false;
        #region PrywatneMechanizmy

        //Arduino
        private const string I2C_CONTROLLER_NAME = "I2C1"; //specific to RPI2
        private const byte I2C_ARDUINO = 18;

        I2cDevice m_devArduino;

        private const string SPI_CONTROLLER_NAME = "SPI0";  /* For Raspberry Pi 2, use SPI0                             */
        private const Int32 SPI_CHIP_SELECT_LINE = 0;       /* Line 0 maps to physical pin number 24 on the Rpi2        */

        byte[] m_readBuffer = new byte[2]; /*this is defined to hold the output data*/
        byte[] m_writeBufferCH0 = new byte[] { 0x68, 0x00 }; //0 1 10 1 000
        byte[] m_writeBufferCH1 = new byte[] { 0x70, 0x00 }; //0 1 11 0 000
                                                             //
        SpiDevice m_spiDev;

        //PIR
        private const int PIR_PIN = 13;
        private GpioPin pinPIR;

        //Gyro
        I2cDevice i2c_gy652_pressureBMP085; //Baromert = wysokość, 0x77
        I2cDevice i2c_gy652_compassHMC5983; //Compass = wysokość, 0x1E

        //PCF8591
        private const byte I2C_ADDR_PCF8591 = 0x48; //Bo 127 bitów, 0x90 >> 1 = 0x48
        I2cDevice i2cPCF8591;
        #endregion
        public string GetDeviceName()
        {
            return "rpi2c";
        }

        public async Task<bool> Init()
        {
            TelemetryClient tc = new TelemetryClient();
            try
            {
                //PIR
                var gpio = GpioController.GetDefault();
                pinPIR = gpio.OpenPin(PIR_PIN);
                pinPIR.SetDriveMode(GpioPinDriveMode.Input);


                //SPI
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 8000000;// 4000000, 3200000;3000000;500000
                settings.Mode = SpiMode.Mode0;

                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                m_spiDev = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);

                //I2C
                var i2cSettings = new I2cConnectionSettings(I2C_ARDUINO);
                i2cSettings.BusSpeed = I2cBusSpeed.FastMode;
                string deviceSelector = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);
                var i2cDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);
                m_devArduino = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);

                //Gyro
                i2cSettings = new I2cConnectionSettings(0x77);
                i2c_gy652_pressureBMP085 = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
                i2cSettings = new I2cConnectionSettings(0x1E);
                i2c_gy652_compassHMC5983 = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);

                ac1 = readInt(i2c_gy652_pressureBMP085, 0xAA);
                ac2 = readInt(i2c_gy652_pressureBMP085, 0xAC);
                ac3 = readInt(i2c_gy652_pressureBMP085, 0xAE);
                ac4 = (uint)readInt(i2c_gy652_pressureBMP085, 0xB0);
                ac5 = (uint)readInt(i2c_gy652_pressureBMP085, 0xB2);
                ac6 = (uint)readInt(i2c_gy652_pressureBMP085, 0xB4);
                b1 = readInt(i2c_gy652_pressureBMP085, 0xB6);
                b2 = readInt(i2c_gy652_pressureBMP085, 0xB8);
                mb = readInt(i2c_gy652_pressureBMP085, 0xBA);
                mc = readInt(i2c_gy652_pressureBMP085, 0xBC);
                md = readInt(i2c_gy652_pressureBMP085, 0xBE);

                //I2C PCF
                i2cSettings = new I2cConnectionSettings(I2C_ADDR_PCF8591);
                i2cPCF8591 = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);


            }
            catch (Exception ex)
            {
                tc.TrackException(ex);
            }
            m_init = true;
            return true;
        }

        public async Task<IoTTelemetry> GetData()
        {
            IoTTelemetry data = new IoTTelemetry();
            data.DeviceName = GetDeviceName();
            if (!m_init) return data;
            byte[] wbuffer = new byte[] { 1, 2 };
            byte[] rbuffer = new byte[8];
            m_devArduino.Write(wbuffer);
            m_devArduino.Read(rbuffer);
            
            //Nie możemy tu pominąć "nadmiarowych" - wada "protokołu"
            data.ARDUINO_DIST = rbuffer[4] + (rbuffer[5] << 8);
            data.ARDUINO_LIGHT = rbuffer[6] + (rbuffer[7] << 8);
            
            //SPI
            m_spiDev.TransferFullDuplex(m_writeBufferCH0, m_readBuffer);
            data.MCP3002_CH0 = convertToInt(m_readBuffer);
            m_spiDev.TransferFullDuplex(m_writeBufferCH1, m_readBuffer);
            data.MCP3002_CH1 = convertToInt(m_readBuffer);
            
            //PIR
            data.PIR = (pinPIR.Read() == GpioPinValue.High);
            
            //Gyro
            data.GY_652_H = await readCompassHeading();
            
            //i2cPCF8591
            byte[] cmd = new byte[1] { 0x04 };
            byte[] buf5 = new byte[5];
            i2cPCF8591.Write(cmd);
            i2cPCF8591.Read(buf5);
            data.PCF8591_CH0 = buf5[1];
            data.PCF8591_CH1 = buf5[2];
            data.PCF8591_CH2 = buf5[3];
            data.PCF8591_CH3 = buf5[4];
            return data;
        }

        #region Nieistotne, długie rzeczy
        long PressureCompensate;
        byte OSS = 0;
        int ac1;
        int ac2;
        int ac3;
        uint ac4;
        uint ac5;
        uint ac6;
        int b1;
        int b2;
        int mb;
        int mc;
        int md;
        public int convertToInt(byte[] data)
        {
            //10 bitowe wejście, czyli 2 + 8 bitów
            int result = data[0] & 0x03;
            result <<= 8;
            result += data[1];
            return result; //0 - 1023

        }
        private byte readByte(I2cDevice dev, byte addr)
        {
            byte[] w = new byte[1] { addr }, r = new byte[1];
            dev.WriteRead(w, r);
            return r[0];
        }
        private int readInt(I2cDevice dev, byte addr)
        {
            byte[] w = new byte[2] { addr, 0 }, r = new byte[2];
            dev.WriteRead(w, r);
            return ((int)r[0]) << 8 + (int)r[0];
        }

        async Task<int> bmp085ReadUT()
        {
            int ut;
            i2c_gy652_pressureBMP085.Write(new byte[] { 0xF4, 0x2E });
            await Task.Delay(5);
            ut = readInt(i2c_gy652_pressureBMP085, 0xF6);
            return ut;
        }

        async Task<ulong> bmp085ReadUP()
        {
            byte msb, lsb, xlsb;
            ulong up = 0;
            i2c_gy652_pressureBMP085.Write(new byte[] { 0xF4, (byte)(0x34 + (OSS << 6)) });
            await Task.Delay(2 + (3 << OSS));

            // Read register 0xF6 (MSB), 0xF7 (LSB), and 0xF8 (XLSB)
            msb = readByte(i2c_gy652_pressureBMP085, 0xF6);
            lsb = readByte(i2c_gy652_pressureBMP085, 0xF7);
            xlsb = readByte(i2c_gy652_pressureBMP085, 0xF8);
            up = (((ulong)msb << 16) | ((ulong)lsb << 8) | (ulong)xlsb) >> (8 - OSS);
            return up;
        }

        double calcAltitude(double pressure)
        {
            double A = pressure / 101325;
            double B = 1 / 5.25588;
            double C = Math.Pow(A, B);
            C = 1 - C;
            C = C / 0.0000225577;
            return C;
        }

        async Task<double> bmp085GetTemperature()
        {
            int ut = await bmp085ReadUT();
            long x1, x2;

            x1 = (((long)ut - (long)ac6) * (long)ac5) >> 15;
            x2 = ((long)mc << 11) / (x1 + md);
            PressureCompensate = x1 + x2;

            float temp = ((PressureCompensate + 8) >> 4);
            temp = temp / 10;

            return temp;
        }

        async Task<long> bmp085GetPressure()
        {
            ulong up = await bmp085ReadUP();
            long x1, x2, x3, b3, b6, p;
            ulong b4, b7;
            b6 = PressureCompensate - 4000;
            x1 = (b2 * (b6 * b6) >> 12) >> 11;
            x2 = (ac2 * b6) >> 11;
            x3 = x1 + x2;
            b3 = (((((long)ac1) * 4 + x3) << OSS) + 2) >> 2;

            // Calculate B4
            x1 = (ac3 * b6) >> 13;
            x2 = (b1 * ((b6 * b6) >> 12)) >> 16;
            x3 = ((x1 + x2) + 2) >> 2;
            b4 = (ac4 * (ulong)(x3 + 32768)) >> 15;

            b7 = (up - (ulong)b3) * (ulong)(50000 >> OSS);
            if (b7 < 0x80000000)
                p = (long)((b7 << 1) / b4);
            else
                p = (long)((b7 / b4) << 1);

            x1 = (p >> 8) * (p >> 8);
            x1 = (x1 * 3038) >> 16;
            x2 = (-7357 * p) >> 16;
            p += (x1 + x2 + 3791) >> 4;

            long temp = p;
            return temp;
        }

        async Task<double> readCompassHeading()
        {
            byte[] response = new byte[6];
            double x, y, z;
            i2c_gy652_compassHMC5983.Write(new byte[] { 0x02, 0x01 });
            await Task.Delay(6);
            i2c_gy652_compassHMC5983.Read(response);
            //MSB, LSB !
            x = (int)response[1] + (int)response[0] << 8;
            z = (int)response[3] + (int)response[2] << 8;
            y = (int)response[5] + (int)response[4] << 8;
            // convert the numbers to fit the 
            if (x > 0x07FF) x = 0xFFFF - x;
            if (z > 0x07FF) z = 0xFFFF - z;
            if (y > 0x07FF) y = 0xFFFF - y;

            // declare the heading variable we'll be returning
            double H = 0;

            if (y == 0 && x > 0) H = 180.0;
            if (y == 0 && x <= 0) H = 0.0;
            if (y > 0) H = 90 - Math.Atan(x / y) * 180 / Math.PI;
            if (y < 0) H = 270 - Math.Atan(y / y) * 180 / Math.PI;
            return H;
        }
    }
    #endregion
}