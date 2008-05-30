#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Presentation.SkinEngine.Controls.Brushes
{
  public class BrushCache
  {
    static BrushCache _instance;
    List<BrushTexture> _cache;

    static BrushCache()
    {
      _instance = new BrushCache();
    }

    public static BrushCache Instance
    {
      get
      {
        return _instance;
      }
    }
    public BrushCache()
    {
      _cache = new List<BrushTexture>();
    }

    public BrushTexture GetGradientBrush(GradientStopCollection stops, bool opacitybrush)
    {
      for (int i = 0; i < _cache.Count; ++i)
      {
        if ((_cache[i].OpacityBrush == opacitybrush) && _cache[i].IsSame(stops))
        {
          return _cache[i];
        }
      }
      // Here we must do a deep copy of the source. if we don't, then the cache will change
      // when we change the source. Resulting in that we always get a hit in the cache.
      GradientStopCollection stopsDeepCopy = new GradientStopCollection(null);
      stopsDeepCopy.DeepCopy(stops);

      BrushTexture brush = new BrushTexture(stopsDeepCopy, opacitybrush, null);
      _cache.Add(brush);
      return brush;
    }

  }
}
