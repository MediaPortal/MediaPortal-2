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

using System;
using System.Net;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.General;
using MediaPortal.Common.Network;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client
{
  public class FanArtImageSource : MultiImageSource
  {
    #region Protected fields

    protected static string _baseUrl;

    protected AbstractProperty _fanArtMediaTypeProperty;
    protected AbstractProperty _fanArtTypeProperty;
    protected AbstractProperty _fanArtNameProperty;
    protected AbstractProperty _maxWidthProperty;
    protected AbstractProperty _maxHeightProperty;
    protected AbstractProperty _cacheProperty;

    #endregion

    #region Properties

    public string FanArtMediaType
    {
      get { return (string)_fanArtMediaTypeProperty.GetValue(); }
      set { _fanArtMediaTypeProperty.SetValue(value); }
    }

    public AbstractProperty FanArtMediaTypeProperty
    {
      get { return _fanArtMediaTypeProperty; }
    }

    public string FanArtType
    {
      get { return (string)_fanArtTypeProperty.GetValue(); }
      set { _fanArtTypeProperty.SetValue(value); }
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

    public int MaxWidth
    {
      get { return (int)_maxWidthProperty.GetValue(); }
      set { _maxWidthProperty.SetValue(value); }
    }

    public AbstractProperty MaxWidthProperty
    {
      get { return _maxWidthProperty; }
    }

    public int MaxHeight
    {
      get { return (int)_maxHeightProperty.GetValue(); }
      set { _maxHeightProperty.SetValue(value); }
    }

    public AbstractProperty MaxHeightProperty
    {
      get { return _maxHeightProperty; }
    }

    /// <summary>
    /// Indicates if an image can be cached by the ContentManger. By default this property is set to <c>true</c>. It should be only modified
    /// in skins, if it is intended to get new results for same request (<see cref="IFanArtService.GetFanArt"/> usually returns a single random file
    /// that matches the query).
    /// </summary>
    public bool Cache
    {
      get { return (bool)_cacheProperty.GetValue(); }
      set { _cacheProperty.SetValue(value); }
    }

    public AbstractProperty CacheProperty
    {
      get { return _cacheProperty; }
    }

    #endregion

    #region Constructor

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
      FanArtImageSource fanArtImageSource = (FanArtImageSource)source;
      FanArtType = fanArtImageSource.FanArtType;
      FanArtMediaType = fanArtImageSource.FanArtMediaType;
      FanArtName = fanArtImageSource.FanArtName;
      MaxWidth = fanArtImageSource.MaxWidth;
      MaxHeight = fanArtImageSource.MaxHeight;
      Cache = fanArtImageSource.Cache;
      Attach();
    }

    protected void Init()
    {
      _fanArtMediaTypeProperty = new SProperty(typeof(string), FanArtMediaTypes.Undefined);
      _fanArtTypeProperty = new SProperty(typeof(string), FanArtTypes.Undefined);
      _fanArtNameProperty = new SProperty(typeof(string), string.Empty);
      _maxWidthProperty = new SProperty(typeof(int), 0);
      _maxHeightProperty = new SProperty(typeof(int), 0);
      _cacheProperty = new SProperty(typeof(bool), true);
    }

    protected void Attach()
    {
      _fanArtMediaTypeProperty.Attach(UpdateSource);
      _fanArtTypeProperty.Attach(UpdateSource);
      _fanArtNameProperty.Attach(UpdateSource);
      _cacheProperty.Attach(UpdateSource);
    }

    protected void Detach()
    {
      _fanArtMediaTypeProperty.Detach(UpdateSource);
      _fanArtTypeProperty.Detach(UpdateSource);
      _fanArtNameProperty.Detach(UpdateSource);
      _cacheProperty.Detach(UpdateSource);
    }

    protected bool BuildBaseUrl()
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      // In case we lost the connection clear the url so it can be looked up later again
      if (!scm.IsHomeServerConnected)
      {
        _baseUrl = null;
        return false;
      }
      if (!string.IsNullOrWhiteSpace(_baseUrl))
        return true;

      // We need to know the base url of the server's remote access service, so we can use the IP and port number.
      try
      {
        IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
        string resourceUrl;
        IPAddress localIpAddress;
        if (!rris.GetFileHttpUrl(scm.HomeServerSystemId, ResourcePath.BuildBaseProviderPath(Guid.Empty, string.Empty), out resourceUrl, out localIpAddress))
          return false;

        Uri uri = new Uri(resourceUrl);
        _baseUrl = uri.Authority;
        return true;
      }
      catch
      {
        return false;
      }
    }

    protected void UpdateSource(AbstractProperty property, object oldvalue)
    {
      if (!BuildBaseUrl() || !CheckValidArgs())
        return;

      string cacheHint = Cache ? "" : "&NoCache=" + DateTime.Now.Ticks;
      UriSource = string.Format("http://{0}/FanartService?mediatype={1}&fanarttype={2}&name={3}&width={4}&height={5}{6}", _baseUrl, FanArtMediaType, FanArtType, FanArtName.Encode(), MaxWidth, MaxHeight, cacheHint);
    }

    protected bool CheckValidArgs()
    {
      return (
        FanArtMediaType != FanArtMediaTypes.Undefined && FanArtType != FanArtTypes.Undefined ||
        FanArtMediaType == FanArtMediaTypes.Undefined && FanArtType == FanArtTypes.Thumbnail /* Special case for all MediaItem thumbs */
        )
        && !string.IsNullOrWhiteSpace(FanArtName);
    }
  }
}
