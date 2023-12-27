using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Cinema.OnlineLibraries.Helper
{

  public class Help
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

    public static List<string> GetDays(int count)
    {
      List<string> days = new List<string>();

      try
      {
        DateTime d1 = DateTime.Now;
        for (int i = 0; i < count; i++)
        {
          DateTime d2 = d1.AddDays(i);
          days.Add(d2.ToString("yyyy-MM-dd"));
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw;
      }

      return days;
    }

    public static string UserRatingFromString(string userRating)
    {
      bool success = double.TryParse(userRating, out var number);
      if (success)
      {
        double r = number / 2;
        return ((int)Math.Round(r)).ToString();
      }
      return "0";
    }
  }

}
