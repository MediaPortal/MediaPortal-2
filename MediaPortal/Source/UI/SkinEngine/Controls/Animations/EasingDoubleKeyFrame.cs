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
using MediaPortal.UI.SkinEngine.Controls.Animations.EasingFunctions;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class EasingDoubleKeyFrame : DoubleKeyFrame
  {
    #region Protected fields
    
    protected AbstractProperty _easingFunctionProperty;

    #endregion

    #region Ctor

    public EasingDoubleKeyFrame()
    {
      Init();
    }

    void Init()
    {
      _easingFunctionProperty = new SProperty(typeof(IEasingFunction), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      EasingDoubleKeyFrame k = (EasingDoubleKeyFrame)source;
      EasingFunction = copyManager.GetCopy(k.EasingFunction);
    }

    #endregion

    #region Public properties

    public AbstractProperty EasingFunctionProperty
    {
      get { return _easingFunctionProperty; }
    }

    public IEasingFunction EasingFunction
    {
      get { return (IEasingFunction)_easingFunctionProperty.GetValue(); }
      set { _easingFunctionProperty.SetValue(value); }
    }

    #endregion

    #region DoubleKeyFrame

    public override double Interpolate(double start, double keyframe)
    {
      if (keyframe <= 0.0) return start;
      if (keyframe >= 1.0) return Value;
      if (double.IsNaN(keyframe)) return start;

      IEasingFunction easingFunction = EasingFunction;
      if (easingFunction != null)
        keyframe = easingFunction.Ease(keyframe);

      return (start + ((Value - start) * keyframe));
    }

    #endregion
  }
}
