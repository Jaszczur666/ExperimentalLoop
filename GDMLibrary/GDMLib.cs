using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System;

namespace GDMLib
    {
        public class GDM
        {
            private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            private SerialPort MotorPort;
        public int stepNum;
        private double curWavenumber=0;
        private const int single_step_duration = 100;
        private bool bInitialized;
        private int stepNumber = 0;
        public GDM()
        {
            MotorPort = new SerialPort
            {
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One
            };
        }
        public void SetCurrentWaveNumber(double wn) { curWavenumber = wn; }
        public void Parse(string command)
        {
            string[] comarg;
            if (command.Contains(" ")) comarg = command.Split(' ');
            else
            {
                comarg = new string[1];
                comarg[0] = command;
            }
            switch (comarg[0]) {
                case "step":
                    {
                        int.TryParse(comarg[1], out int spp);
                        Step(spp);
                        break;
                    }
            }
        }
        private void Step(int numsteps)
        {
            if (bInitialized)
            {
                
                for (int i = 0; i < numsteps; i++)
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    string pom = "0" + ((stepNumber % 4)+1).ToString();
                    MotorPort.Write(pom);
                    log.Info("GDM driver was sent a " + pom + " command");
                    curWavenumber+=0.5;
                    while (stopWatch.ElapsedMilliseconds < single_step_duration) { 
                    
                    };
                    MotorPort.Write("0");
                    stepNumber++;
                    stopWatch.Stop();
                    log.Debug("Step took " + stopWatch.ElapsedMilliseconds+ " ms");

                }
            }
        }
        public void SetPortName(string pname) { MotorPort.PortName = pname; log.Info("GDM Port name was set to :"+pname); }
        public void Initialize() { MotorPort.Open(); bInitialized = true; }
        public void ShutDown() { MotorPort.Close(); bInitialized = false; }
        public double GetCurrentWN() { return curWavenumber; }
    }
    
    }

