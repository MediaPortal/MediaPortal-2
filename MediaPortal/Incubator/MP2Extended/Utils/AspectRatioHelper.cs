#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

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
