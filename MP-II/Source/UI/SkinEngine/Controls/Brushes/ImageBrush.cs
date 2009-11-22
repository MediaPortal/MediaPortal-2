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

using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class ImageBrush : TileBrush
  {
    #region Private fields

    Property _imageSourceProperty;
    Property _downloadProgressProperty;
    TextureAsset _tex;

    #endregion

    #region Ctor

    public ImageBrush()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _imageSourceProperty = new Property(typeof(string), null);
      _downloadProgressProperty = new Property(typeof(double), 0.0);
    }

    void Attach()
    {
      _imageSourceProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _imageSourceProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ImageBrush b = (ImageBrush) source;
      ImageSource = copyManager.GetCopy(b.ImageSource);
      DownloadProgress = copyManager.GetCopy(b.DownloadProgress);
      Attach();
    }

    #endregion

    #region Public properties

    public Property ImageSourceProperty
    {
      get { return _imageSourceProperty; }
    }

    public string ImageSource
    {
      get { return (string)_imageSourceProperty.GetValue(); }
      set { _imageSourceProperty.SetValue(value); }
    }

    public Property DownloadProgressProperty
    {
      get { return _downloadProgressProperty; }
    }

    public double DownloadProgress
    {
      get { return (double)_downloadProgressProperty.GetValue(); }
      set { _downloadProgressProperty.SetValue(value); }
    }

    #endregion

    #region Protected methods

    protected override void OnPropertyChanged(Property prop, object oldValue)
    {
      Free();
    }

    protected override void Scale(ref float u, ref float v)
    {
      if (_tex == null) return;
      u *= _tex.MaxU;
      v *= _tex.MaxV;
    }

    protected override Vector2 BrushDimensions
    {
      get
      {
        if (_tex == null)
          return base.BrushDimensions;
        return new Vector2(_tex.Width, _tex.Height);
      }
    }

    #endregion

    #region Public methods

    public void Free()
    {
      _tex = null;
    }

    public override void Allocate()
    {
      _tex = ContentManager.GetTexture(ImageSource, true);
      _tex.Allocate();
    }

    public override void SetupBrush(RectangleF bounds, ExtendedMatrix layoutTransform, float zOrder, ref PositionColored2Textured[] verts)
    {
      if (_tex == null)
      {
        Allocate();
        base.SetupBrush(bounds, layoutTransform, zOrder, ref verts);
      }
    }

    public override bool BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {
      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      if (_tex == null)
      {
        Allocate();
      }
      _tex.Set(0);
      return true;
    }

    public override void EndRender()
    {
      //GraphicsDevice.Device.SetTexture(0, null);
    }

    #endregion
  }
}
