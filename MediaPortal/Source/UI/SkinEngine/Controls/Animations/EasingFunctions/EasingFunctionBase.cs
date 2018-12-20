#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations.EasingFunctions
{
  public abstract class EasingFunctionBase : DependencyObject, IEasingFunction
  {
    #region Protected fields

    protected AbstractProperty _easingModeProperty;

    #endregion

    #region Ctor

    public EasingFunctionBase()
    {
      Init();
    }

    private void Init()
    {
      _easingModeProperty = new SProperty(typeof(EasingMode), EasingMode.EaseOut);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      EasingFunctionBase e = (EasingFunctionBase)source;
      EasingMode = e.EasingMode;
    }

    #endregion

    #region Public properties

    public AbstractProperty EasingModeProperty
    {
      get { return _easingModeProperty; }
    }

    public EasingMode EasingMode
    {
      get { return (EasingMode)_easingModeProperty.GetValue(); }
      set { _easingModeProperty.SetValue(value); }
    }

    #endregion

    #region IEasingFunction

    public double Ease(double normalizedTime)
    {
      switch (EasingMode)
      {
        case EasingMode.EaseIn:
          return EaseInCore(normalizedTime);
        case EasingMode.EaseOut:
          // Same as EaseIn, except time and result are reversed
          return 1.0 - EaseInCore(1.0 - normalizedTime);
        case EasingMode.EaseInOut:
        default:
          // Combination of EaseIn & EaseOut
          return (normalizedTime < 0.5) ?
                     EaseInCore(normalizedTime * 2.0) * 0.5 :
              (1.0 - EaseInCore((1.0 - normalizedTime) * 2.0)) * 0.5 + 0.5;
      }
    }

    #endregion

    protected abstract double EaseInCore(double normalizedTime);
  }
}
