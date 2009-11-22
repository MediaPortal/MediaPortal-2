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

using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class GradientStop : DependencyObject, IObservable
  {
    #region Private fields

    Property _colorProperty;
    Property _offsetProperty;

    #endregion

    #region Ctor

    public GradientStop()
    {
      Init();
      Attach();
    }

    public GradientStop(double offset, Color color)
    {
      Init();
      Color = color;
      Offset = offset;
      Attach();
    }

    void Init()
    {
      _colorProperty = new Property(typeof(Color), Color.White);
      _offsetProperty = new Property(typeof(double), 0.0);
    }

    void Attach()
    {
      _colorProperty.Attach(OnPropertyChanged);
      _offsetProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _colorProperty.Detach(OnPropertyChanged);
      _offsetProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Detach();
      GradientStop s = source as GradientStop;
      Color = copyManager.GetCopy(s.Color);
      Offset = copyManager.GetCopy(s.Offset);
      Attach();
    }

    #endregion

    public event ObjectChangedHandler ObjectChanged;

    #region Protected methods

    protected void Fire()
    {
      if (ObjectChanged != null)
        ObjectChanged(this);
    }

    protected void OnPropertyChanged(Property prop, object oldValue)
    {
      Fire();
    }

    #endregion

    #region Public properties

    public Property ColorProperty
    {
      get { return _colorProperty; }
    }

    public Color Color
    {
      get { return (Color) _colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    public Property OffsetProperty
    {
      get { return _offsetProperty; }
    }

    public double Offset
    {
      get { return (double) _offsetProperty.GetValue(); }
      set { _offsetProperty.SetValue(value); }
    }

    #endregion

    public override string ToString()
    {
      return Offset + ": " + Color.ToString();
    }
  }
}
