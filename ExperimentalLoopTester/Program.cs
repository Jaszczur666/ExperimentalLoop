using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace ExperimentalLoopTester
{
    class Program
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            
            ELoop petla = new ELoop();
            //petla.initMono1("COM1");
            log.Info("Program started");
            //petla.OnDataAvailable += gotData;
            //petla.fakedecay();
            //petla.InitScope();
            petla.SetCurrentWaveNumber(12345);
            //petla.fakedecay();
            //petla.PostMessage("decay");
            //petla.PostMessage("decay");
            //            petla.PostMessage("decay");
            //log.Info("Znowu w mainie");
            //petla.SetSampleLabel("");
            petla.FakeSpectrum();
            petla.PostMessage("dumpspec");
            string path = @"H:\Wuje muje\dzikie wunsze\test.txt";
            petla.PostMessage("dumpspec "+path);
            //petla.InitBox("COM1");
            //
            petla.PostMessage("wait 4e1");
            //petla.PostMessage("wait 20000");

            petla.loop();
            while (petla.Count > 0)Thread.Sleep(10000);
            //Thread.Sleep((int)30e3);
            /*
            petla.ProcessMessages();
            petla.ProcessMessages();
            petla.ProcessMessages();
            petla.ProcessMessages();
            petla.ProcessMessages();
            */
            petla.Dispose();
        }
        static void gotData()
        {
            log.Info("Data arrived!");
        }
    }
}
