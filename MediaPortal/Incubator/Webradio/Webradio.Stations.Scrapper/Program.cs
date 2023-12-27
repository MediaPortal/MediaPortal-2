using System.Diagnostics;
using Webradio.Stations.Helper;
using Webradio.Stations.RadioNet;

namespace Webradio.Stations;

internal class Program
{
  public static RadioStations RadioStations = new();

  private static bool scrapp = true;

  private static async Task Main(string[] args)
  {
    AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
    Console.CancelKeyPress += ConsoleOnCancelKeyPress;

    if (File.Exists(RadioStations.FileName))
    {
      Console.WriteLine("Found existing " + RadioStations.FileName + ":");
      Console.WriteLine("Test this File? y/n");
      var res = Console.ReadKey();
      Console.WriteLine("");
      if (res.Key == ConsoleKey.Y)
      {
        scrapp = false;
        RadioStations.Read();
      }
    }

    Standby.Suppress();
    var sw = new Stopwatch();
    sw.Start();

    if (scrapp)
    {
      Console.WriteLine("Start Scapper");
      var rns = new Scrapper();
      var re1 = await Task.Run(() => rns.Start());
      RadioStations.Write();
    }

    Console.WriteLine("Start Tester");
    var stt = new StationsTester();
    var re2 = await Task.Run(() => stt.Start());

    Console.WriteLine("Start Cleaner");
    var stc = new StationsCleaner();
    var re3 = await Task.Run(() => stc.Start());

    RadioStations.Write();

    sw.Stop();
    var ts = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
    var time = ts.Hours.ToString("D2") + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2");
    Console.WriteLine("Finished with " + RadioStations.Stations.Count + " Stations in " + time);

    Standby.Enable();

    Console.ReadKey();
  }

  private static void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
  {
    MyExit();
  }

  private static void CurrentDomainOnProcessExit(object? sender, EventArgs e)
  {
    MyExit();
  }

  private static void MyExit()
  {
    Standby.Enable();
  }
}
