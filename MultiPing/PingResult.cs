﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MultiPing {
  
  public class PingResult : INotifyPropertyChanged {

    // Stuff to notify UI when a value has changed
    public event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(String propertyName = "") {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    // Internal storage
    private int _ttl;
    private int _time;
    private string _hostname;
    private string _mac;
    private int _fails;

    // Public properties, for the UI to access
    public int sort { get { return ip.GetAddressBytes()[3]; } set { } }
    public IPAddress ip { get; set; }
    public int ttl {
      get { return _ttl; }
      set {
        if (_ttl != value) {
          _ttl = value;
          NotifyPropertyChanged("ttl");
        }
      }
    }
    public int time {
      get { return _time; }
      set {
        if (_time != value) {
          _time = value;
          NotifyPropertyChanged("time");
        }
      }
    }
    public string hostname {
      get { return _hostname; }
      set {
        if (_hostname != value) {
          _hostname = value;
          NotifyPropertyChanged("hostname");
        }
      }
    }
    public string mac {
      get { return _mac; }
      set {
        if (_mac != value) {
          _mac = value;
          NotifyPropertyChanged("mac");
        }
      }
    }
    public int fails {
      get { return _fails; }
      set {
        if (_fails != value) {
          _fails = value;
          NotifyPropertyChanged("fails");
          NotifyPropertyChanged("color");
        }
      }
    }
    public string color {
      get {
        if (_fails >= 0)
          return "#" + ((15 - _fails) * 17).ToString("X2")
                     + ((15 - _fails) * 17).ToString("X2")
                     + ((15 - _fails) * 17).ToString("X2");
        else
          return "#" + ((15 + _fails) * 17).ToString("X2")
                     + "FF"
                     + ((15 + _fails) * 17).ToString("X2");
      }
    }


    public PingResult(IPAddress IP, int Time, int TTL, bool gethost, bool benchmark) {
      ip = IP;
      _ttl = TTL;
      _time = Time;
      _fails = -10;
      if (gethost) {
        Stopwatch stop = new Stopwatch();
        stop.Start();
        Console.WriteLine("gethost " + ip);
        if (benchmark)
          getHostnameAsync(stop);
        else
          getHostnameAsync(null);
        hostname = "DNS search";
        // Delay this (enqueue on UI thread) to prevent UI to freeze on the first click
        MainWindow.disp.BeginInvoke(DispatcherPriority.Background,
          new Action(() => {
                mac = GetMacAddress(ip.ToString());
            if (benchmark)
              mac = stop.Elapsed.Seconds.ToString();
          }));
      }
    }

    private async void getHostnameAsync(Stopwatch watch) {
      try {
        //Console.WriteLine("getting hostname for " + ip);
        var host = await Dns.GetHostEntryAsync(ip);
        //foreach (string s in host.Aliases)
        //  Console.WriteLine(s);
        hostname = host.HostName;
        if (watch != null)
          hostname = watch.Elapsed.Seconds.ToString();
        //Console.WriteLine(ip + " done.");
        /*DnsEndPoint dns = new DnsEndPoint(ip.ToString(), 80);
        hostname = dns.Host;*/
      }
      catch (SocketException) {
        if (watch != null)
          hostname = watch.Elapsed.Seconds.ToString();
        else
          hostname = "";
      };
    }
    

    public string GetMacAddress(string ipAddress) {
      try {
        //Console.WriteLine("zzzz...");
        //Thread.Sleep(1999);
        //Console.WriteLine("arping "+ipAddress+"...");
        //Stopwatch stop = new Stopwatch();
        //stop.Start();
        string macAddress = string.Empty;
        System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
        pProcess.StartInfo.FileName = "arp";
        pProcess.StartInfo.Arguments = "-a " + ipAddress;
        pProcess.StartInfo.UseShellExecute = false;
        pProcess.StartInfo.RedirectStandardOutput = true;
        pProcess.StartInfo.CreateNoWindow = true;
        pProcess.Start();
        string strOutput = pProcess.StandardOutput.ReadToEnd();
        string[] substrings = strOutput.Split('-');
        if (substrings.Length >= 8) {
          string expression = substrings[3].Substring(Math.Max(0, substrings[3].Length - 2)) + ":" + substrings[4] + ":" + substrings[5] + "(.*?)\n";
          var matches = Regex.Match(MainWindow.manuf, expression, RegexOptions.IgnoreCase);
          string result = matches.Groups[1].Value;
          if (result.Contains('#'))
            result = result.Substring(result.IndexOf('#') + 1);
          //return stop.Elapsed.ToString();
          return result.TrimStart();
        } else /*return stop.Elapsed.ToString();*/ return "";
      } catch (Exception e) { return e.ToString(); }
    }


  }
}