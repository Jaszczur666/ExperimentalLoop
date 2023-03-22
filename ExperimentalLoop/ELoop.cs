using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System.Text;
public enum Devices {none, dm750,dm150,scope,boxcar,gdm }
public class Command{
    public Devices device;
    public string command;
    public Command() { device = Devices.none; command = ""; }
    public Command(Devices d, string c) {device = d; command = c; }
    }
public class ELoop: IDisposable
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public delegate void DataAvailableHandler();
    public delegate void ExperimentFinishedHandler();
    public delegate void PresetDoneHandler();
    private readonly Monochromator Mono1;
    private readonly Monochromator Mono2;
    private readonly GDMLib.GDM GDM1000;
    private readonly boxCarLib.Boxcar Box;
    private readonly Tektro.Scope Oscyloskop;
    private string SampleLabel="";
    private long pings = 0;
    private bool btimerStarted = false;
    public double pingspersecond()
    {
        log.Debug("Pings " + pings + "  in " + 1e-3 * pingometer.ElapsedMilliseconds + " seconds");
        if (pings > 0) return 1e-3 * pingometer.ElapsedMilliseconds / pings;
        else return 7;
    }
    public event DataAvailableHandler OnDataAvailable;
    public event ExperimentFinishedHandler OnExperimentFinished;
    public event PresetDoneHandler OnPresetDone;
    private readonly List<Command> Messages;
    private readonly List<Tektro.curve> decaymap;
    private readonly List<Tektro.punkt> spectrum;
    private double currWL1,currWL2,currWN;
    public Tektro.punkt lastpoint;
    public bool isWavenumber=false;
    private System.Diagnostics.Stopwatch pingometer;
    public int Count { get => Messages.Count; }
    public void SetSampleLabel(string label) { SampleLabel = label; }
    public ELoop()
    {
        Messages = new List<Command>();
        
        Mono1 = new Monochromator();
        Mono2 = new Monochromator();
        GDM1000 = new GDMLib.GDM();
        Box = new boxCarLib.Boxcar();
        Oscyloskop = new Tektro.Scope();
        currWL1 = 0.0;
        currWL2 = 0.0;
        spectrum = new List<Tektro.punkt> ();
        decaymap = new List<Tektro.curve>();
        pingometer = new System.Diagnostics.Stopwatch();

    }
    public void GetLastCurve(out Tektro.curve data)
    {
        if (decaymap.Count > 0)
        {
            data = decaymap[decaymap.Count - 1];
        }
        else
            data = new Tektro.curve();
    }
    public void InitScope()
    {
        Oscyloskop.Initialize();
    }
    public void InitBox(string pname) { Box.SetPortName(pname); Box.Initialize(); }
    public void InitGDM(string pname) { GDM1000.SetPortName(pname); GDM1000.Initialize(); }
    public void Move1(string wl)
    {
        Mono1.Goto(wl);
        OnPresetDone?.Invoke();
    }
    public void SelectGrating1(string grating)
    {
        Mono1.SelectGrating(grating);
    }
    /*public void goto1(string com)
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
    }*/
    public double StartWN { get; set; }
    public double EndWN { get; set; }
    public void GetDecayCurve()
    {
        Oscyloskop.dumpList(out Tektro.curve curve);
        if (isWavenumber) curve.exc = currWN;
        else curve.exc = currWL1;
        log.Debug("getdecay:curr WL" + currWL1.ToString());
        decaymap.Add(curve);
        log.Debug("Just added curve to map, firing event");
        OnDataAvailable?.Invoke();
        log.Debug("About to finish here");
        Finito();
    }
    /*public void ScanMono1(string com)
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
    }*/
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
    public void SetCurrentWaveNumber(double WN)
    {
        currWN = WN;
        GDM1000.SetCurrentWaveNumber(WN);
    }
    public void PostMessage(Devices dev, string mes)
    {
        Messages.Add(new Command(dev, mes));
        log.Info("Adding message " + mes + " to device "+dev );
    }
    public void PostMessage(string mes)
    {
        PostMessage(Devices.none, mes);
    }
    public void Parse(Command com)
    {
        
        string command = com.command;
        Devices dev = com.device;
        command = command.ToLower();
        //string[] comarg;
        //if (command.Contains(" ")) comarg = command.Split(' ');
        //else
        //{
        //    comarg = new string[1];
        //    comarg[0] = command;
        //}
        switch (dev)
        {
            case Devices.dm750:
                {
                    Mono1.Parse(command);
                    double.TryParse(Mono1.cWavLen, out currWL1);
                    Finito();
                    break;
                }
            case Devices.dm150:
                {
                    Mono2.Parse(command);
                    double.TryParse(Mono2.cWavLen, out currWL2);
                    Finito();
                    break;
                }
            case Devices.boxcar:
                {
                    //Box.Initialize();
                    Box.Parse(command);
                    //log.Info("stan b "+Box.GetValue(1));
                    Finito();
                    break;
                }
            case Devices.gdm:
                {
                    
                    GDM1000.Parse(command);
                    currWN = GDM1000.GetCurrentWN();
                    Finito();
                    break;
                }
            case Devices.none:
                {
                    string[] comarg;
                    if (command.Contains(" ")) comarg = command.Split(' ');
                    else
                    {
                        comarg = new string[1];
                        comarg[0] = command;
                    }

                    switch (comarg[0])
                    {
                        case "wait":
                            {
                                int.TryParse(comarg[1], NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out int duration);
                                Sleep(duration);
                                break;
                            }
                        case "decay":
                            {
                                GetDecayCurve();
                                break;
                            }
                        case "dump":
                            {
                                log.Debug("Flow control, going into dump decay map ");
                                DumpDecayMap();
                                break;
                            }
                        case "getval":
                            {
                                log.Info("Getval started");
                                string tVal = Box.GetValue(1);
                                log.Info("Getvalue returned ");

                                double.TryParse(tVal.Replace(".", ","), out double y);
                                lastpoint = new Tektro.punkt(currWN, y);
                                spectrum.Add(lastpoint);
                                OnDataAvailable();
                                currWN += 1;
                                log.Info("Value added to spectrum");
                                Finito();
                                break;
                            }
                        case "dumpspec":
                            {
                                if (comarg.Length > 1)
                                {
                                    string path="";
                                    for (int i= 1;i<comarg.Length;i++) path += comarg[i]+" ";
                                    log.Info("Dumping to file" + path);
                                    DumpSpectrum(path);

                                }
                                else
                                {
                                    log.Info("Dumping spectrum no filename provided ");
                                    DumpSpectrum();
                                }

                                Finito();
                                break;
                            }
                        case "rstscope":
                            {
                                Oscyloskop.resetAcquisition();
                                Finito();
                                break;
                            }
                        case "ping":
                            {
                                pings++;
                                Finito();
                                break;
                            }
                    }
                    
                    break;
                }



        }
    }
    private void DumpSpectrum()
    {
        CultureInfo cul = CultureInfo.InvariantCulture;
        log.Info("Entered dump spectrum subroutine");
        string textdump = "";
        string uniq = DateTime.Now.ToString("yyyyMMddHHmmss");
        log.Info("Sample label is \""+SampleLabel+"\"");
        if (SampleLabel != "") uniq = SampleLabel;
        string path = @"./" + "dump" + uniq + ".txt";
        log.Debug("Data file path " + path);
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        for (int i = 0; i<spectrum.Count; i++) {
            textdump += spectrum[i].x.ToString("g7", cul) + " " + spectrum[i].y.ToString("g7", cul)+"\r\n";
                }
        System.IO.File.WriteAllText(path, textdump);
    }
    private void DumpSpectrum( string fname)
    {
        CultureInfo cul = CultureInfo.InvariantCulture;
        log.Info("Entered dump spectrum subroutine");
        string textdump = "";
        string path = fname;
        log.Debug("Data file path " + path);
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        for (int i = 0; i < spectrum.Count; i++)
        {
            textdump += spectrum[i].x.ToString("g7", cul) + " " + spectrum[i].y.ToString("g7", cul) + "\r\n";
        }
        System.IO.File.WriteAllText(path, textdump);
    }
    public void FakeSpectrum()
    {
        Tektro.punkt fakepoint = new Tektro.punkt(0, 1);
        spectrum.Add(fakepoint);
        fakepoint = new Tektro.punkt(0, 1)
        {
            x = 100,
            y = 1.12
        };
        spectrum.Add(fakepoint);
        fakepoint = new Tektro.punkt(0, 1);
        fakepoint.y += 0.12;
        spectrum.Add(fakepoint);
        fakepoint = new Tektro.punkt(0, 1);
        fakepoint.y += 0.22;
        spectrum.Add(fakepoint);
        fakepoint = new Tektro.punkt(0, 1);
        fakepoint.y += 0.02;
        spectrum.Add(fakepoint);


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
            Command msg = Messages[0];
            log.Info("Processing command " + msg.command + " for device " + msg.device);
            Messages.RemoveAt(0);
            Parse(msg);

        }
        else
        {
            OnExperimentFinished?.Invoke();
            log.Info("Experiment finished");
            log.Info("Pingometer stats " + pingometer.Elapsed + " " + pings + " rate = "+pingspersecond());
        }
    }
    public void fakedecay()
    {
        Tektro.curve fakedec;// = new Tektro.curve();
        Random random = new Random();
        for (int j = 0; j < 2000; j++)
        {
            fakedec = new Tektro.curve();
            for (int i = 0; i < 1000; i++)
            {
                Tektro.punkt fakepoint = new Tektro.punkt
                {
                    x = i * 1e-6,
                    y = 3e-5 * random.NextDouble() + 1e-6 * Math.Sin(i / 150.0)
                };
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
        if (!btimerStarted)
        {
            pingometer.Restart();
            btimerStarted = true;
        }
        Task.Factory.StartNew(delegate { ProcessMessages(); });
    }


    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // dispose managed resources
            Mono1.Dispose();
            Mono2.Dispose();
            Oscyloskop.Dispose();
            Box.Dispose();

        }
        // free native resources
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}