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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ProgressBar : Control
  {
    #region Protected fields

    protected AbstractProperty _valueProperty;
    protected AbstractProperty _partIndicatorWidthProperty;

    #endregion

    #region Ctor

    public ProgressBar()
    {
      Init();
      Attach();
    }

    void Init()
    {
      Focusable = false;
      _valueProperty = new SProperty(typeof(float), 0.0f);
      _partIndicatorWidthProperty = new SProperty(typeof(float), 0.0f);
    }

    void Attach()
    {
      _valueProperty.Attach(OnValueChanged);
    }

    void Detach()
    {
      _valueProperty.Detach(OnValueChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ProgressBar pb = (ProgressBar)source;
      Value = pb.Value;
      Attach();
    }

    #endregion

    #region Properties

    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    public float Value
    {
      get { return (float)_valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    public AbstractProperty PartIndicatorWidthProperty
    {
      get { return _partIndicatorWidthProperty; }
    }

    public float PartIndicatorWidth
    {
      get { return (float)_partIndicatorWidthProperty.GetValue(); }
      set { _partIndicatorWidthProperty.SetValue(value); }
    }

    public void OnMouseClick(object sender, MouseButtonEventArgs e)
    {
      var position = e.GetPosition(sender as UIElement);
      var value = (float)(position.X /ActualWidth) * 100;

      ServiceRegistration.Get<ILogger>().Debug("ProgressBar: Seeking to {0}% after mouse click", value);
      Value = value;
    }

    #endregion

    #region Overrides

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      CalcPartIndicatorWidth();
    }

    #endregion

    #region Private members

    void OnValueChanged(AbstractProperty property, object oldValue)
    {
      CalcPartIndicatorWidth();
    }

    protected void CalcPartIndicatorWidth()
    {
      PartIndicatorWidth = (float)(ActualWidth * Value / 100.0);
    }

    #endregion
  }
}

