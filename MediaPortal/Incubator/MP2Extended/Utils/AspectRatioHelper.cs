using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
  static class AspectRatioHelper
  {
    internal static string AspectRatioToString(float aspectRatio)
    {
      double aspectRationRound = Math.Floor(aspectRatio * 10) / 10;

      string output;
      // https://en.wikipedia.org/wiki/Aspect_ratio#Rectangles
      if (aspectRationRound <= 1.3) // 4:3
      {
        output = "4:3";
      }else if (aspectRationRound <= 1.5) // 3:2
      {
        output = "3:2";
      }
      else if (aspectRationRound <= 1.6) // 16:10
      {
        output = "16:10";
      }
      else if (aspectRationRound <= 1.7) // 16:9
      {
        output = "16:9";
      }
      else
      {
        output = "undef";
      }

      return output;
    }
  }
}
