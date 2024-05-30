using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using USBClassLibrary;
using System.Management;

namespace denali_cmd_to_unit {
    class Program {
        private static SerialPort mySerialPort = new SerialPort();
        static private List<USBClassLibrary.USBClass.DeviceProperties> ListOfUSBDeviceProperties;
        private static bool debug = false;
        private static bool flag_discom = false;
        private static System.Threading.Timer close_program;
        private static string head = "1";
        private static string tx = "ledp";
        private static string step = "non";
        static void Main(string[] args) {
            while (true) {
                try { head = File.ReadAllText("../../config/head.txt"); break; } catch { Thread.Sleep(50); }
            }
            File.Delete("../../config/head.txt");
            File.WriteAllText("call_exe_tric.txt", "");

            //File.WriteAllText("../../config/denali_cmd_to_unit_" + head + "_step.txt", "OK");
            //File.WriteAllText("../../config/denali_cmd_to_unit_" + head + "_data_tx.txt", "AT+MODE=2");
            //File.WriteAllText("../../config/denali_cmd_to_unit_" + head + "_data_rx.txt", "20");
            //File.WriteAllText("../../config/denali_cmd_to_unit_" + head + "_data_rx_min.txt", "27.65");
            //File.WriteAllText("../../config/denali_cmd_to_unit_" + head + "_data_rx_max.txt", "30.5");

            int timeout = 10000;
            int send_every = 1000;
            int retest = 5;
            string rx = "PASS\n";
            string rxx_min = "5";
            string rxx_max = "90";
            string port_name = "COM6";
            try { step = File.ReadAllText("../../config/denali_cmd_to_unit_" + head + "_step.txt"); } catch { }
            try { timeout = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_timeout.txt")); } catch { }
            try { debug = Convert.ToBoolean(File.ReadAllText("../../config/test_head_" + head + "_debug.txt")); } catch { }
            try { tx = File.ReadAllText("../../config/denali_cmd_to_unit_" + head + "_data_tx.txt"); } catch { }
            try { port_name = File.ReadAllText("../../config/denali_cmd_to_unit_" + head + "_comport.txt"); } catch { }
            try { retest = Convert.ToInt32(File.ReadAllText("../../config/denali_cmd_to_unit_" + head + "_retest.txt")); } catch { }
            try { send_every = Convert.ToInt32(File.ReadAllText("../../config/denali_cmd_to_unit_" + head + "_send_every.txt")); } catch { }
            try { rx = File.ReadAllText("../../config/denali_cmd_to_unit_" + head + "_data_rx.txt"); } catch { }
            try { rxx_min = File.ReadAllText("../../config/denali_cmd_to_unit_" + head + "_data_rx_min.txt"); } catch { }
            try { rxx_max = File.ReadAllText("../../config/denali_cmd_to_unit_" + head + "_data_rx_max.txt"); } catch { }
            close_program = new System.Threading.Timer(TimerCallback, null, 0, timeout);
            Console.WriteLine("step = " + step);
            Console.WriteLine();
            mySerialPort.PortName = port_name;
            mySerialPort.BaudRate = 9600; //19200
            mySerialPort.DataBits = 8;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.Parity = Parity.None;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.RtsEnable = true;
            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            if (debug) {
                Console.WriteLine("port name = " + mySerialPort.PortName);
                Console.ReadLine();
            }
            Stopwatch t = new Stopwatch();
            aaaa:
            t.Restart();
            while (t.ElapsedMilliseconds < 5000) {
                try {
                    mySerialPort.Open();
                    t.Stop();
                    break;
                } catch {
                    Thread.Sleep(250);
                }
                try { mySerialPort.Close(); } catch { }
            }
            if (t.IsRunning) {
                if (!flag_discom) {
                    try { mySerialPort.Close(); } catch { }
                    flag_wait_discom = true;
                    discom("disable", port_name);
                    discom("enable", port_name);
                    flag_discom = true;
                    flag_wait_discom = false;
                    goto aaaa;
                }
                try { mySerialPort.Close(); } catch { }
                File.WriteAllText("test_head_" + head + "_result.txt", "can not open port\r\nFAIL");
                return;
            }
            Console.WriteLine("Port Name = " + mySerialPort.PortName);
            Console.WriteLine("Baud Rate = " + mySerialPort.BaudRate);

            bool falg_error = false;
            flag_data = false;
            t.Restart();
            if (step != "Initial") {
                while (t.ElapsedMilliseconds < 500) {
                    if (flag_data == true) { mySerialPort.ReadExisting(); flag_data = false; t.Restart(); }
                    Thread.Sleep(25);
                }
            }

            for (int re = 0; re < retest; re++) {
                Console.WriteLine("send: " + tx);
                mySerialPort.DiscardInBuffer();
                mySerialPort.DiscardOutBuffer();
                rx_.Clear();
                flag_data = false;
                try { mySerialPort.Write(tx + "\r\n"); } catch { }
                if (step == "Led") {
                    if (debug) {
                        if (Console.ReadLine() == "end") { Thread.Sleep(1000); Application.Exit(); return; }
                        re = 0;
                        continue;
                    }
                    mySerialPort.Close();
                    mySerialPort.Dispose();
                    Application.Exit(); return;
                }
                t.Restart();
                while (t.ElapsedMilliseconds < send_every) {
                    if (flag_data != true) { Thread.Sleep(100); continue; }
                    flag_data = false;
                    t.Stop();
                    break;
                }
                if (t.IsRunning) {
                    if (re < retest - 1) continue;
                    if (!flag_discom) {
                        mySerialPort.Close();
                        flag_wait_discom = true;
                        discom("disable", port_name);
                        discom("enable", port_name);
                        flag_discom = true;
                        flag_wait_discom = false;
                        goto aaaa;
                    }
                    Console.WriteLine("timeout!!!");
                    File.WriteAllText("test_head_" + head + "_result.txt", "timeout readData\r\nFAIL");
                    return;
                }
                t.Restart();
                while (t.ElapsedMilliseconds < send_every) {
                    if (step.Contains("Frequency")) break;
                    if(rx_.Count == 0) { Thread.Sleep(100); continue; }
                    if (rx_[rx_.Count - 1] != "OK" && rx_[rx_.Count - 1] != "ERROR") { Thread.Sleep(100); continue; }
                    break;
                }
                if (rx_[rx_.Count - 1] == "ERROR" && step != "Switch" && !falg_error) {
                    falg_error = true;
                    re = 0;
                    try { mySerialPort.Write("AT+MODE=3\r\n"); } catch { }
                    Thread.Sleep(5000);
                    rx_.Clear();
                    continue;
                }
                double rx_min = 0;
                double rx_max = 0;
                try { rx_min = Convert.ToDouble(rxx_min); } catch { }
                try { rx_max = Convert.ToDouble(rxx_max); } catch { }
                bool result = false;
                string str_result = "";
                if(rx_.Count != 0) foreach (string mmm in rx_) { str_result += mmm + " "; }
                switch (step) {
                    case "OK":
                        if (rx_.Count != 2) { result = false; break; }
                        if (rx_[1] == "OK") result = true;
                        else result = false;
                        str_result = rx_[1]; break;
                    case "Initial":
                        if (rx_.Count != 2) { result = false; break; }
                        if (rx_[1] == "OK") result = true;
                        else result = false;
                        str_result = rx_[1]; break;
                    case "Equal":
                        if (rx_.Count != 3) { result = false; break; }
                        if (rx_[1] == rx) result = true;
                        else result = false;
                        str_result = rx_[1]; break;
                    case "Value":
                        if (rx_.Count != 3) { result = false; break; }
                        Console.WriteLine("rx min = " + rx_min);
                        Console.WriteLine("rx max = " + rx_max);
                        double rx_double = 0;
                        try { rx_double = Convert.ToDouble(rx_[1]); } catch { result = false; str_result = rx_[1]; break; }
                        if (rx_double >= rx_min && rx_double <= rx_max) result = true;
                        else result = false;
                        str_result = rx_double.ToString(); break;
                    case "Light":
                        if (rx_.Count != 3) { result = false; break; }
                        Console.WriteLine("rx min = " + rx_min);
                        Console.WriteLine("rx max = " + rx_max);
                        rx_double = 0;
                        try { rx_double = Convert.ToDouble(rx_[1]); } catch { result = false; str_result = rx_[1]; break; }
                        if (rx_double == 0) rx_double = 1;
                        if (rx_double >= rx_min && rx_double <= rx_max) result = true;
                        else result = false;
                        str_result = rx_double.ToString(); break;
                    case "Temp":
                        if (rx_.Count != 3) { result = false; break; }
                        Console.WriteLine("rx min = " + rx_min);
                        Console.WriteLine("rx max = " + rx_max);
                        rx_double = 0;
                        try { rx_double = Convert.ToDouble(rx_[1]); } catch { result = false; str_result = rx_[1]; break; }
                        rx_double = ((rx_double / 65536) * 175) - 45;
                        Console.WriteLine("rx convert = " + rx_double);
                        if (rx_double >= rx_min && rx_double <= rx_max) result = true;
                        else result = false;
                        str_result = rx_double.ToString("0.#####"); break;
                    case "Humidity":
                        if (rx_.Count != 3) { result = false; break; }
                        Console.WriteLine("rx min = " + rx_min);
                        Console.WriteLine("rx max = " + rx_max);
                        rx_double = 0;
                        try { rx_double = Convert.ToDouble(rx_[1]); } catch { result = false; str_result = rx_[1]; break; }
                        rx_double = (rx_double / 65536) * 100;
                        Console.WriteLine("rx convert = " + rx_double);
                        if (rx_double >= rx_min && rx_double <= rx_max) result = true;
                        else result = false;
                        str_result = rx_double.ToString("0.#####"); break;
                    case "Hardware":
                        foreach (string nnb in rx_) {
                            if (nnb.Contains("LOCKED")) {
                                string[] lkj = tx.Split(',');
                                tx = lkj[0] + "," + "UNSAFE," + lkj[1];
                                break;
                            }
                        }
                        if (rx_.Count != 2) { result = false; break; }
                        if (rx_[1] == rx) result = true;
                        else result = false;
                        str_result = rx_[1]; break;
                    case "Digit":
                        if (rx_.Count != 3) {
                            result = false;
                            break;
                        }
                        if (rx_[1].Count() == Convert.ToInt32(rx)) result = true;
                        else result = false;
                        if (rx_[1].Contains("Error") || rx_[1].Contains("ERROR")) result = false;
                        str_result = rx_[1]; break;
                    case "Frequency":
                        if (rx_.Count != 1) { result = false; break; }
                        if (rx_[0] == rx) result = true;
                        else result = false;
                        str_result = rx_[0]; break;
                    case "Frequency2":
                        if (rx_hex.Count != 1) { result = false; break; }
                        byte rxxxx = Convert.ToByte(rx.Substring(2, 2), 16);
                        if (rx_hex[0] == rxxxx) result = true;
                        else result = false;
                        str_result = "0x" + rx_hex[0].ToString("x2"); break;
                    case "Switch":
                        if (rx_.Count != 3) { result = false; break; }
                        if (rx_[1] == rx) result = true;
                        else result = false;
                        str_result = rx_[1]; break;
                }
                if (debug) Console.ReadLine();
                if (result) {
                    File.WriteAllText("test_head_" + head + "_result.txt", str_result + "\r\nPASS");
                    mySerialPort.Close();
                    mySerialPort.Dispose();
                    break;
                } else {
                    if (re < retest - 1) {
                        while (t.ElapsedMilliseconds < send_every) {
                            Thread.Sleep(50);
                            continue;
                        }
                        continue;
                    }
                    File.WriteAllText("test_head_" + head + "_result.txt", str_result + "\r\nFAIL");
                    mySerialPort.Close();
                    mySerialPort.Dispose();
                }
            }
        }

        static List<string> rx_ = new List<string>();
        static List<int> rx_hex = new List<int>();
        static bool flag_data = false;
        static bool flag_DataReceived = false;
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) {
            Thread.Sleep(150);
            if (flag_DataReceived) return;
            if (step == "Frequency2") {
                int length = 0;
                mySerialPort = (SerialPort)sender;
                try { length = mySerialPort.BytesToRead; } catch { return; }
                int buf = 0;
                for (int i = 0; i < length; i++) {
                    buf = mySerialPort.ReadByte();
                    rx_hex.Add(buf);
                    Console.WriteLine("read: 0x" + buf.ToString("X2"));
                }
                rx_.Add("");
                mySerialPort.DiscardInBuffer();
                mySerialPort.DiscardOutBuffer();
                flag_data = true;
                return;
            }
            if (step == "Initial") {
                string zxc = mySerialPort.ReadExisting();
                zxc = zxc.Replace("\0", "");
                Console.WriteLine("Read: " + zxc);
                string[] xc = zxc.Replace("\r", "").Split('\n');
                for (int i = 0; i < xc.Length; i++) {
                    if (xc[i] == "") continue;
                    string bbbn = xc[i].Trim();
                    if (bbbn == tx && rx_.Count == 0) {
                        rx_.Add(bbbn);
                    }
                    if (bbbn == tx && rx_.Count == 1) {
                        rx_[0] = bbbn;
                    }
                    if (rx_.Count == 0) continue;
                    if (rx_.Count == 1 && rx_[0] != tx) continue;
                    if (bbbn == tx) continue;
                    rx_.Add(bbbn);
                }
                if (rx_.Count == 0) rx_.Add("");
                mySerialPort.DiscardInBuffer();
                mySerialPort.DiscardOutBuffer();
                flag_data = true;
                return;
            }
            string s = mySerialPort.ReadExisting();
            while (true) {
                Thread.Sleep(250);
                string waitBuff = mySerialPort.ReadExisting();
                if (waitBuff != "") s += waitBuff;
                else break;
            }
            s = s.Replace("\0", "");
            s = s.Replace("\r", "\n");
            s = s.Replace("\n\n", "\n");
            Console.WriteLine("Read: " + s);
            string[] ss = s.Split('\n');
            for (int i = 0; i < ss.Length; i++) {
                if (ss[i] == "") continue;
                rx_.Add(ss[i].Trim());
                if (rx_[0] != tx && rx_.Count != 1) {
                    rx_[0] = rx_[0] + rx_[1];
                    rx_.Remove(rx_[1]);
                }
            }
            if (rx_.Count == 0) rx_.Add("");
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            flag_data = true;
        }

        private static bool flag_close = false;
        private static void TimerCallback(Object o) {
            if (!flag_close) { flag_close = true; return; }
            if (debug || flag_wait_discom) return;
            File.WriteAllText("test_head_" + head + "_result.txt", "timeout main\r\nFAIL");
            Environment.Exit(0);
        }
        private static bool flag_wait_discom = false;
        private static void discom(string cmd, string comport) {//enable disable//
            ManagementObjectSearcher objOSDetails2 = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");
            ManagementObjectCollection osDetailsCollection2 = objOSDetails2.Get();
            foreach (ManagementObject usblist in osDetailsCollection2) {
                string arrport = usblist.GetPropertyValue("NAME").ToString();
                if (arrport.Contains(comport)) {
                    Process devManViewProc = new Process();
                    devManViewProc.StartInfo.FileName = "DevManView.exe";
                    devManViewProc.StartInfo.Arguments = "/" + cmd + " \"" + arrport + "\"";
                    devManViewProc.Start();
                    devManViewProc.WaitForExit();
                }
            }
        }
    }
}
