namespace Webradio.Stations.Helper;

internal class Help
{
  public static string TimeStampToDate(double unixTimeStamp)
  {
    var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
    return dateTime.ToString("dd.MM.yyyy");
  }

  private static string Runtime(int seconds)
  {
    var ts = TimeSpan.FromSeconds(seconds);

    var rt = ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2");

    if (ts.Hours > 0) rt = ts.Hours.ToString("D2") + ":" + rt;

    return rt;
  }
}
