using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;

namespace boxCarLib
{
    
    public class Boxcar: IDisposable
    {
        const long  boxcar_timeout= 5000;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly SerialPort boxCarPort;
        private string readBuffer;
        bool bresponseReceived,bTimedOut;
        public Boxcar(string pName) {
            boxCarPort = new SerialPort
            {
                PortName = pName,
                BaudRate = 19200,
                Parity = Parity.None,
                StopBits = StopBits.Two,
                NewLine = "\r"
            };
            boxCarPort.DataReceived += BoxCarPort_DataReceived;
        }
        public Boxcar()
        {
            boxCarPort = new SerialPort
            {
                PortName = "C",
                BaudRate = 19200,
                Parity = Parity.None,
                StopBits = StopBits.Two,
                NewLine = "\r"
            };
            boxCarPort.DataReceived += BoxCarPort_DataReceived;
        }
        public string LastResp() { return readBuffer; }
        
        public string GetValue(int channel)
        {
            log.Info("Request sent");
            string com = "?" + channel.ToString();
            SendCommand(com);
            Stopwatch swResp = new Stopwatch();
            swResp.Start();
            while ((!bresponseReceived)&&(swResp.ElapsedMilliseconds<boxcar_timeout) ) { Thread.Sleep(10); };
            if (!bresponseReceived)
            {
                log.Error("Communication timed out");
                bTimedOut = true;
            }
            log.Warn("End of listening loop");
            return LastResp();
        }

        private void BoxCarPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            GetResponse(out readBuffer);

        }
        public void SetPortName(string pname) { boxCarPort.PortName = pname; }
        public void SendCommand(string command)
        {
            if (boxCarPort.IsOpen)
            {
                boxCarPort.Write(command + "\r");
                log.Debug(command + " was sent to boxcar");
                bresponseReceived = false;
                bTimedOut = false;
            }
        }
        private void GetResponse(out string response)
        {
            
            if (boxCarPort.IsOpen)
            {
                response = boxCarPort.ReadLine();
                log.Info("Response obtained was "+response);
                bresponseReceived = true;
                log.Info("Flag set");
            }
            else response = "Failure";
        }
        public void Parse(string com){

            log.Info("Boxcar command "+com +" sent");
            SendCommand(com);
        }
        public void Initialize() {
            boxCarPort.Open();
            log.Debug("trying to send w0 ");
            SendCommand("W0");
            log.Info("The W0 command was sent, boxcar ready for further instructions");
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                boxCarPort.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
