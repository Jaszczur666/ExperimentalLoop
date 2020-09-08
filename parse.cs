    public void Parse(Command com)
    {
        string command = com.command;
        devices dev = com.device;
        command = command.ToLower();
        string[] comarg;
        if (command.Contains(" ")) comarg = command.Split(' ');
        else
        {
            comarg = new string[1];
            comarg[0] = command;
        }
        switch (dev)
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
            case "box":
                {
                    Box.Initialize();
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
