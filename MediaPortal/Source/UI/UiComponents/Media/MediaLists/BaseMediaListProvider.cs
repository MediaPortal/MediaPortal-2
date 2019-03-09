#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Extensions;
using MediaPortal.UiComponents.Media.Helpers;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.UserManagement;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public abstract class BaseMediaListProvider : IMediaListProvider
  {
    public delegate PlayableMediaItem PlayableMediaItemToListItemAction(MediaItem mediaItem);
    public delegate PlayableContainerMediaItem PlayableContainerMediaItemToListItemAction(MediaItem mediaItem);

    protected IList<MediaItem> _currentMediaItems;
    protected ItemsList _allItems;
    protected IEnumerable<Guid> _necessaryMias;
    protected IEnumerable<Guid> _optionalMias;
    protected PlayableMediaItemToListItemAction _playableConverterAction;
    protected PlayableContainerMediaItemToListItemAction _playableContainerConverterAction;

    protected object _navigationFilterSync = new object();
    protected FixedItemStateTracker _tracker;
    protected IDictionary<string, IList<IFilter>> _navigationFilters;
    protected Type _navigationInitializerType;

    public BaseMediaListProvider()
    {
      _allItems = new ItemsList();
    }

    public ItemsList AllItems
    {
      get { return _allItems; }
    }

    protected virtual bool ShouldUpdate(UpdateReason updateReason)
    {
      return updateReason.HasFlag(UpdateReason.Forced);
    }

    protected abstract Task<MediaItemQuery> CreateQueryAsync();

    public UserProfile CurrentUserProfile
    {
      get
      {
        IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
        if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        {
          return userProfileDataManagement.CurrentUser;
        }
        return null;
      }
    }

    public async Task<IFilter> AppendUserFilterAsync(IFilter filter, IEnumerable<Guid> filterMias)
    {
      IFilter userFilter = await CertificationHelper.GetUserCertificateFilter(filterMias);
      if (userFilter != null)
      {
        return filter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter) : userFilter;
      }
      return filter;
    }

    public virtual async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason)
    {
      if (!ShouldUpdate(updateReason))
        return false;

      if (_playableConverterAction == null && _playableContainerConverterAction == null)
        return false;

      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
        return false;

      MediaItemQuery query = await CreateQueryAsync();
      if (query == null)
        return false;
      query.Limit = (uint)maxItems;

      Guid? userProfile = CurrentUserProfile?.ProfileId;
      bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(_necessaryMias);

      var items = await contentDirectory.SearchAsync(query, true, userProfile, showVirtual);
      lock (_allItems.SyncRoot)
      {
        if (_currentMediaItems != null && _currentMediaItems.Select(m => m.MediaItemId).SequenceEqual(items.Select(m => m.MediaItemId)))
          return false;
        _currentMediaItems = items;
        IEnumerable<ListItem> listItems;
        if (_playableConverterAction != null)
        {
          listItems = items.Select(mi =>
          {
            PlayableMediaItem listItem = _playableConverterAction(mi);
            listItem.Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(listItem.MediaItem));
            return listItem;
          });
        }
        else
        {
          listItems = items.Select(mi => _playableContainerConverterAction(mi));
        }

        _allItems.Clear();
        CollectionUtils.AddAll(_allItems, listItems);
      }

      _allItems.FireChange();
      return true;
    }

    /// <summary>
    /// Gets a <see cref="BooleanCombinationFilter"/> of all filters provided by plugins that are applied to media view initialized
    /// by the specified <paramref name="navigationInitializerType"/>.
    /// </summary>
    /// <param name="navigationInitializerType">The type of the derived <see cref="MediaPortal.UiComponents.Media.Models.NavigationModel.IMediaNavigationInitializer"/></param>
    /// <returns></returns>
    protected IFilter GetNavigationFilter(Type navigationInitializerType)
    {
      if (navigationInitializerType != null)
        lock (_navigationFilterSync)
        {
          InitNavigationFilters();
          if (_navigationFilters.TryGetValue(navigationInitializerType.Name, out IList<IFilter> filters) && filters.Count > 0)
            return filters.Count == 1 ? filters[0] : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filters);
        }
      return null;
    }

    private void InitNavigationFilters()
    {
      if (_tracker != null)
        return;
      
      _tracker = new FixedItemStateTracker("MediaListProvider - Media navigation filter registration");
      _navigationFilters = new Dictionary<string, IList<IFilter>>();

      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(MediaNavigationFilterBuilder.MEDIA_FILTERS_PATH))
      {
        try
        {
          MediaNavigationFilter navigationFilter = pluginManager.RequestPluginItem<MediaNavigationFilter>(
              MediaNavigationFilterBuilder.MEDIA_FILTERS_PATH, itemMetadata.Id, _tracker);
          if (navigationFilter == null)
            ServiceRegistration.Get<ILogger>().Warn("MediaListProvider: Could not instantiate Media navigation filter with id '{0}'", itemMetadata.Id);
          else
          {
            string extensionClass = navigationFilter.ClassName;
            if (extensionClass == null)
              throw new PluginInvalidStateException("MediaListProvider: Could not find class type for Media navigation filter  {0}", navigationFilter.ClassName);
            IList<IFilter> filters;
            if (!_navigationFilters.TryGetValue(extensionClass, out filters))
              _navigationFilters[extensionClass] = filters = new List<IFilter>();
            filters.Add(navigationFilter.Filter);
          }
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("MediaListProvider: Cannot add Media navigation filter with id '{0}'", e, itemMetadata.Id);
        }
      }
    }
  }
}
