using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System.Text;

public class ELoop
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public delegate void DataAvailableHandler();
    public delegate void ExperimentFinishedHandler();
    public delegate void PresetDoneHandler();
    private Monochromator Mono1;
    private Monochromator Mono2;
    private Tektro.Scope Oscyloskop;
    private string SampleLabel;
    public event DataAvailableHandler OnDataAvailable;
    public event ExperimentFinishedHandler OnExperimentFinished;
    public event PresetDoneHandler OnPresetDone;
    private List<string> Messages;
    private List<Tektro.curve> decaymap;
    private double currWL;
    public int count { get => Messages.Count; }
    public void SetSampleLabel(string label) { SampleLabel = label; }
    public ELoop()
    {
        Messages = new List<string>();
        Mono1 = new Monochromator();
        Mono2 = new Monochromator();
        Oscyloskop = new Tektro.Scope();
        currWL = 0.0;
        decaymap = new List<Tektro.curve>();

    }
    public void getLastCurve(out Tektro.curve data)
    {
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
    public void move1(string wl)
    {
        Mono1.Goto(wl);
        OnPresetDone();
    }
    public void SelectGrating1(string grating)
    {
        Mono1.SelectGrating(grating);
    }
    public void goto1(string com)
    {
        log.Debug("Entered into GOTO with param " + com);
        Mono1.Goto(com);
        if (!Mono1.bCommFailed)
        {

            //while (!Mono1.responseObtained) { Thread.Sleep(1); };
            double.TryParse(com, out currWL);
            Finito();
        }
        else
        {
            Mono1.Fix();
            goto1(com);
        };
    }
    public void getDecayCurve()
    {
        Tektro.curve curve;
        Oscyloskop.dumpList(out curve);
        curve.exc = currWL;
        log.Debug("getdecay:curr WL" + currWL.ToString());
        decaymap.Add(curve);
        OnDataAvailable();
        Finito();
    }
    public void ScanMono1(string com)
    {
        Mono1.ScanTo(com);
        if (!Mono1.bCommFailed)
        {
            //while (!Mono1.responseObtained) { Thread.Sleep(1); };
            double.TryParse(com, out currWL);
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
        while (!Mono2.responseObtained) { };
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
        log.Info("Initialized Mono1 with port name" + pname);
    }
    public void initMono2(string pname)
    {
        Mono2.InitializePort(pname);
        log.Info("Initialized Mono2 with port name" + pname);
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
                    ScanMono1(wl);
                    break;
                }
            case "goto1":
                {
                    string wl = comarg[1];
                    goto1(wl);
                    break;
                }
            case "scan2":
                {
                    string wl = comarg[1];
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
        CultureInfo cul = CultureInfo.InvariantCulture;
        log.Info("Entered dump subroutine");
        string textdump = "";
        string uniq = DateTime.Now.ToString("yyyyMMddHHmmss");
        log.Info("Sample label is " + SampleLabel);
        if (SampleLabel != "") uniq = SampleLabel;
        string path = @"./" + "dump" + uniq + ".txt";
        log.Debug("Data file path " + path);
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        int numCurves = decaymap.Count;
        log.Info("Dump numcurves " + numCurves);
        int pointsPerCurve = decaymap[0].decay.Count;
        log.Info("Dump pointspercurve " + pointsPerCurve);
        if ((numCurves > 0) && (pointsPerCurve > 0))
        {
            textdump += "-0 ";
            for (int k = 0; k < numCurves; k++)
            {
                textdump += decaymap[k].exc.ToString("g7", cul) + " ";
                //log.Debug("Curve " + k + " has " + decaymap[k].decay.Count + " points");
            }
            textdump += "\r\n";
            System.IO.File.AppendAllText(path, textdump);
            log.Debug("Dump, first line done. Writing data: ");
            for (int j = 0; j < pointsPerCurve; j++)
            {
                StringBuilder stringbuilder = new StringBuilder();
                stringbuilder.Append(decaymap[0].decay[j].x.ToString("g7", cul) + " ");
                for (int k = 0; k < numCurves; k++)
                {
                    stringbuilder.Append(decaymap[k].decay[j].y.ToString("g6", cul) + " ");
                }
                stringbuilder.Append("\r\n");
                System.IO.File.AppendAllText(path, stringbuilder.ToString());
                
                if ((j % 50) == 0) log.Info("Dump: " + (sw.ElapsedMilliseconds / 1e3).ToString("G4", cul) + " row j is written, j= " + j);
            }
            log.Info("Dump: " + (sw.ElapsedMilliseconds / 1e3).ToString("G4", cul) + " Finished writing file");
            //System.IO.File.WriteAllText(@"./dump.txt", textdump);
            //Console.WriteLine("Dump finished");
        }
        Finito();
    }

    public void ProcessMessages()
    {
        log.Debug(Messages.Count + " message(s) still in queue");
        if (Messages.Count > 0)
        {
            string msg = Messages[0];
            log.Info("Processing command " + msg);
            Messages.RemoveAt(0);
            Parse(msg);

        }
        else OnExperimentFinished();
    }
    public void fakedecay()
    {
        Tektro.curve fakedec = new Tektro.curve();
        Random random = new Random();
        for (int j = 0; j < 2000; j++)
        {
            fakedec = new Tektro.curve();
            for (int i = 0; i < 1000; i++)
            {
                Tektro.punkt fakepoint = new Tektro.punkt();
                fakepoint.x = i * 1e-6;
                fakepoint.y = 3e-5 * random.NextDouble() + 1e-6 * Math.Sin(i / 150.0);
                fakedec.decay.Add(fakepoint);

            }
            fakedec.exc = 650 + j / 10.0;
            //log.Info("Faked curve " + j);
            decaymap.Add(fakedec);
        }
        log.Debug("Just faked " + decaymap.Count + " curves");

    }

    public void loop()
    {
        log.Debug("Entered into a loop on the thread");
        Task.Factory.StartNew(delegate { ProcessMessages(); });
    }
}