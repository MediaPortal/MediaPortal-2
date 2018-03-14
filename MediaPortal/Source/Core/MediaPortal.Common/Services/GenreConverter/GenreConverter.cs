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
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;

namespace MediaPortal.Common.Services.GenreConverter
{
  /// <summary>
  /// Represents a genre converter service, which converts genre names to genre id and vice versa.
  /// </summary>
  public class GenreConverter : IGenreConverter, IDisposable
  {
    protected List<SortedGenreConverter> _providerList = null;
    protected IPluginItemStateTracker _genreProviderPluginItemStateTracker;
    protected readonly object _syncObj = new object();

    #region Internal class
    protected class SortedGenreConverter : IDisposable
    {
      public int Priority;
      public IGenreProvider Provider;
      public void Dispose()
      {
        IDisposable disp = Provider as IDisposable;
        if (disp != null)
          disp.Dispose();
      }
    }
    #endregion

    public void InitProviders()
    {
      lock (_syncObj)
      {
        if (_providerList != null)
          return;

        var providerList = new List<SortedGenreConverter>();

        _genreProviderPluginItemStateTracker = new FixedItemStateTracker("GenreConverter Service - Provider registration");

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(GenreProviderBuilder.GENRE_PROVIDER_PATH))
        {
          try
          {
            GenreProviderRegistration genreProviderRegistration = pluginManager.RequestPluginItem<GenreProviderRegistration>(GenreProviderBuilder.GENRE_PROVIDER_PATH, itemMetadata.Id, _genreProviderPluginItemStateTracker);
            if (genreProviderRegistration == null || genreProviderRegistration.ProviderClass == null)
              ServiceRegistration.Get<ILogger>().Warn("Could not instantiate IGenreProvider with id '{0}'", itemMetadata.Id);
            else
            {
              IGenreProvider provider = Activator.CreateInstance(genreProviderRegistration.ProviderClass) as IGenreProvider;
              if (provider == null)
                throw new PluginInvalidStateException("Could not create IGenreProvider instance of class {0}", genreProviderRegistration.ProviderClass);
              providerList.Add(new SortedGenreConverter { Priority = genreProviderRegistration.Priority, Provider = provider });
            }
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add IGenreProvider with id '{0}'", e, itemMetadata.Id);
          }
        }
        providerList.Sort((p1, p2) => p1.Priority.CompareTo(p2.Priority));
        _providerList = providerList;
      }
    }

    public void Dispose()
    {
      if (_providerList != null)
        foreach (IDisposable result in _providerList.OfType<IDisposable>())
          result.Dispose();
    }

    #region IGenreGenerator implementation

    public bool GetGenreId(string genreName, string genreCategory, string genreCulture, out int genreId)
    {
      InitProviders();
      genreId = 0;
      foreach (var genreProvider in _providerList)
      {
        try
        {
          if (genreProvider.Provider.GetGenreId(genreName, genreCategory, genreCulture, out genreId))
            return true;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error converting to genre id using provider '{0}", ex, genreProvider.GetType().Name);
        }
      }
      return false;
    }

    public bool GetGenreName(int genreId, string genreCategory, string genreCulture, out string genreName)
    {
      InitProviders();
      genreName = null;
      foreach (var genreProvider in _providerList)
      {
        try
        {
          if (genreProvider.Provider.GetGenreName(genreId, genreCategory, genreCulture, out genreName))
            return true;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error converting to genre name using provider '{0}", ex, genreProvider.GetType().Name);
        }
      }
      return false;
    }

    public bool GetGenreType(int genreId, string genreCategory, out string genreType)
    {
      InitProviders();
      genreType = null;
      foreach (var genreProvider in _providerList)
      {
        try
        {
          if (genreProvider.Provider.GetGenreType(genreId, genreCategory, out genreType))
            return true;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error converting to genre type using provider '{0}", ex, genreProvider.GetType().Name);
        }
      }
      return false;
    }

    #endregion
  }
}
