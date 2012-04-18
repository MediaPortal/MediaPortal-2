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
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.Utilities.DeepCopy;
using SlimDX.Direct3D9;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class FanArtImageSource : TextureImageSource
  {
    #region Protected fields

    protected string _source = @"D:\Coding\MP\MP2\Series\TvdbLib\Cache\79334\img_graphical_79334-g4.jpg";
    protected TextureAsset _texture;
    protected AbstractProperty _fanArtMediaTypeProperty;
    protected AbstractProperty _fanArtTypeProperty;
    protected AbstractProperty _fanArtNameProperty;

    #endregion

    #region Properties

    public FanArtConstants.FanArtMediaType FanArtMediaType
    {
      get { return (FanArtConstants.FanArtMediaType) _fanArtMediaTypeProperty.GetValue(); }
      set { _fanArtMediaTypeProperty.SetValue(value);}
    }
    public AbstractProperty FanArtMediaTypeProperty
    {
      get { return _fanArtMediaTypeProperty; }
    }

    public FanArtConstants.FanArtType FanArtType
    {
      get { return (FanArtConstants.FanArtType)_fanArtTypeProperty.GetValue(); }
      set { _fanArtTypeProperty.SetValue(value);}
    }
    public AbstractProperty FanArtTypeProperty
    {
      get { return _fanArtTypeProperty; }
    }    

    public string FanArtName
    {
      get { return (string)_fanArtNameProperty.GetValue(); }
      set { _fanArtNameProperty.SetValue(value); }
    }
    public AbstractProperty FanArtNameProperty
    {
      get { return _fanArtNameProperty; }
    }

    #endregion

    #region Constructor

    static FanArtImageSource()
    {
       ServiceRegistration.Set<IFanArtService>(new FanArtService());
    }
    /// <summary>
    /// Constructs a <see cref="FanArtImageSource"/>.
    /// </summary>
    public FanArtImageSource()
    {
      _fanArtMediaTypeProperty = new WProperty(typeof(FanArtConstants.FanArtMediaType), FanArtConstants.FanArtMediaType.Undefined);
      _fanArtTypeProperty = new WProperty(typeof(FanArtConstants.FanArtType), FanArtConstants.FanArtType.Undefined);
      _fanArtNameProperty = new WProperty(typeof(string), string.Empty);
    }

    #endregion

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      FanArtImageSource fanArtImageSource = (FanArtImageSource)source;
      FanArtType = fanArtImageSource.FanArtType;
      FanArtMediaType = fanArtImageSource.FanArtMediaType;
      FanArtName = fanArtImageSource.FanArtName;
      Attach();
    }

    protected void Attach()
    {
      
    }

    protected void Detach()
    {
      
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
      if (FanArtMediaType == FanArtConstants.FanArtMediaType.Undefined || FanArtType == FanArtConstants.FanArtType.Undefined)
        return;

      IFanArtService fanArtService = ServiceRegistration.Get<IFanArtService>();
      IList<string> possibleSources = fanArtService.GetFanArt(FanArtMediaType, FanArtType, FanArtName, true);
      if (possibleSources.Count == 0)
        return;

      if (_texture == null)
        _texture = ContentManager.Instance.GetTexture(possibleSources[0], false);
      
      if (_texture == null || _texture.IsAllocated) 
        return;
      
      _texture.Allocate();
      
      if (!_texture.IsAllocated) 
        return;
      
      _imageContext.Refresh();
      FireChanged();
    }

    #endregion

    #region Protected methods

    protected override void FreeData()
    {
      _texture = null;
      base.FreeData();
    }

    #endregion

  }
}
