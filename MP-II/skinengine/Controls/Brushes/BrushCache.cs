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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Presentation.Properties;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine.DirectX;
using MyXaml.Core;
namespace SkinEngine.Controls.Brushes
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

    public BrushTexture GetGradientBrush(GradientStopCollection stops, bool opacitybrush,string name)
    {
      for (int i = 0; i < _cache.Count; ++i)
      {
        if (_cache[i].OpacityBrush == opacitybrush && _cache[i].IsSame(stops))
        {
          return _cache[i];
        }
      }
      BrushTexture brush = new BrushTexture(stops, opacitybrush,name);
      _cache.Add(brush);
//      Trace.WriteLine(String.Format("brushes:{0}", _cache.Count));
      return brush;
    }

  }
}
