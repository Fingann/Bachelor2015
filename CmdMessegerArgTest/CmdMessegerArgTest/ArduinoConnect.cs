using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandMessenger;
using CommandMessenger.TransportLayer;
using Microsoft.Win32.SafeHandles;

namespace CmdMessegerArgTest
{
    internal class ArduinoConnect
    {

        #region Variables

        private string _portname = "COM6";
        public string PortName 
        {
            get 
            {
                return _portname;
            }
            set
            {
                _portname = value;
            }
        }
        
        private readonly string _installDir = Path.GetDirectoryName(Application.ExecutablePath);

        #endregion
        
        #region Auto Detect Arduino Port

        private void AutoDetectArduinoPort()
        {
            //http://www.powertheshell.com/reference/wmireference/root/cimv2/win32_serialport/
            var connectionScope = new ManagementScope();
            var serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
            var searcher = new ManagementObjectSearcher(connectionScope, serialQuery);


            try
            {
                
                foreach (ManagementObject item in searcher.Get())
                {
                    //Henter The Description property provides a textual description of the objec
                    string desc = item["Description"].ToString();
                    // Henter The DeviceID property contains a string uniquely identifying the serial port with other devices on the system.
                    string deviceId = item["DeviceID"].ToString();

                    if (!desc.Contains("Arduino")) continue;
                    PortName= deviceId;
                    Logger.Log("Funnet arduino på :" + PortName);
                    MessageBox.Show(PortName); //debugg
                }
            }
            catch (ManagementException e)
            {
                
                Logger.Log(PortName);
                MessageBox.Show(e.ToString());

            }
           
        }
    #endregion

        #region Upload Arduino Software
        
        public void AvrUpdate()
        {
            AutoDetectArduinoPort();
            // Ser om Avrdude filene finnes i instalasjons folder.
            // logger om ikke
            if (!File.Exists(_installDir + @"\avr\avrdude.exe"))
            {
                MessageBox.Show("avrdude tool not found", "AVRUpdate error");
                return;
            }
            if (!File.Exists(_installDir + @"\avr\avrdude.conf"))
            {
                MessageBox.Show("avrdude config file not found", "AVRUpdate error");
                return;
            }
            if (!File.Exists(_installDir + @"\avr\cygwin1.dll"))
            {
                MessageBox.Show("avrdude cygwin dll not found", "AVRUpdate error");
                return;
            }
            if (!File.Exists(_installDir + @"\avr\libusb0.dll"))
            {
                MessageBox.Show("avrdude usb dll not found", "AVRUpdate error");
                return;
            }

            // Skjekker om HEX-filen som skal lastes opp til arduinoen finnes
            // HEX-filen inneholder c-koden som utgjør prodrammet til arduinoen
            if (!File.Exists(_installDir + @"\avr\AVRImage.hex"))
            {
                MessageBox.Show("AVR image not installed", "AVRUpdate error");
                return;
            }
            // Igangsetter en ny cmd-prosess
            Process avrprog = new Process();
            ProcessStartInfo psI = new ProcessStartInfo("cmd")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            avrprog.StartInfo = psI;
            avrprog.Start();
            // Lagrer inn-ut-error i log
            var avrstdin = avrprog.StandardInput;
            var avrstdout = avrprog.StandardOutput;
            var avrstderr = avrprog.StandardError;
            avrstdin.AutoFlush = true;

            // Skriver inn koden i cmd
            avrstdin.WriteLine("cd " + _installDir + @"\avr\");
            avrstdin.WriteLine(
                @"avrdude " + _installDir + @"\avr\avrdude " +
                @"-C" + _installDir + @"\avr\avrdude.conf" +
                @" -v -patmega328p -carduino -P" + PortName + " -b115200 -D " +
                @"-Uflash:w:" + _installDir + @"\avr\AVRImage.hex:i");
            /*avrdude C:\Users\Sondre\Desktop\CmdMessegerArgTest\CmdMessegerArgTest\bin\Debug\avr\avrdude
             * -CC:\Users\Sondre\Desktop\CmdMessegerArgTest\CmdMessegerArgTest\bin\Debug\avr\avrdude.conf
             * -v -patmega328p -carduino -PCOM6 -b115200 -D 
             * -Uflash:w:C:\Users\Sondre\Desktop\CmdMessegerArgTest\CmdMessegerArgTest\bin\Debug\avr\AVRImage.hex:i*/

            avrstdin.Close();
            // Loggfører hendelser
            //Logger.Log(avrstdin.ReadToEnd());
            Logger.Log(avrstdout.ReadToEnd());
            Logger.Log(avrstderr.ReadToEnd());
        }
        #endregion

    }
}