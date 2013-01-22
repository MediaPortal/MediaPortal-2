﻿#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces.Providers;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  public class FanArtService : IFanArtService
  {
    protected readonly object _syncObj = new object();
    protected IList<IFanArtProvider> _providerList = null;
    protected IPluginItemStateTracker _fanartProviderPluginItemStateTracker;

    private void BuildProviders()
    {
      lock (_syncObj)
      {
        if (_providerList != null)
          return;
        _providerList = new List<IFanArtProvider>();
      }

      _fanartProviderPluginItemStateTracker = new FixedItemStateTracker("Fanart Service - Provider registration");

      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(FanartProviderBuilder.FANART_PROVIDER_PATH))
      {
        try
        {
          FanartProviderRegistration fanartProviderRegistration =
            pluginManager.RequestPluginItem<FanartProviderRegistration>(FanartProviderBuilder.FANART_PROVIDER_PATH, itemMetadata.Id, _fanartProviderPluginItemStateTracker);

          if (fanartProviderRegistration == null)
            ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Fanart provider with id '{0}'", itemMetadata.Id);
          else
          {
            IFanArtProvider provider = Activator.CreateInstance(fanartProviderRegistration.ProviderClass) as IFanArtProvider;
            if (provider == null)
              throw new PluginInvalidStateException("Could not create IFanArtProvider instance of class {0}", fanartProviderRegistration.ProviderClass);
            _providerList.Add(provider);
          }
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot add IFanArtProvider extension with id '{0}'", e, itemMetadata.Id);
        }
      }
    }

    /// <summary>
    /// Gets the list of all registered <see cref="IFanArtProvider"/>.
    /// </summary>
    public IList<IFanArtProvider> Providers
    {
      get { return _providerList; }
    }

    public IList<FanArtImage> GetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom)
    {
      BuildProviders();
      foreach (IFanArtProvider fanArtProvider in _providerList)
      {
        IList<string> fanArtImages = fanArtProvider.GetFanArt(mediaType, fanArtType, name, maxWidth, maxHeight, singleRandom);
        if (fanArtImages != null)
        {
          IList<string> result = singleRandom ? GetSingleRandom(fanArtImages) : fanArtImages;
          return result.Select(f => FanArtImage.FromFile(f, maxWidth, maxHeight)).Where(fanArtImage => fanArtImage != null).ToList();
        }
      }
      return null;
    }

    protected IList<string> GetSingleRandom(IList<string> fullList)
    {
      if (fullList.Count <= 1)
        return fullList;

      Random rnd = new Random(DateTime.Now.Millisecond);
      int rndIndex = rnd.Next(fullList.Count - 1);
      return new List<string> { fullList[rndIndex] };
    }

    protected string GetPattern(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name)
    {
      switch (mediaType)
      {
       case FanArtConstants.FanArtMediaType.Channel:
          return string.Format("{0}.png", name);
      }
      return null;
    }

    protected string GetBaseFolder(FanArtConstants.FanArtMediaType mediaType, string name)
    {
      switch (mediaType)
      {
    
        case FanArtConstants.FanArtMediaType.Channel:
          return @"Plugins\SlimTv.Service\Content\ChannelLogos";

        default:
          return null;
      }
    }
  }
}
