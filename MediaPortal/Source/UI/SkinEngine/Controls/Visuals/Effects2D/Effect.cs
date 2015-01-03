#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.MpfElements;
using SharpDX;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects2D
{
  /// <summary>
  /// Provides a base class for all bitmap effects.
  /// </summary>
  public abstract class Effect : DependencyObject
  {
    #region Fields

    protected RectangleF _vertsBounds;
    protected Bitmap1 _input;

    #endregion

    #region (De-)Allocation

    public bool IsAllocated
    {
      get { return Output != null; }
    }

    public abstract void Allocate();
    public abstract void Deallocate();

    public override void Dispose()
    {
      base.Dispose();
      Deallocate();
    }

    #endregion

    #region Processing properties

    public Bitmap1 Input
    {
      get { return _input; }
      set
      {
        bool changed = _input != value;
        _input = value;
        if (changed)
          Allocate();
      }
    }

    public abstract SharpDX.Direct2D1.Effect Output { get; }

    #endregion
  }
}
