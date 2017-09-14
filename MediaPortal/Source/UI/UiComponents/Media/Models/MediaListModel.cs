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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.Utilities.Collections;
using MediaPortal.Common.Threading;
using MediaPortal.UiComponents.Media.MediaLists;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaListModel : BaseTimerControlledModel
  {
    #region Consts

    // Global ID definitions and references
    public const string MEDIA_LIST_MODEL_ID_STR = "6121E6CC-EB66-4ABC-8AA0-D952B64C0414";

    // ID variables
    public static readonly Guid MEDIA_LIST_MODEL_ID = new Guid(MEDIA_LIST_MODEL_ID_STR);

    protected readonly AbstractProperty _queryLimitProperty;
    protected bool _updatePending = false;
    protected IPluginItemStateTracker _providerPluginItemStateTracker;

    #endregion

    public const int DEFAULT_QUERY_LIMIT = 5;
    public const int AUTO_UPDATE_INTERVAL = 30000;

    public delegate PlayableMediaItem MediaItemToListItemAction(MediaItem mediaItem);

    public AbstractProperty LimitProperty { get { return _queryLimitProperty; } }

    public int Limit
    {
      get { return (int)_queryLimitProperty.GetValue(); }
      set { _queryLimitProperty.SetValue(value); }
    }

    public SafeDictionary<string, IMediaListProvider> Lists { get; private set; }

    public MediaListModel()
      : base(true, 500)
    {
      _queryLimitProperty = new WProperty(typeof(int), DEFAULT_QUERY_LIMIT);

      InitProviders();
    }

    protected override void Update()
    {
      if (_updatePending)
        UpdateItems();
    }

    public void InitProviders()
    {
      lock (_syncObj)
      {
        if (Lists != null)
          return;
        Lists = new SafeDictionary<string, IMediaListProvider>();

        _providerPluginItemStateTracker = new FixedItemStateTracker("Media Lists - Provider registration");

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(MediaListProviderBuilder.MEDIA_LIST_PROVIDER_PATH))
        {
          try
          {
            MediaListProviderRegistration providerRegistration = pluginManager.RequestPluginItem<MediaListProviderRegistration>(MediaListProviderBuilder.MEDIA_LIST_PROVIDER_PATH, itemMetadata.Id, _providerPluginItemStateTracker);
            if (providerRegistration == null)
              ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Media List provider with id '{0}'", itemMetadata.Id);
            else
            {
              IMediaListProvider provider = Activator.CreateInstance(providerRegistration.ProviderClass) as IMediaListProvider;
              if (provider == null)
                throw new PluginInvalidStateException("Could not create IMediaListProvider instance of class {0}", providerRegistration.ProviderClass);
              if (Lists.ContainsKey(providerRegistration.Key))
              {
                //The default providers cannot replace existing providers
                if (provider.GetType().Assembly != System.Reflection.Assembly.GetExecutingAssembly())
                {
                  //Replace the provider
                  Lists[providerRegistration.Key] = provider;
                  ServiceRegistration.Get<ILogger>().Info("Successfully replaced Media List '{1}' with provider '{0}' (Id '{2}')", itemMetadata.Attributes["ClassName"], itemMetadata.Attributes["Key"], itemMetadata.Id);
                }
              }
              else
              {
                Lists.Add(providerRegistration.Key, provider);
                ServiceRegistration.Get<ILogger>().Info("Successfully activated Media List '{1}' with provider '{0}' (Id '{2}')", itemMetadata.Attributes["ClassName"], itemMetadata.Attributes["Key"], itemMetadata.Id);
              }
            }
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add IMediaListProvider extension with id '{0}'", e, itemMetadata.Id);
          }
        }
      }
    }

    public bool UpdateItems()
    {
      try
      {
        var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (contentDirectory == null)
        {
          _updatePending = true;
          return false;
        }
        _updatePending = false;

        SetLayout();

        foreach (var provider in Lists.Values)
        {
          UpdateAsync(provider);
        }

        if (_updateInterval != AUTO_UPDATE_INTERVAL)
        {
          ChangeInterval(AUTO_UPDATE_INTERVAL);
        }

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error updating Media List", ex);
        return false;
      }
    }

    protected void UpdateAsync(IMediaListProvider provider)
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(() => provider.UpdateItems(Limit));
    }

    protected void SetLayout()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      ViewModeModel vwm = workflowManager.GetModel(ViewModeModel.VM_MODEL_ID) as ViewModeModel;
      if (vwm != null)
      {
        vwm.LayoutType = LayoutType.GridLayout;
        vwm.LayoutSize = LayoutSize.Medium;
      }
    }
  }
}
