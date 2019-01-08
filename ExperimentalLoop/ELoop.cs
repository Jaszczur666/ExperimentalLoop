﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System.Text;

public class ELoop
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    //public delegate void ScanEventHandler(string command);
    //public delegate void GotoEventHandler(string command);
    //public delegate void SleepEventHandler(int duration);
    //public delegate void FinitoEventHandler();
    public delegate void DataAvailableHandler();
    private Monochromator Mono1;
    private Monochromator Mono2;
    private Tektro.Scope Oscyloskop;
    //public event ScanEventHandler ScanEvent1;
    //public event ScanEventHandler ScanEvent2;
    //public event SleepEventHandler SleepEvent;
    //public event GotoEventHandler gotoEvent1;
    //public event FinitoEventHandler Finito;
    public event DataAvailableHandler OnDataAvailable;
    private List<string> Messages;
    private List<Tektro.curve> decaymap;
    private double currWL;
    public int count { get => Messages.Count; }
    public ELoop()
    {
        Messages = new List<string>();
        Mono1 = new Monochromator();
        Mono2 = new Monochromator();
        Oscyloskop = new Tektro.Scope();
        currWL = 0.0;
        //ScanEvent1 += new ScanEventHandler(ScanMono1);
        //SleepEvent += new SleepEventHandler(Sleep);
        //Finito += new FinitoEventHandler(Finito);
        //gotoEvent1 += new GotoEventHandler(goto1);
        decaymap = new List<Tektro.curve>();//List<List<Tektro.punkt>>();

    }
    public void getLastCurve(out Tektro.curve data) {
        if (decaymap.Count > 0)
        {
            data = decaymap[decaymap.Count - 1];
        }
        else
            data = new Tektro.curve();
    }
    public void initScope()
    {
        Oscyloskop.Initialize();
    }
    public void goto1(string com)
    {
        log.Debug("Entere GOTO with param "+com);//Console.WriteLine("Doleciawszy ja do wejścia we goto ");
        Mono1.Goto(com);
        if (!Mono1.repeatNeeded)
        {
            //Console.WriteLine("Test warunku" + Mono1.finishedMove);
            while (!Mono1.finishedMove) { Thread.Sleep(1); };
            //Console.WriteLine("Test2 warunku" + Mono1.finishedMove);
            //Console.WriteLine("Doleciawszy ja do wyjścia z goto ");
            double.TryParse(com, out currWL);
            //Console.WriteLine("DEBUG: currWL " + currWL);
            Finito();
        }
        else goto1(com);
    }
    public void getDecayCurve()
    {
        //List<Tektro.punkt> curve;
        Tektro.curve curve;
        Oscyloskop.dumpList(out curve);
        curve.exc = currWL;
        //Console.WriteLine
        log.Debug("getdecay:curr WL" + currWL.ToString());
        decaymap.Add(curve);
        OnDataAvailable();
        Finito();
               
    }
    public void ScanMono1(string com)
    {
        //Console.WriteLine("Doleciawszy ja do wejścia we scanto ");
        Mono1.ScanTo(com);
        //Console.WriteLine("Test1 warunku" + Mono1.finishedMove);
        if (!Mono1.repeatNeeded)
        {
            while (!Mono1.finishedMove) { Thread.Sleep(1); };
            //Console.WriteLine("Test2 warunku" + Mono1.finishedMove);
            //Console.WriteLine("Doleciawszy ja do wyjścia ze scanto ");
            double.TryParse(com, out currWL);
            //Console.WriteLine("DEBUG: currWL " + currWL);
            Finito();
        }
        else
        {
            Mono1.Fix();
            ScanMono1(com);
        }
    }
    public void ScanMono2(string com)
    {
        Mono2.ScanTo(com);
        while (!Mono2.finishedMove) { };
        Finito();
    }
    public void Finito()
    {
        log.Info("Task done, processing next job");
        loop();// ProcessMessages();
    }
    public void initMono1(string pname)
    {
        Mono1.InitializePort(pname);
    }
    public void initMono2(string pname)
    {
        Mono2.InitializePort(pname);
    }
    public void Sleep(int duration)
    {
        Thread.Sleep(duration);
        Finito();
    }
    public void PostMessage(string mes)
    {
        Messages.Add(mes);
        log.Info("Adding message " + mes);
    }
    public void Parse(string command)
    {
        command = command.ToLower();
        string[] comarg;
        if (command.Contains(" ")) comarg = command.Split(' ');
        else
        {
            comarg = new string[1];
            comarg[0] = command;
        }
        switch (comarg[0])
        {
            case "scan1":
                {
                    string wl = comarg[1];
                    //Console.WriteLine("sl " + wl);
                    ScanMono1(wl);
                    break;
                }
            case "goto1":
                {
                    string wl = comarg[1];
                    //Console.WriteLine("sl " + wl);
                    goto1(wl);
                    break;
                }
            case "scan2":
                {
                    string wl = comarg[1];
                    //Console.WriteLine("sl " + wl);
                    ScanMono2(wl);
                    break;
                }
            case "wait":
                {
                    int duration = 0;
                    int.TryParse(comarg[1], NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out duration);
                    Sleep(duration);
                    break;
                }
            case "decay":
                {
                    getDecayCurve();
                    break;
                }
            case "dump":
                {
                    log.Debug("Flow control, going into dump decay map ");
                    DumpDecayMap();
                    break;
                }
            case "rstscope":
                {
                    Oscyloskop.resetAcquisition();
                    Finito();
                    break;
                }
            
        }
    }

    private void DumpDecayMap()
    {
        log.Info("Entered dump subroutine");
        string textdump = "";
        string uniq = DateTime.Now.ToString("yyyyMMddHHmmss");
        string path = @"./" + uniq + "dump.txt";
        log.Debug("Data file path " + path);        
        System.Diagnostics.Stopwatch sw=new System.Diagnostics.Stopwatch();
        sw.Start();
        int numCurves = decaymap.Count;
        //Console.WriteLine("Dump numcurves " + numCurves);
        log.Info("Dump numcurves " + numCurves);
        int pointsPerCurve = decaymap[0].decay.Count;
        //Console.WriteLine("Dump pointspercurve " + pointsPerCurve);
        log.Info("Dump pointspercurve " + pointsPerCurve);
        if ((numCurves > 0) && (pointsPerCurve > 0))
        {
            textdump += "-0 ";
            for (int k = 0; k < numCurves; k++) textdump += decaymap[k].exc+" ";
            textdump += "\r\n";
            log.Debug("Dump, first line done. Writing data: ");
            for (int j = 0; j < pointsPerCurve; j++)
            {
                textdump += decaymap[0].decay[j].x.ToString("g7", CultureInfo.InvariantCulture) + " ";
                for (int k = 0; k < numCurves; k++)
                {
                    textdump += decaymap[k].decay[j].y.ToString("g9", CultureInfo.InvariantCulture) + " ";                    
                }
                textdump += "\r\n";
                System.IO.File.AppendAllText(path, textdump);
                textdump = "";
                if ((j%50)==0)log.Info("Dump: "+ (sw.ElapsedMilliseconds / 1e3).ToString("G4", CultureInfo.InvariantCulture) + " row j is written, j= "+ j);
            }
            log.Info("Dump: " + (sw.ElapsedMilliseconds / 1e3).ToString("G4", CultureInfo.InvariantCulture) + " Finished writing file");
            //System.IO.File.WriteAllText(@"./dump.txt", textdump);
            //Console.WriteLine("Dump finished");
        }
        Finito();
    }

    public void ProcessMessages()
    {
        //Console.WriteLine(Messages.Count);
        log.Debug(Messages.Count + " message(s) still in queue");
        if (Messages.Count > 0)
        {
            string msg = Messages[0];
            //Console.WriteLine("Processing command "+ msg);
            log.Debug("Processing command " + msg);
            Messages.RemoveAt(0);
            //Console.WriteLine(msg);
            Parse(msg);

        }
    }
    public void fakedecay() {
        Tektro.curve fakedec=new Tektro.curve();
        Random random = new Random();
        for (int j = 0; j < 200; j++)
        {
            fakedec = new Tektro.curve();
            for (int i = 0; i < 1000; i++)
            {
                Tektro.punkt fakepoint = new Tektro.punkt();
                fakepoint.x = i*1e-6;
                fakepoint.y =3e-5*random.NextDouble()+1e-6*Math.Sin(i/150.0);
                fakedec.decay.Add(fakepoint);
                
            }
            fakedec.exc = 650 + j;
            //log.Info("Faked curve " + j);
            decaymap.Add(fakedec);
        }
        log.Debug("Just faked " + decaymap.Count + " curves");

    }

    public void loop() {
        log.Debug("Entered into a loop on the thread");
        //Console.WriteLine(ThreadPool.QueueUserWorkItem(delegate { ProcessMessages(); }));
        Task.Factory.StartNew(delegate { ProcessMessages(); });
    }
}