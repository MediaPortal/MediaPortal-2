#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.Utilities.DeepCopy;
using SlimDX.Direct3D9;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client
{
  public class FanArtImageSource : TextureImageSource
  {
    #region Protected fields

    protected TextureAsset _texture;
    protected AbstractProperty _fanArtMediaTypeProperty;
    protected AbstractProperty _fanArtTypeProperty;
    protected AbstractProperty _fanArtNameProperty;
    protected AbstractProperty _maxWidthProperty;
    protected AbstractProperty _maxHeightProperty;

    protected IList<FanArtImage> _possibleSources;
    protected bool _asyncStarted = false;

    protected readonly object _syncObj = new object();

    #endregion

    #region Properties

    public FanArtConstants.FanArtMediaType FanArtMediaType
    {
      get { return (FanArtConstants.FanArtMediaType) _fanArtMediaTypeProperty.GetValue(); }
      set { _fanArtMediaTypeProperty.SetValue(value); }
    }

    public AbstractProperty FanArtMediaTypeProperty
    {
      get { return _fanArtMediaTypeProperty; }
    }

    public FanArtConstants.FanArtType FanArtType
    {
      get { return (FanArtConstants.FanArtType) _fanArtTypeProperty.GetValue(); }
      set { _fanArtTypeProperty.SetValue(value); }
    }

    public AbstractProperty FanArtTypeProperty
    {
      get { return _fanArtTypeProperty; }
    }

    public string FanArtName
    {
      get { return (string) _fanArtNameProperty.GetValue(); }
      set { _fanArtNameProperty.SetValue(value); }
    }

    public AbstractProperty FanArtNameProperty
    {
      get { return _fanArtNameProperty; }
    }

    public int MaxWidth
    {
      get { return (int) _maxWidthProperty.GetValue(); }
      set { _maxWidthProperty.SetValue(value); }
    }

    public AbstractProperty MaxWidthProperty
    {
      get { return _maxWidthProperty; }
    }

    public int MaxHeight
    {
      get { return (int) _maxHeightProperty.GetValue(); }
      set { _maxHeightProperty.SetValue(value); }
    }

    public AbstractProperty MaxHeightProperty
    {
      get { return _maxHeightProperty; }
    }

    #endregion

    #region Constructor

    static FanArtImageSource()
    {
      FanArtServiceProxyRegistration.RegisterService();
    }
    /// <summary>
    /// Constructs a <see cref="FanArtImageSource"/>.
    /// </summary>
    public FanArtImageSource()
    {
      Init();
      Attach();
    }

    #endregion

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      FanArtImageSource fanArtImageSource = (FanArtImageSource) source;
      FanArtType = fanArtImageSource.FanArtType;
      FanArtMediaType = fanArtImageSource.FanArtMediaType;
      FanArtName = fanArtImageSource.FanArtName;
      MaxWidth = fanArtImageSource.MaxWidth;
      MaxHeight = fanArtImageSource.MaxHeight;
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      Deallocate();
    }

    protected void Init()
    {
      _fanArtMediaTypeProperty = new SProperty(typeof(FanArtConstants.FanArtMediaType),
        FanArtConstants.FanArtMediaType.Undefined);
      _fanArtTypeProperty = new SProperty(typeof(FanArtConstants.FanArtType), FanArtConstants.FanArtType.Undefined);
      _fanArtNameProperty = new SProperty(typeof(string), string.Empty);
      _maxWidthProperty = new SProperty(typeof(int), 0);
      _maxHeightProperty = new SProperty(typeof(int), 0);
    }

    protected void Attach()
    {
      FanArtMediaTypeProperty.Attach(UpdateSource);
      FanArtNameProperty.Attach(UpdateSource);
    }

    protected void Detach()
    {
      FanArtMediaTypeProperty.Detach(UpdateSource);
      FanArtNameProperty.Detach(UpdateSource);
    }

    private void UpdateSource(AbstractProperty property, object oldvalue)
    {
      FreeData();
      FireChanged();
    }

    #region ImageSource implementation

    public override bool IsAllocated
    {
      get { return _texture != null && _texture.IsAllocated; }
    }

    protected override Texture Texture
    {
      get { return _texture == null ? null : _texture.Texture; }
    }

    protected override SizeF RawSourceSize
    {
      get { return (_texture != null && _texture.IsAllocated) ? new SizeF(_texture.Width, _texture.Height) : SizeF.Empty; }
    }

    protected override RectangleF TextureClip
    {
      get { return _texture == null ? RectangleF.Empty : new RectangleF(0, 0, _texture.MaxU, _texture.MaxV); }
    }

    public override void Allocate()
    {
      if (FanArtMediaType == FanArtConstants.FanArtMediaType.Undefined ||
        FanArtType == FanArtConstants.FanArtType.Undefined)
        return;

      Download_Async();

      IList<FanArtImage> possibleSources;
      lock (_syncObj)
        possibleSources = _possibleSources;

      if (possibleSources == null || possibleSources.Count == 0)
        return;

      if (_texture == null)
      {
        FanArtImage image = possibleSources[0];
        _texture = ContentManager.Instance.GetTexture(image.BinaryData, image.Name);
      }

      if (_texture == null || _texture.IsAllocated)
        return;

      _texture.Allocate();

      if (!_texture.IsAllocated)
        return;

      _imageContext.Refresh();
      FireChanged();
    }

    public override void Deallocate()
    {
      base.Deallocate();
      _texture = null;
    }

    #endregion

    #region Protected methods

    protected void Download_Async()
    {
      if (!_asyncStarted)
      {
        IFanArtService fanArtService = ServiceRegistration.Get<IFanArtService>(false);
        if (fanArtService == null)
          return;

        FanArtConstants.FanArtMediaType mediaType = FanArtMediaType;
        FanArtConstants.FanArtType type = FanArtType;
        string name = FanArtName;
        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
        threadPool.Add(() =>
                         {
                           IList<FanArtImage> possibleSources = fanArtService.GetFanArt(mediaType, type, name, MaxWidth, MaxHeight, true);
                           lock (_syncObj)
                           {
                             // Selection can be changed meanwhile, so set source only if same as on starting
                             if (FanArtMediaType == mediaType && FanArtType == type && FanArtName == name)
                               _possibleSources = possibleSources;
                           }
                         });
        _asyncStarted = true;
      }
    }

    protected override void FreeData()
    {
      _possibleSources = null;
      _texture = null;
      _asyncStarted = false;
      base.FreeData();
    }

    #endregion

  }
}
