namespace Webradio.Stations;

internal class StationsCleaner
{
  private static RadioStations _cleanRadioStations;

  public bool Start()
  {
    _cleanRadioStations = new RadioStations { Version = Program.RadioStations.Version };

    foreach (var station in Program.RadioStations.Stations)
    {
      var streams = new List<Stream>();
      foreach (var stream in station.Streams)
        if (stream.Status == "OK")
          streams.Add(stream);

      if (streams.Count > 0)
      {
        var st = station;
        st.Streams = streams;
        _cleanRadioStations.Stations.Add(st);
      }
    }

    Program.RadioStations = _cleanRadioStations;

    Console.WriteLine("Done");
    Console.WriteLine("");

    return true;
  }
}
