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

using System;
using System.Drawing;
using MediaPortal.Core;
using MediaPortal.Core.Players;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using SkinEngine.Effects;
using SkinEngine.Players;

namespace SkinEngine.Controls
{
  public class Image : Control
  {
    #region variables

    private VertextBufferAsset _image;
    private Property _source;
    private Property _defaultTexture;
    private string _textureName = "";
    private Property _effect;
    private Property _useThumbNail;
    private Property _keepAspectRatio;
    private Property _zoom;
    private SkinEngine.Fonts.Font.Align _align = SkinEngine.Fonts.Font.Align.Left;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public Image(Control parent)
      : base(parent)
    {
      _source = new Property("");
      _defaultTexture = new Property("");
      _effect = new Property("");
      _useThumbNail = new Property(true);
      _keepAspectRatio = new Property(false);
      _zoom = new Property(true);
    }

    /// <summary>
    /// Gets or sets the use thumb nail property.
    /// </summary>
    /// <value>The use thumb nail property.</value>
    public Property UseThumbNailProperty
    {
      get { return _useThumbNail; }
      set { _useThumbNail = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to keep the aspect ratio of the image.
    /// </summary>
    /// <value><c>true</c> if keep aspect ratio; otherwise, <c>false</c>.</value>
    public bool KeepAspectRatio
    {
      get { return (bool)_keepAspectRatio.GetValue(); }
      set { _keepAspectRatio.SetValue(value); }
    }


    /// <summary>
    /// Gets or sets the keep aspect ratio property.
    /// </summary>
    /// <value>The keep aspect ratio property.</value>
    public Property KeepAspectRatioProperty
    {
      get { return _keepAspectRatio; }
      set { _keepAspectRatio = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to to zoom the image.
    /// </summary>
    /// <value><c>true</c> if zoom enabled; otherwise, <c>false</c>.</value>
    public bool Zoom
    {
      get { return (bool)_zoom.GetValue(); }
      set { _zoom.SetValue(value); }
    }
    /// <summary>
    /// Gets or sets a the alignment.
    /// </summary>
    public SkinEngine.Fonts.Font.Align Align
    {
      get { return _align; }
      set { _align = value; }
    }

    /// <summary>
    /// Gets or sets the keep aspect ratio property.
    /// </summary>
    /// <value>The keep aspect ratio property.</value>
    public Property ZoomProperty
    {
      get { return _zoom; }
      set { _zoom = value; }
    }
    /// <summary>
    /// gets/sets a value indicating if we should use a thumbnail for the source
    /// or the original image
    /// </summary>
    public bool UseThumbNail
    {
      get { return (bool)_useThumbNail.GetValue(); }
      set { _useThumbNail.SetValue(value); }
    }


    /// <summary>
    /// Gets or sets the image source property.
    /// </summary>
    /// <value>The source.</value>
    public Property SourceProperty
    {
      get { return _source; }
      set { _source = value; }
    }

    /// <summary>
    /// Gets or sets the image source.
    /// </summary>
    /// <value>The source.</value>
    public string Source
    {
      get
      {
        if (_source == null) return "";
        if (_source.GetValue() == null) return "";
        return (string)_source.GetValue();
      }
      set { _source.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the effect.
    /// </summary>
    /// <value>The effect.</value>
    public string Effect
    {
      get { return (string)_effect.GetValue(); }
      set { _effect.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the effect property.
    /// </summary>
    /// <value>The effect property.</value>
    public Property EffectProperty
    {
      get { return _effect; }
      set { _effect = value; }
    }

    /// <summary>
    /// Gets or sets the default image property.
    /// </summary>
    /// <value>The default image property.</value>
    public Property DefaultProperty
    {
      get { return _defaultTexture; }
      set { _defaultTexture = value; }
    }

    /// <summary>
    /// Gets or sets the default image.
    /// </summary>
    /// <value>The default image.</value>
    public string Default
    {
      get
      {
        if (_defaultTexture == null) return "";
        if (_defaultTexture.GetValue() == null) return "";
        return (string)_defaultTexture.GetValue();
      }
      set { _defaultTexture.SetValue(value); }
    }

    /// <summary>
    /// Renders the image
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public override void Render(uint timePassed)
    {
      if (!IsVisible)
      {
        if (!IsAnimating)
        {
          return;
        }
      }
      string texture = Source;
      if (texture == null)
      {
        return;
      }
      if (texture.StartsWith("#"))
      {
        if (texture.StartsWith("#video") && texture.Length > "#video".Length)
        {
          int index = Int32.Parse(texture.Substring("#video".Length)) - 1;
          GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix;
          PlayerCollection collection = ServiceScope.Get<PlayerCollection>();
          if (index >= collection.Count)
          {
            return;
          }
          Vector4 mask = AlphaMask;
          mask.X *= SkinContext.FinalMatrix.Alpha.X;
          mask.Y *= SkinContext.FinalMatrix.Alpha.Y;
          mask.W *= SkinContext.FinalMatrix.Alpha.W;
          mask.Z *= SkinContext.FinalMatrix.Alpha.Z;
          //VideoPlayer vp = collection[index] as VideoPlayer;
          //if (vp != null)
          //  vp.Effect = null;

          //if (Effect.Length != 0 && GraphicsDevice.SupportsShaders)
          //{
          //  EffectAsset effect = ContentManager.GetEffect(Effect);
          //  if (effect != null)
          //  {
          //    if (vp != null)
          //      vp.Effect = effect;
          //  }
          //}
          collection[index].Position = new Point((int)Position.X, (int)Position.Y);
          collection[index].Size = new Size((int)Width, (int)Height);
          collection[index].AlphaMask =
            new Rectangle((int)(mask.X * 255.0f), (int)(mask.Y * 255.0f), (int)(mask.W * 255.0f), (int)(mask.Z * 255.0f));
          collection[index].Render();
          //          if (collection.Count > index)
          //          {
          //            if (vp != null)
          //              vp.Effect = null;
          //          }
          return;
        }
        else
        {
          string controlName = texture.Substring(1);
          Control control = Window.GetControlByName(controlName);
          if (control != null)
          {
            //fx=m11, fy=m22, fz=m33
            //x =m41, y =m42, m =m43 
            SkinContext.RemoveTransform();
            Vector3 finalScale =
              new Vector3(SkinContext.FinalMatrix.Matrix.M11, SkinContext.FinalMatrix.Matrix.M22,
                          SkinContext.FinalMatrix.Matrix.M33);
            Vector3 finalTranslation =
              new Vector3(SkinContext.FinalMatrix.Matrix.M41, SkinContext.FinalMatrix.Matrix.M42,
                          SkinContext.FinalMatrix.Matrix.M43);

            float w = control.Width;
            float h = control.Height;
            float scaleX = Width / w;
            float scaleY = Height / h;
            ExtendedMatrix m = new ExtendedMatrix();
            Vector3 controlPos = control.Position;
            controlPos.X *= finalScale.X;
            controlPos.Y *= finalScale.Y;
            controlPos.Z *= finalScale.Z;
            controlPos.X += finalTranslation.X;
            controlPos.Y += finalTranslation.Y;
            controlPos.Z += finalTranslation.Z;

            Vector3 thisPos = Position;
            thisPos.X *= finalScale.X;
            thisPos.Y *= finalScale.Y;
            thisPos.Z *= finalScale.Z;
            thisPos.X += finalTranslation.X;
            thisPos.Y += finalTranslation.Y;
            thisPos.Z += finalTranslation.Z;

            m.Matrix *= Matrix.Translation(new Vector3(-controlPos.X, -controlPos.Y, -controlPos.Z));
            m.Matrix *= Matrix.Scaling(new Vector3(scaleX, scaleY, 0));
            m.Matrix *= Matrix.Translation(thisPos);
            m.Alpha = AlphaMask;
            m.Translation = new Vector3(-controlPos.X + thisPos.X, -controlPos.Y + thisPos.Y, -controlPos.Z + thisPos.Z);

            Animations.Animate(timePassed, this, ref m);
            SkinContext.AddTransform(new ExtendedMatrix());

            SkinContext.TemporaryTransform = m;
            control.DoRender(timePassed);
            SkinContext.TemporaryTransform = null;
            return;
          }
        }
      }
      if (texture != _textureName)
      {
        _textureName = texture;
        if (_textureName.Length > 0)
        {
          if (_image == null)
          {
            _image = ContentManager.Load(_textureName, UseThumbNail);
            _image.Texture.UseThumbNail = UseThumbNail;
          }
          else
          {
            _image.SwitchTexture(_textureName, UseThumbNail);
            _image.Texture.UseThumbNail = UseThumbNail;
          }
        }
        else
        {
          _image = null;
        }
      }
      if (Default != null)
      {
        if (_image == null)
        {
          _image = ContentManager.Load(Default, UseThumbNail);
          _image.Texture.UseThumbNail = UseThumbNail;
        }
        else if (_image.Texture.Name != Default)
        {
          _image.Texture.UseThumbNail = UseThumbNail;
          if (_image.Texture.DoesExists == false)
          {
            _image.SwitchTexture(Default, UseThumbNail);
          }
        }
      }

      if (_image == null)
      {
        return;
      }
      Vector3 imgScale = new Vector3(1, 1, 1);
      if (Width > 0.0f)
      {
        if (_image.Texture.Width > 0)
        {
          imgScale.X = Width / ((float)_image.Texture.Width);
        }
      }
      if (Height > 0.0f)
      {
        if (_image.Texture.Height > 0)
        {
          imgScale.Y = Height / ((float)_image.Texture.Height);
        }
      }

      Vector3 pos = Position;
      float height = Height;
      float width = Width;
      float pixelRatio = 1.0f;
      float fSourceFrameRatio = ((float)_image.Texture.Width) / ((float)_image.Texture.Height);
      float fOutputFrameRatio = fSourceFrameRatio / pixelRatio;
      if (KeepAspectRatio)
      {

        if (!Zoom)
        {
          float fNewWidth = (float)width;
          float fNewHeight = fNewWidth / fOutputFrameRatio;

          // make sure the height is not larger than the maximum
          if (fNewHeight > Height)
          {
            fNewHeight = (float)_height;
            fNewWidth = fNewHeight * fOutputFrameRatio;
          }

          // this shouldnt happen, but just make sure that everything still fits onscreen
          if (fNewWidth > Width || fNewHeight > Height)
          {
            fNewWidth = (float)Width;
            fNewHeight = (float)Height;
          }

          width = fNewWidth;
          height = fNewHeight;

        }
      }
      //center image
      pos.X += ((((float)Width) - width) / 2.0f);
      pos.Y += ((((float)Height) - height) / 2.0f);

      float iSourceX = 0;
      float iSourceY = 0;
      float iSourceWidth = _image.Texture.Width;
      float iSourceHeight = _image.Texture.Height;
      if (Zoom && KeepAspectRatio)
      {
        float imageWidth = (float)_image.Texture.Width;
        float imageHeight = (float)_image.Texture.Height;
        // suggested by ziphnor
        //float fOutputFrameRatio = fSourceFrameRatio / pixelRatio;
        float fSourcePixelRatio = fSourceFrameRatio / ((float)imageWidth / (float)imageHeight);
        float fCroppedOutputFrameRatio = fSourcePixelRatio * ((float)imageWidth / (float)imageHeight) / pixelRatio;

        // calculate AR compensation (see http://www.iki.fi/znark/video/conversion)
        // assume that the movie is widescreen first, so use full height
        float fVertBorder = 0;
        float fNewHeight = (float)(height);
        float fNewWidth = fNewHeight * fOutputFrameRatio;
        float fHorzBorder = (fNewWidth - (float)width) / 2.0f;
        float fFactor = fNewWidth / ((float)imageWidth);
        fFactor *= pixelRatio;
        fHorzBorder = fHorzBorder / fFactor;

        if ((int)fNewWidth < width)
        {
          fHorzBorder = 0;
          fNewWidth = (float)(width);
          fNewHeight = fNewWidth / fOutputFrameRatio;
          fVertBorder = (fNewHeight - (float)height) / 2.0f;
          fFactor = fNewWidth / ((float)imageWidth);
          fFactor *= pixelRatio;
          fVertBorder = fVertBorder / fFactor;
        }
        iSourceX = fHorzBorder;
        iSourceY = fVertBorder;
        iSourceWidth = ((float)imageWidth - 2.0f * fHorzBorder);
        iSourceHeight = ((float)imageHeight - 2.0f * fVertBorder);
      }

      if (_align == SkinEngine.Fonts.Font.Align.Right)
      {
        if (Parent != null && Parent != this)
        {
          Control c = ((Control)Container);
          pos.X = c.Position.X + c.Width - (pos.X - c.Position.X + Width);
        }
        else if (Container != null && Container != this)
        {
          Control c = ((Control)Container);
          pos.X = c.Position.X + c.Width - (pos.X - c.Position.X + Width);
        }
      }
      // x-offset in texture
      float uoffs = ((float)(iSourceX)) / ((float)_image.Texture.Width);

      // y-offset in texture
      float voffs = ((float)iSourceY) / ((float)_image.Texture.Height);

      // width copied from texture
      float u = ((float)iSourceWidth) / ((float)_image.Texture.Width);

      // height copied from texture
      float v = ((float)iSourceHeight) / ((float)_image.Texture.Height);


      if (uoffs < 0 || uoffs > 1)
        uoffs = 0;
      if (u < 0 || u > 1)
        u = 1;
      if (v < 0 || v > 1)
        v = 1;
      if (u + uoffs > 1)
      {
        uoffs = 0;
        u = 1;
      }
      Vector4 alpha = AlphaMask;
      GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix;
      if (SkinContext.TemporaryTransform != null)
      {
        GraphicsDevice.Device.Transform.World *= SkinContext.TemporaryTransform.Matrix;
        alpha.X *= SkinContext.TemporaryTransform.Alpha.X;
        alpha.W *= SkinContext.TemporaryTransform.Alpha.W;
        alpha.Z *= SkinContext.TemporaryTransform.Alpha.Z;
        alpha.Y *= SkinContext.TemporaryTransform.Alpha.Y;
      }
      //_image.Draw(Position.X, Position.Y, Width, Height, SkinContext.FinalMatrix.Alpha);
      if (Effect.Length == 0 || (GraphicsDevice.SupportsShaders == false))
      {
        _image.Draw(pos.X, pos.Y, pos.Z, width, height, uoffs, voffs, u, v,
                    SkinContext.FinalMatrix.Alpha.X * alpha.X,
                    SkinContext.FinalMatrix.Alpha.W * alpha.W,
                    SkinContext.FinalMatrix.Alpha.Z * alpha.Z,
                    SkinContext.FinalMatrix.Alpha.Y * alpha.Y);
      }
      else
      {
        EffectAsset effect = ContentManager.GetEffect(Effect);
        if (effect != null)
        {
          _image.Draw(pos.X, pos.Y, pos.Z, width, height, uoffs, voffs, u, v,
                      SkinContext.FinalMatrix.Alpha.X * alpha.X,
                      SkinContext.FinalMatrix.Alpha.W * alpha.W,
                      SkinContext.FinalMatrix.Alpha.Z * alpha.Z,
                      SkinContext.FinalMatrix.Alpha.Y * alpha.Y, effect);
        }
      }

      if (_image.Texture.Name != _textureName)
      {
        _image.SwitchTexture(_textureName, UseThumbNail);
      }
    }
  }
}
