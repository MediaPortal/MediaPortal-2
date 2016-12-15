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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class ImageBrush : TileBrush
  {
    #region Protected fields

    protected AbstractProperty _imageSourceProperty;
    protected AbstractProperty _downloadProgressProperty;
    protected AbstractProperty _thumbnailProperty;
    protected TextureAsset _tex;

    #endregion

    #region Ctor

    public ImageBrush()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      Free();
    }

    void Init()
    {
      _imageSourceProperty = new SProperty(typeof(string), null);
      _downloadProgressProperty = new SProperty(typeof(double), 0.0);
      _thumbnailProperty = new SProperty(typeof(bool), false);
    }

    void Attach()
    {
      _imageSourceProperty.Attach(OnPropertyChanged);
      _downloadProgressProperty.Attach(OnPropertyChanged);
      _thumbnailProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _imageSourceProperty.Detach(OnPropertyChanged);
      _downloadProgressProperty.Detach(OnPropertyChanged);
      _thumbnailProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ImageBrush b = (ImageBrush) source;
      ImageSource = b.ImageSource;
      DownloadProgress = b.DownloadProgress;
      Thumbnail = b.Thumbnail;
      _tex = null;
      Attach();
    }

    #endregion

    #region Public properties

    public AbstractProperty ImageSourceProperty
    {
      get { return _imageSourceProperty; }
    }

    public string ImageSource
    {
      get { return (string) _imageSourceProperty.GetValue(); }
      set { _imageSourceProperty.SetValue(value); }
    }

    public AbstractProperty DownloadProgressProperty
    {
      get { return _downloadProgressProperty; }
    }

    public double DownloadProgress
    {
      get { return (double) _downloadProgressProperty.GetValue(); }
      set { _downloadProgressProperty.SetValue(value); }
    }

    public override Texture Texture
    {
      get { return (_tex == null) ? null : _tex.Texture; }
    }

    public AbstractProperty ThumbnailProperty
    {
      get { return _thumbnailProperty; }
    }

    public bool Thumbnail
    {
      get { return (bool) _thumbnailProperty.GetValue(); }
      set { _thumbnailProperty.SetValue(value); }
    }

    #endregion

    #region Protected methods

    protected override Vector2 TextureMaxUV
    {
      get { return (_tex == null || !_tex.IsAllocated) ? new Vector2(1.0f, 1.0f) : new Vector2(_tex.MaxU, _tex.MaxV); }
    }

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      Free();
      base.OnPropertyChanged(prop, oldValue);
    }

    protected override Vector2 BrushDimensions
    {
      get { return _tex == null ? base.BrushDimensions : new Vector2(_tex.Width, _tex.Height); }
    }

    #endregion

    #region Public methods

    public void Free()
    {
      _tex = null;
    }

    public override void Allocate()
    {
      if (_tex == null && !string.IsNullOrEmpty(ImageSource))
        _tex = ContentManager.Instance.GetTexture(ImageSource, Thumbnail);
      if (_tex != null && !_tex.IsAllocated)
        _tex.Allocate();
    }

    public override void SetupBrush(FrameworkElement parent, ref PositionColoredTextured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      Allocate();
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);
    }

    protected override bool BeginRenderBrushOverride(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    {
      Allocate();
      if (_tex != null)
        _tex.Bind(0);
      return base.BeginRenderBrushOverride(primitiveContext, renderContext);
    }

    #endregion
  }
}
