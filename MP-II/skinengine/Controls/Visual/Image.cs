using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;
using SkinEngine;
using Microsoft.DirectX;

namespace SkinEngine.Controls.Visuals
{
  public enum StretchDirection { UpOnly, DownOnly, Both };

  public enum Stretch
  {
    //The content preserves its original size.
    None,

    //The content is resized to fill the destination dimensions. The aspect ratio is not preserved.
    Fill,

    //The content is resized to fit in the destination dimensions while it preserves its native aspect ratio.
    Uniform,

    //The content is resized to fill the destination dimensions while it preserves its native aspect ratio. 
    //If the aspect ratio of the destination rectangle differs from the source, the source content is 
    //clipped to fit in the destination dimensions (zoom-in)
    UniformToFill
  };

  public class Image : FrameworkElement
  {
    Property _imageSourceProperty;
    Property _stretchDirectionProperty;
    Property _stretchProperty;
    Property _opacityProperty;
    private VertextBufferAsset _image;
    bool _performLayout = false;
    float _u, _v, _uoff, _voff, _w, _h;
    Vector3 _pos;

    public Image()
    {
      Init();
    }
    public Image(Image img)
      : base((FrameworkElement)img)
    {
      Init();
      ImageSource = img.ImageSource;
      Stretch = img.Stretch;
      StretchDirection = img.StretchDirection;
      Opacity = img.Opacity;
    }
    void Init()
    {
      _imageSourceProperty = new Property(null);
      _stretchDirectionProperty = new Property(StretchDirection.Both);
      _stretchProperty = new Property(Stretch.Fill);
      _opacityProperty = new Property((double)1.0f);
      _imageSourceProperty.Attach(new PropertyChangedHandler(OnImageChanged));
      _stretchProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _stretchDirectionProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }
    public override object Clone()
    {
      return new Image(this);
    }
    /// <summary>
    /// Called when a property changed. 
    /// Simply sets a variable to indicate a layout needs to be performed
    /// </summary>
    /// <param name="property">The property.</param>
    void OnPropertyChanged(Property property)
    {
      _performLayout = true;
    }
    /// <summary>
    /// Called when the imagesource has been changed
    /// Simply invalidates the image, the renderer will automaticly create a new one
    /// with the new imagesource
    /// </summary>
    /// <param name="property">The property.</param>
    void OnImageChanged(Property property)
    {
      _image = null;
    }

