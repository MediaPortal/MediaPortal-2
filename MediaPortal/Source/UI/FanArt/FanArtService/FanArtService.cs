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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces.Providers;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  public class FanArtService : IFanArtService
  {
    protected readonly object _syncObj = new object();
    protected IList<IFanArtProvider> _providerList = null;
    protected IPluginItemStateTracker _fanartProviderPluginItemStateTracker;

    public void InitProviders()
    {
      lock (_syncObj)
      {
        if (_providerList != null)
          return;
        _providerList = new List<IFanArtProvider>();

        _fanartProviderPluginItemStateTracker = new FixedItemStateTracker("Fanart Service - Provider registration");

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(FanartProviderBuilder.FANART_PROVIDER_PATH))
        {
          try
          {
            FanartProviderRegistration fanartProviderRegistration = pluginManager.RequestPluginItem<FanartProviderRegistration>(FanartProviderBuilder.FANART_PROVIDER_PATH, itemMetadata.Id, _fanartProviderPluginItemStateTracker);
            if (fanartProviderRegistration == null)
              ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Fanart provider with id '{0}'", itemMetadata.Id);
            else
            {
              IFanArtProvider provider = Activator.CreateInstance(fanartProviderRegistration.ProviderClass) as IFanArtProvider;
              if (provider == null)
                throw new PluginInvalidStateException("Could not create IFanArtProvider instance of class {0}", fanartProviderRegistration.ProviderClass);
              _providerList.Add(provider);
              ServiceRegistration.Get<ILogger>().Info("Successfully activated Fanart provider '{0}' (Id '{1}')", itemMetadata.Attributes["ClassName"], itemMetadata.Id);
            }
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add IFanArtProvider extension with id '{0}'", e, itemMetadata.Id);
          }
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

    public IList<FanArtImage> GetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom)
    {
      InitProviders();
      foreach (FanArtProviderSource source in Enum.GetValues(typeof(FanArtProviderSource)))
      {
        foreach (IFanArtProvider fanArtProvider in _providerList.Where(p => p.Source == source))
        {
          IList<IResourceLocator> fanArtImages;
          if (fanArtProvider.TryGetFanArt(mediaType, fanArtType, name, maxWidth, maxHeight, singleRandom, out fanArtImages))
          {
            IList<IResourceLocator> result = singleRandom ? GetSingleRandom(fanArtImages) : fanArtImages;
            return result.Select(f => FanArtImage.FromResource(f, maxWidth, maxHeight)).Where(fanArtImage => fanArtImage != null).ToList();
          }
        }
        foreach (IBinaryFanArtProvider binaryProvider in _providerList.OfType<IBinaryFanArtProvider>().Where(p => p.Source == source))
        {
          if (binaryProvider != null)
          {
            IList<FanArtImage> binaryImages;
            if (binaryProvider.TryGetFanArt(mediaType, fanArtType, name, maxWidth, maxHeight, singleRandom, out binaryImages))
              return singleRandom ? GetSingleRandom(binaryImages) : binaryImages;
          }
        }
      }
      return null;
    }

    protected IList<TE> GetSingleRandom<TE>(IList<TE> fullList)
    {
      if (fullList.Count <= 1)
        return fullList;

      Random rnd = new Random(DateTime.Now.Millisecond);
      int rndIndex = rnd.Next(fullList.Count - 1);
      return new List<TE> { fullList[rndIndex] };
    }
  }
}
