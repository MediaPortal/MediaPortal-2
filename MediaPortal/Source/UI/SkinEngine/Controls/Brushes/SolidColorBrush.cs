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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class SolidColorBrush : Brush
  {
    #region Protected properties

    protected AbstractProperty _colorProperty;

    #endregion

    #region Ctor

    public SolidColorBrush()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    public override void Allocate()
    {
      base.Allocate();
      SetBrush(new SharpDX.Direct2D1.SolidColorBrush(GraphicsDevice11.Instance.Context2D1, Color));
    }

    void Init()
    {
      _colorProperty = new SProperty(typeof(Color), Color.White);
    }

    void Attach()
    {
      _colorProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _colorProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      SolidColorBrush b = (SolidColorBrush)source;
      Color = b.Color;
      Attach();
    }

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      base.OnPropertyChanged(prop, oldValue);
      UpdateBrush();
    }

    protected void UpdateBrush()
    {
      // Forward all property changes to internal brush
      var brush = _brush2D as SharpDX.Direct2D1.SolidColorBrush;
      if (brush != null)
      {
        brush.Color = Color;
      }
    }

    #endregion

    public AbstractProperty ColorProperty
    {
      get { return _colorProperty; }
    }

    public Color Color
    {
      get { return (Color)_colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }
  }
}
