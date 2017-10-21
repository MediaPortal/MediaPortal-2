using System;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
  static class AspectRatioHelper
  {
    internal static string AspectRatioToString(decimal aspectRatio)
    {
      decimal aspectRationRound = Math.Floor(aspectRatio * 10) / 10;

      string output;
      // https://en.wikipedia.org/wiki/Aspect_ratio#Rectangles
      /*if (aspectRationRound <= 1 / 1) // 1:1 => 1
      {
        output = "1:1";
      }
      else if (aspectRationRound <= 5 / 4) // 5:4 => 1,25
      {
        output = "5:4";
      }
      else */if (aspectRationRound <= (decimal)(1.3333)) // 4:3 => 1,3
      {
        output = "4:3";
      }/*else if (aspectRationRound <= 3/2) // 3:2 => 1,5
      {
        output = "3:2";
      }
      else if (aspectRationRound <= 14 / 9) // 14:9 => 1,555
      {
        output = "14:9";
      }
      else  if (aspectRationRound <= 16 / 10) // 16:10 => 1,6
      {
        output = "16:10";
      }
      else if (aspectRationRound <= 15 / 9) // 15:9 => 1,666
      {
        output = "15:9";
      }
      */else if (aspectRationRound <= (decimal)(1.7777)) // 16:9 => 1,7
      {
        output = "16:9";
      }
      /*else if (aspectRationRound <= 2/1) // 2:1 => 2,0
      {
        output = "2:1";
      }
      */else if (aspectRationRound <= (decimal)(2.3333)) // 21:9 => 2,3
      {
        output = "21:9";
      }
      else if (aspectRationRound <= (decimal)(2.4)) // 2.4:1 => 2,4
      {
        output = "2.4:1";
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Info("AspectRatio undef! - '{0}' - round: '{1}'", aspectRatio, aspectRationRound);
        output = "undef";
      }

      return output;
    }
  }
}
