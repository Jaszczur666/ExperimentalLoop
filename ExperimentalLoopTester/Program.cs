using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ExperimentalLoopTester
{
    class Program
    {
        static void Main(string[] args)
        {
            ELoop petla = new ELoop();
            petla.initMono1("COM1");
            petla.fakedecay();
            //petla.PostMessage("wait 1000");
            
            //petla.PostMessage("decay");
            //petla.PostMessage("dump");
            petla.PostMessage("scan1 123,6");
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
