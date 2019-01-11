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
            petla.initMono1("COM1");
            log.Info("Program started");
            petla.fakedecay();
            petla.PostMessage("wait 10");
            petla.PostMessage("wait 10");
            petla.PostMessage("wait 10");
            petla.PostMessage("wait 10");
            petla.PostMessage("wait 10");
            petla.PostMessage("wait 10");
            petla.PostMessage("wait 10");
            petla.PostMessage("dump");
            //petla.PostMessage("scan1 123,6");
            //petla.PostMessage("wait 4e3");
            petla.PostMessage("wait 2000");

            petla.loop();
            while (petla.count > 0) ;
            //Thread.Sleep((int)30e3);
            /*
            petla.ProcessMessages();
            petla.ProcessMessages();
            petla.ProcessMessages();
            petla.ProcessMessages();
            petla.ProcessMessages();
            */

        }
    }
}
