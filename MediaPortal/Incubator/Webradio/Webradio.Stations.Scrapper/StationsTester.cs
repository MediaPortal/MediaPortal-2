using System.Diagnostics;
using System.Net;
using Webradio.Stations.Helper;

namespace Webradio.Stations;

internal class StationsTester
{
  private static RadioStations _testedRadioStations;

  private static int max;
  private static int done;
  private static int ok;
  private static int nok;

  private static Stopwatch stopwatch;
  private static readonly SemaphoreSlim semaphoreSlim = new(10);

  public async Task<bool> Start()
  {
    _testedRadioStations = new RadioStations();
    _testedRadioStations.Version = Program.RadioStations.Version;
    max = GetStreamsCount();

    stopwatch = new Stopwatch();
    stopwatch.Start();

    List<Task<bool>> tasks = Program.RadioStations.Stations.Select(async station =>
    {
      await semaphoreSlim.WaitAsync();
      try
      {
        return await TestStationRequest(station);
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }).ToList();
    await Task.WhenAll(tasks);

    Program.RadioStations = _testedRadioStations;

    stopwatch.Stop();
    var ts = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
    var v = ts.Hours.ToString("D2") + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2");
    Console.WriteLine();
    Console.WriteLine("Done in " + v);
    Console.WriteLine("");

    return true;
  }

  internal static async Task<bool> TestStationRequest(RadioStation station)
  {
    foreach (var stream in station.Streams)
    {
      var ret = await Http.Check(stream.Url);
      if (ret == HttpStatusCode.OK)
      {
        stream.Status = "OK";
        ok++;
      }
      else
      {
        stream.Status = "NOK";
        nok++;
      }

      done++;
    }

    _testedRadioStations.Stations.Add(station);

    var value = "OK: " + ok + " | FAIL: " + nok + " | Done: " + done + "/" + max + " Completed in " + GetMaxTime();
    Console.Write("\r{0}   ", value);

    return true;
  }

  private static int GetStreamsCount()
  {
    var sum = 0;
    foreach (var station in Program.RadioStations.Stations) sum += station.Streams.Count();

    return sum;
  }

  private static string GetMaxTime()
  {
    var timeForOne = stopwatch.ElapsedMilliseconds / done;
    var timeForRest = (max - done) * timeForOne;
    var ts = TimeSpan.FromMilliseconds(timeForRest);
    return ts.Hours.ToString("D2") + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2");
  }
}
