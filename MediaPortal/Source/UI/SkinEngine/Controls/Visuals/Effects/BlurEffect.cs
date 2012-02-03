#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public sealed class BlurEffect : ImageEffect
  {
    #region Consts

    private const string EFFECT_BLUR = "effects\\zoom_blur";

    #endregion

    #region Protected fields

    protected AbstractProperty _radiusProperty;

    #endregion


    #region Ctor & maintainance

    public BlurEffect()
    {
      _partialShaderEffect = EFFECT_BLUR;
      Init();
    }

    void Init()
    {
      _radiusProperty = new SProperty(typeof(double), 1.0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BlurEffect el = (BlurEffect) source;
      Radius = el.Radius;
    }

    #endregion

    #region Properties

    public AbstractProperty RadiusProperty
    {
      get { return _radiusProperty; }
    }

    public double Radius
    {
      get { return (double) _radiusProperty.GetValue(); }
      set { _radiusProperty.SetValue(value); }
    }

    #endregion
  }
}
