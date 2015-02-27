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
  public struct ImageTransitionEffectParams
  {
    public Matrix WorldTransform;
    public Matrix RelativeTransform;
    public Vector4 ImageTransform;
    public Vector4 FrameData;
    public Matrix RelativeTransformA;
    public Vector4 ImageTransformA;
    public Vector4 FrameDataA;
    public Color4 BorderColor;
    public float Opacity;
    public float MixAB;
  }

  public enum ParamIndexIT
  {
    WorldTransform = 0,
    RelativeTransform = 1,
    ImageTransform = 2,
    FrameData = 3,
    RelativeTransformA = 4,
    ImageTransformA = 5,
    FrameDataA = 6,
    BorderColor = 7,
    Opacity = 8,
    MixAB = 9
  }

  #endregion

  [CustomEffect("Effect for rendering textures using different transforms and effects", "Image/Video", "Team MediaPortal", DisplayName = "Generic texture renderer effect")]
  [CustomEffectInput("Source")]
  [CustomEffectInput("Source2")]
  public class ImageTransitionEffect : CustomRenderEffect<ImageTransitionEffectParams>
  {
    public ImageTransitionEffect()
    {
      _effectParams = new ImageTransitionEffectParams
      {
        WorldTransform = Matrix.Identity,
        Opacity = 1.0f,
        RelativeTransform = Matrix.Identity,
        ImageTransform = new Vector4(0f, 0f, 1f, 1f),
        FrameData = new Vector4(0f, 0f, 1f, 1f),
        RelativeTransformA = Matrix.Identity,
        ImageTransformA = new Vector4(0f, 0f, 1f, 1f),
        FrameDataA = new Vector4(0f, 0f, 1f, 1f),
        BorderColor = Color4.Black,
        MixAB = 0f,
      };
    }

    #region Parameters wrapper properties

    /// <summary>
    /// Gets or sets the RelativeTransform.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.WorldTransform, "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", Type = PropertyType.Matrix4x4)]
    public Matrix WorldTransform
    {
      get { return _effectParams.WorldTransform; }
      set { _effectParams.WorldTransform = value; }
    }

    /// <summary>
    /// Gets or sets the Opacity.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.Opacity, "0.0", "1.0", "1.0")]
    public float Opacity
    {
      get { return _effectParams.Opacity; }
      set { _effectParams.Opacity = MathUtil.Clamp(value, 0.0f, 1.0f); }
    }

    /// <summary>
    /// Gets or sets the RelativeTransform.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.RelativeTransform, "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", Type = PropertyType.Matrix4x4)]
    public Matrix RelativeTransform
    {
      get { return _effectParams.RelativeTransform; }
      set { _effectParams.RelativeTransform = value; }
    }

    /// <summary>
    /// Gets or sets the ImageTransform.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.ImageTransform, "(0,0,0,0)", "(1,1,1,1)", "(0,0,0,0)")]
    public Vector4 ImageTransform
    {
      get { return _effectParams.ImageTransform; }
      set { _effectParams.ImageTransform = value; }
    }

    /// <summary>
    /// Gets or sets the ImageTransform.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.FrameData, "(0,0,0,0)", "(1,1,1,1)", "(0,0,0,0)")]
    public Vector4 FrameData
    {
      get { return _effectParams.FrameData; }
      set { _effectParams.FrameData = value; }
    }

    /// <summary>
    /// Gets or sets the RelativeTransform of 2nd texture.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.RelativeTransformA, "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", "(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)", Type = PropertyType.Matrix4x4)]
    public Matrix RelativeTransformA
    {
      get { return _effectParams.RelativeTransformA; }
      set { _effectParams.RelativeTransformA = value; }
    }

    /// <summary>
    /// Gets or sets the ImageTransform of 2nd texture.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.ImageTransformA, "(0,0,0,0)", "(1,1,1,1)", "(0,0,0,0)")]
    public Vector4 ImageTransformA
    {
      get { return _effectParams.ImageTransformA; }
      set { _effectParams.ImageTransformA = value; }
    }

    /// <summary>
    /// Gets or sets the ImageTransform of 2nd texture.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.FrameDataA, "(0,0,0,0)", "(1,1,1,1)", "(0,0,0,0)")]
    public Vector4 FrameDataA
    {
      get { return _effectParams.FrameDataA; }
      set { _effectParams.FrameData = value; }
    }

    /// <summary>
    /// Gets or sets the ImageTransform.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.BorderColor, "(0,0,0,0)", "(1,1,1,1)", "(0,0,0,0)")]
    public Color4 BorderColor
    {
      get { return _effectParams.BorderColor; }
      set { _effectParams.BorderColor = value; }
    }

    /// <summary>
    /// Gets or sets the mix factor for transition of both textures.
    /// </summary>
    [PropertyBinding((int)ParamIndexIT.MixAB, "0.0", "1.0", "1.0")]
    public float MixAB
    {
      get { return _effectParams.MixAB; }
      set { _effectParams.MixAB = MathUtil.Clamp(value, 0.0f, 1.0f); }
    }

    #endregion

    #region Overrides

    public override void SetDrawInformation(DrawInformation drawInfo)
    {
      base.SetDrawInformation(drawInfo);
      _drawInformation.SetInputDescription(1, new InputDescription(Filter.MinimumMagLinearMipPoint, 1));
    }

    /// <summary>
    /// The transition effect has two inputs.
    /// </summary>
    public override int InputCount
    {
      get { return 2; }
    }

    #endregion
  }
}
