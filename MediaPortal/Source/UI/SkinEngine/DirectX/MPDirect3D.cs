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

using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX
{
  public static class MPDirect3D 
  {
    private static Direct3DEx _d3d;

    public static Direct3DEx Direct3D
    {
      get { return _d3d; }
    }

    public static void Load()
    {
      if (_d3d == null)
        _d3d = new Direct3DEx();
    }

    public static void Unload()
    {
      if (_d3d != null)
        _d3d.Dispose();
      _d3d = null;
    }
  }
}