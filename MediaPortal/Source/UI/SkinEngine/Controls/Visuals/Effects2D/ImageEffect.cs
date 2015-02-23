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

using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects2D
{
  #region Effect constants

  [StructLayout(LayoutKind.Sequential)]
  public struct ImageEffectParams
  {
    public Matrix WorldTransform;
    public Matrix RelativeTransform;
    public Vector4 ImageTransform;
    public Vector4 FrameData;
    public Color4 BorderColor;
    public float Opacity;
 }

  public enum ParamIndexI
  {
    WorldTransform = 0,
    RelativeTransform = 1,
    ImageTransform = 2,
    FrameData = 3,
    BorderColor = 4,
    Opacity = 5
  }

  #endregion

  [CustomEffect("Effect for rendering textures using different transforms and effects", "Image/Video", "Team MediaPortal", DisplayName = "Generic texture renderer effect")]
  [CustomEffectInput("Source")]
  public class ImageEffect : CustomRenderEffect<ImageEffectParams>
  {
    public ImageEffect()
    {
      _effectParams = new ImageEffectParams
      {
        WorldTransform = Matrix.Identity,
        Opacity = 1.0f,
        RelativeTransform = Matrix.Identity,
        ImageTransform = new Vector4(0f, 0f, 1f, 1f),
        BorderColor = Color4.Black,
        FrameData = new Vector4(0f, 0f, 1f, 1f)
      };
    }

    public void SetParams(ImageEffectParams newParams)
    {
      // Make update calls only if values have been changed, they cause a huge performance drop.
      if (_effectParams.Equals(newParams))
      {
        _effectParams = newParams;
        UpdateConstants();
      }
    }

    #region Parameters wrapper properties

    /// <summary>
    /// Gets or sets the RelativeTransform.
    /// </summary>
    [PropertyBinding((int)ParamIndexI.WorldTransform, "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", Type = PropertyType.Matrix4x4)]
    public Matrix WorldTransform
    {
      get { return _effectParams.WorldTransform; }
      set { _effectParams.WorldTransform = value; }
    }

    /// <summary>
    /// Gets or sets the Opacity.
    /// </summary>
    [PropertyBinding((int)ParamIndexI.Opacity, "0.0", "1.0", "1.0")]
    public float Opacity
    {
      get { return _effectParams.Opacity; }
      set
      {
        var opacity = MathUtil.Clamp(value, 0.0f, 1.0f);
        _effectParams.Opacity = opacity;
      }
    }

    /// <summary>
    /// Gets or sets the RelativeTransform.
    /// </summary>
    [PropertyBinding((int)ParamIndexI.RelativeTransform, "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", Type = PropertyType.Matrix4x4)]
    public Matrix RelativeTransform
    {
      get { return _effectParams.RelativeTransform; }
      set { _effectParams.RelativeTransform = value; }
    }

    /// <summary>
    /// Gets or sets the ImageTransform.
    /// </summary>
    [PropertyBinding((int)ParamIndexI.ImageTransform, "(0,0,0,0)", "(1,1,1,1)", "(0,0,0,0)")]
    public Vector4 ImageTransform
    {
      get { return _effectParams.ImageTransform; }
      set { _effectParams.ImageTransform = value; }
    }

    /// <summary>
    /// Gets or sets the ImageTransform.
    /// </summary>
    [PropertyBinding((int)ParamIndexI.BorderColor, "(0,0,0,0)", "(1,1,1,1)", "(0,0,0,0)")]
    public Color4 BorderColor
    {
      get { return _effectParams.BorderColor; }
      set { _effectParams.BorderColor = value; }
    }

    /// <summary>
    /// Gets or sets the frame data.
    /// </summary>
    [PropertyBinding((int)ParamIndexI.FrameData, "(0,0,0,0)", "(1,1,1,1)", "(0,0,0,0)")]
    public Vector4 FrameData
    {
      get { return _effectParams.FrameData; }
      set { _effectParams.FrameData = value; }
    }

    #endregion
  }
}