    /// <summary>
    /// Gets or sets the image source property.
    /// </summary>
    /// <value>The image source property.</value>
    public Property ImageSourceProperty
    {
      get
      {
        return _imageSourceProperty;
      }
      set
      {
        _imageSourceProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the image source.
    /// </summary>
    /// <value>The image source.</value>
    public string ImageSource
    {
      get
      {
        return (string)_imageSourceProperty.GetValue();
      }
      set
      {
        _imageSourceProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the opacity property.
    /// </summary>
    /// <value>The opacity property.</value>
    public Property OpacityProperty
    {
      get
      {
        return _opacityProperty;
      }
      set
      {
        _opacityProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the opacity.
    /// </summary>
    /// <value>The opacity.</value>
    public double Opacity
    {
      get
      {
        return (double)_opacityProperty.GetValue();
      }
      set
      {
        _opacityProperty.SetValue(value);
      }
    }



    /// <summary>
    /// Gets or sets the stretch direction property.
    /// </summary>
    /// <value>The stretch direction property.</value>
    public Property StretchDirectionProperty
    {
      get
      {
        return _stretchDirectionProperty;
      }
      set
      {
        _stretchDirectionProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the stretch direction.
    /// </summary>
    /// <value>The stretch direction.</value>
    public StretchDirection StretchDirection
    {
      get
      {
        return (StretchDirection)_stretchDirectionProperty.GetValue();
      }
      set
      {
        _stretchDirectionProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the stretch property.
    /// </summary>
    /// <value>The stretch property.</value>
    public Property StretchProperty
    {
      get
      {
        return _stretchProperty;
      }
      set
      {
        _stretchProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the stretch.
    /// </summary>
    /// <value>The stretch.</value>
    public Stretch Stretch
    {
      get
      {
        return (Stretch)_stretchProperty.GetValue();
      }
      set
      {
        _stretchProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.Rectangle finalRect)
    {
      finalRect.X += (int)(Margin.X);
      finalRect.Y += (int)(Margin.Y);
      finalRect.Width -= (int)(Margin.X);
      finalRect.Height -= (int)(Margin.Y);
      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, 1.0f); ;
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      PerformLayout();
      base.Arrange(finalRect);
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(Size availableSize)
    {
      base.Measure(availableSize);
      if (ImageSource == null)
      {
        _desiredSize = new Size((int)Width, (int)Height);
        return;
      }
      if (_image != null && _image.Texture.IsAllocated)
      {
        int w = (int)Width;
        int h = (int)Height;
        if (Stretch == Stretch.Uniform)
        {
          if (w == 0) w = _image.Texture.Width;
          if (h == 0) h = _image.Texture.Height;
        }
        else
        {
          if (w == 0) w = availableSize.Width;
          if (h == 0) h = availableSize.Height;
        }
        _desiredSize = new Size(w, h);
      }
      else
      {
        _desiredSize = new Size((int)Width, (int)Height);
      }
      _desiredSize.Width += (int)(Margin.X + Margin.W);
      _desiredSize.Height += (int)(Margin.Y + Margin.Z);
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      if (_image == null && ImageSource != null)
      {
        _image = ContentManager.Load(ImageSource, true);
        _performLayout = true;
      }
      if (_image == null) return;

      if (_performLayout && _image.Texture.IsAllocated)
      {
        _performLayout = false;
        if (Width == 0 || Height == 0)
        {
          Invalidate();
        }
        else
        {
          PerformLayout();
        }
      }

      GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix;
      _image.Draw(_pos.X, _pos.Y, _pos.Z, _w, _h, _uoff, _voff, _u, _v, (float)Opacity, (float)Opacity, (float)Opacity, (float)Opacity);
    }

    /// <summary>
    /// Performs the layout.
    /// </summary>
    void PerformLayout()
    {
      if (_image == null) return;
      if (!_image.Texture.IsAllocated) return;

      Vector3 imgScale = new Vector3(1, 1, 1);
      if (Width > 0.0f)
      {
        if (_image.Texture.Width > 0)
        {
          imgScale.X = (float)(ActualWidth / ((float)_image.Texture.Width));
        }
      }
      if (Height > 0.0f)
      {
        if (_image.Texture.Height > 0)
        {
          imgScale.Y = (float)(ActualHeight / ((float)_image.Texture.Height));
        }
      }

      Vector3 pos = ActualPosition;
      float height = (float)ActualHeight;
      float width = (float)ActualWidth;
      float pixelRatio = 1.0f;
      float fSourceFrameRatio = ((float)_image.Texture.Width) / ((float)_image.Texture.Height);
      float fOutputFrameRatio = fSourceFrameRatio / pixelRatio;
      if (Stretch == Stretch.Uniform || Stretch == Stretch.UniformToFill)
      {

        if (Stretch == Stretch.Uniform)
        {
          float fNewWidth = (float)width;
          float fNewHeight = fNewWidth / fOutputFrameRatio;

          // make sure the height is not larger than the maximum
          if (fNewHeight > ActualHeight)
          {
            fNewHeight = (float)height;
            fNewWidth = fNewHeight * fOutputFrameRatio;
          }

          // this shouldnt happen, but just make sure that everything still fits onscreen
          if (fNewWidth > ActualWidth || fNewHeight > ActualHeight)
          {
            fNewWidth = (float)ActualWidth;
            fNewHeight = (float)ActualHeight;
          }

          width = fNewWidth;
          height = fNewHeight;

        }
      }

      //center image
      pos.X += ((((float)ActualWidth) - width) / 2.0f);
      pos.Y += ((((float)ActualHeight) - height) / 2.0f);

      float iSourceX = 0;
      float iSourceY = 0;
      float iSourceWidth = _image.Texture.Width;
      float iSourceHeight = _image.Texture.Height;
      if (Stretch == Stretch.UniformToFill)
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

      _u = u;
      _v = v;
      _uoff = uoffs;
      _voff = voffs;
      _w = width;
      _h = height;
      _pos = pos;
    }
  }
}
