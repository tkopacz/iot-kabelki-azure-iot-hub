using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleSerialClient {
    enum eWaitFor {
        eOK = 1,
        eFAIL = 2,
        eNoChange = 4,
        eGT = 8
    }
    /// <summary>
    /// Działa dla najmniejszej oraz dla Serial Wifi v1
    /// 
    /// </summary>
    /// <remarks>
    /// Dziwne - ceramiczna antena mniejszy ma zasięg??????
    /// </remarks>
    class Program {
        static void Main(string[] args) {
            string result;
            int end = 0;
            SerialPort sp = new SerialPort("COM2");
            sp.BaudRate = 115200; // 56700;
            sp.NewLine = "\r\n";
            sp.Open();
            sendCommand(sp, "AT+RST", eWaitFor.eOK);
            Thread.Sleep(1000);
            sendCommand(sp, "AT+CWJAP=\"\",\"\"", eWaitFor.eOK | eWaitFor.eFAIL);
            Thread.Sleep(1000);
            sendCommand(sp, "AT+CWMODE=1", eWaitFor.eOK | eWaitFor.eNoChange);
            Thread.Sleep(1000);
            //sendCommand(sp, "AT+CWJAP=\"CBA_F1\",\"abc\"", eWaitFor.eOK);
            //sendCommand(sp, "AT+CWJAP=\"LumiaTk5276\",\"abc\"", eWaitFor.eOK);
            sendCommand(sp, "AT+CWJAP=\"edup-rt\",\"abc\"", eWaitFor.eOK);

            Thread.Sleep(1000);
            Console.WriteLine("IP: " + getIP(sp));
            sendCommand(sp, "AT+CIPMUX=1", eWaitFor.eOK);
            sendCommand(sp, "AT+CIPSTART=4,\"TCP\",\"google.com\",80", eWaitFor.eOK);
            sendCommand(sp, "AT+CIPSEND=4,18", eWaitFor.eGT);
            sp.Write("GET / HTTP/1.0\r\n\r\n");
            Thread.Sleep(5000); //Czekamy tu na internet
            result = sp.ReadExisting();
            Console.WriteLine(result);

            //sp.WriteLine("AT+CIFSR");
            //result = sp.ReadTo("OK\r");

            sp.Close();
        }

        private static eWaitFor sendCommand(SerialPort sp, string cmd, eWaitFor wf) {
            string tmp;
            int sendcnt = 0;
            while (true) {
                do {
                    tmp = sp.ReadExisting();
                    Console.WriteLine(tmp);
                } while (tmp != "");
                Console.WriteLine("SENDING : " + (++sendcnt).ToString());
                sp.WriteLine(cmd);
                Thread.Sleep(1000);
                string result;
                int end = 0, findMsg = 0, retrycnt = 10;
                while ( (findMsg == 0 || end == 0 ) && retrycnt-- > 0) {
                    Thread.Sleep(2000);
                    result = sp.ReadExisting();
                    if (result.Contains(cmd)) findMsg = 1;
                    Console.WriteLine(result);
                    if (((wf & eWaitFor.eFAIL) != 0) && findMsg != 0 && ( result.Contains("FAIL") || result.Contains("ERROR"))) return eWaitFor.eFAIL;
                    else if (((wf & eWaitFor.eOK) != 0) && findMsg != 0 && result.Contains("OK")) return eWaitFor.eOK;
                    else if (((wf & eWaitFor.eNoChange) != 0) && findMsg != 0 && result.Contains("no change")) return eWaitFor.eNoChange;
                    else if (((wf & eWaitFor.eGT) != 0) && findMsg != 0 && result.Contains(">")) return eWaitFor.eGT;
                }
                Thread.Sleep(5000);
            }
        }

        //ready
        //c_G?RS??FjS? fJ[??
        //[Vendor: www.ai-thinker.com Version:0.9.2.4]
        //OK
        //ready
        //FAIL
        private static string getIP(SerialPort sp) {
            string cmd = "AT+CIFSR";
            string result;
            string tmp;
            while (true) {
                do {
                    tmp = sp.ReadExisting();
                    Console.WriteLine(tmp);
                } while (tmp != "");
                sp.WriteLine(cmd);
                Thread.Sleep(1000);
                int end = 0, findMsg = 0; ;
                int resultReadCnt = 10;
                while ((findMsg == 0 || end == 0) && resultReadCnt-- > 0) {
                    result = sp.ReadExisting();
                    if (result.Contains(cmd)) findMsg = 1;
                    Console.WriteLine(result);
                    if (findMsg != 0 && ( result.Contains("192.168.16.") || result.Contains("192.168.137.") || result.Contains("192.168.10."))) return result; //Nieładnie, ale na razie - czekamy na IP
                    if (findMsg != 0 && result.Contains("0.0.0.0")) { Thread.Sleep(1000); break; } //Ponawiamy
                    Thread.Sleep(1000);
                }
                Thread.Sleep(5000);
            }

            return null;

        }
    }
}
