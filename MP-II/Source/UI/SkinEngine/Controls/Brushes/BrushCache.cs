#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  // TODO Albert: Cleanup brushes, when not used any more
  public class BrushCache
  {
    static BrushCache _instance;
    ICollection<BrushTexture> _cache;

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
      foreach (BrushTexture texture in _cache)
        if ((texture.OpacityBrush == opacitybrush) && texture.IsSame(stops))
          return texture;
      // Here we must do a copy of the gradient stops. If we don't, the cache will change
      // when the stops are changed outside.
      BrushTexture brush = new BrushTexture(new GradientStopCollection(stops), opacitybrush, null);
      _cache.Add(brush);
      return brush;
    }

  }
}
