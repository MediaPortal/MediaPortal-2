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
using MediaPortal.Common.FanArt;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Extensions.UserServices.FanArtService.Client.ImageSourceProvider;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client.Models
{
  public class FanArtBackgroundModel : IDisposable
  {
    #region Consts

    public const string FANART_MODEL_ID_STR = "8F42C8E9-E8A3-475C-A50A-99E1E644DC91";
    public static Guid FANART_MODEL_ID = new Guid(FANART_MODEL_ID_STR);

    #endregion

    protected AbstractProperty _selectedItemProperty;
    protected AbstractProperty _fanArtMediaTypeProperty;
    protected AbstractProperty _fanArtNameProperty;
    protected AbstractProperty _simpleTitleProperty;
    protected AbstractProperty _itemDescriptionProperty;
    protected AbstractProperty _mediaItemProperty;
    protected AbstractProperty _imageSourceProperty;
    protected AbstractProperty _numItemsProperty;

    protected AsynchronousMessageQueue _messageQueue = null;
    protected readonly object _syncObj = new object();
    protected IList<IFanartImageSourceProvider> _providerList = null;
    protected IPluginItemStateTracker _providerPluginItemStateTracker;

    public FanArtBackgroundModel()
    {
      _selectedItemProperty = new WProperty(typeof(ListItem), null);
      _selectedItemProperty.Attach(SetFanArtType);
      _fanArtMediaTypeProperty = new WProperty(typeof(string), FanArtMediaTypes.Undefined);
      _fanArtNameProperty = new WProperty(typeof(string), string.Empty);
      _simpleTitleProperty = new WProperty(typeof(string), string.Empty);
      _itemDescriptionProperty = new WProperty(typeof(string), string.Empty);
      _mediaItemProperty = new WProperty(typeof(MediaItem), null);
      _imageSourceProperty = new WProperty(typeof(ImageSource), null);
      _numItemsProperty = new WProperty(typeof(int?), null);
      SetFanArtType();
      SetImageSource();
      SubscribeToMessages();
    }

    public void Dispose()
    {
      DisposeMessageQueue();
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new[] { ScreenManagerMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void DisposeMessageQueue()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ScreenManagerMessaging.CHANNEL)
      {
        ScreenManagerMessaging.MessageType messageType = (ScreenManagerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ScreenManagerMessaging.MessageType.ShowScreen:
            SelectedItem = null; // Clear current data for new screen
            break;
        }
      }
    }

    public void InitProviders()
    {
      lock (_syncObj)
      {
        if (_providerList != null)
          return;
        _providerList = new List<IFanartImageSourceProvider>();

        _providerPluginItemStateTracker = new FixedItemStateTracker("Fanart Service - Provider registration");

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(FanartImageSourceProviderBuilder.FANART_IMAGE_SOURCE_PROVIDER_PATH))
        {
          try
          {
            FanartImageSourceProviderRegistration providerRegistration = pluginManager.RequestPluginItem<FanartImageSourceProviderRegistration>(FanartImageSourceProviderBuilder.FANART_IMAGE_SOURCE_PROVIDER_PATH, itemMetadata.Id, _providerPluginItemStateTracker);
            if (providerRegistration == null)
              ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Fanart Image Source provider with id '{0}'", itemMetadata.Id);
            else
            {
              IFanartImageSourceProvider provider = Activator.CreateInstance(providerRegistration.ProviderClass) as IFanartImageSourceProvider;
              if (provider == null)
                throw new PluginInvalidStateException("Could not create IFanartImageSourceProvider instance of class {0}", providerRegistration.ProviderClass);
              _providerList.Add(provider);
              ServiceRegistration.Get<ILogger>().Info("Successfully activated Fanart Image Source provider '{0}' (Id '{1}')", itemMetadata.Attributes["ClassName"], itemMetadata.Id);
            }
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add IFanartImageSourceProvider extension with id '{0}'", e, itemMetadata.Id);
          }
        }
      }
    }

    public Guid ModelId
    {
      get { return FANART_MODEL_ID; }
    }

    #region Members to be accessed from the GUI

    public AbstractProperty SelectedItemProperty
    {
      get { return _selectedItemProperty; }
    }

    public ListItem SelectedItem
    {
      get { return (ListItem)_selectedItemProperty.GetValue(); }
      set { _selectedItemProperty.SetValue(value); }
    }

    public AbstractProperty MediaItemProperty
    {
      get { return _mediaItemProperty; }
    }

    public MediaItem MediaItem
    {
      get { return (MediaItem)_mediaItemProperty.GetValue(); }
      set { _mediaItemProperty.SetValue(value); }
    }

    public AbstractProperty FanArtMediaTypeProperty
    {
      get { return _fanArtMediaTypeProperty; }
    }

    public string FanArtMediaType
    {
      get { return (string)_fanArtMediaTypeProperty.GetValue(); }
      internal set { _fanArtMediaTypeProperty.SetValue(value); }
    }

    public AbstractProperty FanArtNameProperty
    {
      get { return _fanArtNameProperty; }
    }

    public string FanArtName
    {
      get { return (string)_fanArtNameProperty.GetValue(); }
      internal set { _fanArtNameProperty.SetValue(value); }
    }

    public AbstractProperty SimpleTitleProperty
    {
      get { return _simpleTitleProperty; }
    }

    public string SimpleTitle
    {
      get { return (string)_simpleTitleProperty.GetValue(); }
      internal set { _simpleTitleProperty.SetValue(value); }
    }

    public AbstractProperty ItemDescriptionProperty
    {
      get { return _itemDescriptionProperty; }
    }

    public string ItemDescription
    {
      get { return (string)_itemDescriptionProperty.GetValue(); }
      internal set { _itemDescriptionProperty.SetValue(value); }
    }

    public AbstractProperty ImageSourceProperty
    {
      get { return _imageSourceProperty; }
    }

    public ImageSource ImageSource
    {
      get { return (ImageSource)_imageSourceProperty.GetValue(); }
      set { _imageSourceProperty.SetValue(value); }
    }

    public AbstractProperty NumItemsProperty
    {
      get { return _numItemsProperty; }
    }

    public int? NumItems
    {
      get { return (int?)_numItemsProperty.GetValue(); }
      set { _numItemsProperty.SetValue(value); }
    }

    public void SetSelectedItem(object sender, SelectionChangedEventArgs e)
    {
      ListItem item = e.FirstAddedItem as ListItem;
      if (item != null)
        SelectedItem = item;
    }

    public void SetSelectedItemOrNull(object sender, SelectionChangedEventArgs e)
    {
      SelectedItem = e.FirstAddedItem as ListItem;
    }

    #endregion

    private void SetFanArtType(AbstractProperty property, object value)
    {
      SetFanArtType();
      SetImageSource();
    }

    /// <summary>
    /// Creates a new FanArtImageSource instance for exposing it using <see cref="ImageSource"/>.
    /// </summary>
    private void SetImageSource()
    {
      InitProviders();
      FanArtImageSource imageSource;
      foreach (IFanartImageSourceProvider provider in _providerList)
      {
        if (provider.TryCreateFanartImageSource(SelectedItem, out imageSource))
        {
          ImageSource = imageSource;
          FanArtMediaType = imageSource.FanArtMediaType;
          FanArtName = imageSource.FanArtName;
          return;
        }
      }

      ImageSource = new FanArtImageSource
      {
        FanArtMediaType = FanArtMediaTypes.Undefined,
        FanArtName = string.Empty
      };
    }

    private void SetFanArtType()
    {
      // Applies only to container Items
      NumItems = null;
      PlayableMediaItem playableMediaItem = SelectedItem as PlayableMediaItem;
      if (playableMediaItem != null)
      {
        MediaItem = playableMediaItem.MediaItem;
        SimpleTitle = playableMediaItem.SimpleTitle;
      }
      else
      {
        MediaItem = null;
        SimpleTitle = string.Empty;
      }

      SeriesFilterItem series = SelectedItem as SeriesFilterItem;
      if (series != null)
      {
        MediaItem = series.MediaItem;
        SimpleTitle = series.SimpleTitle;
        ItemDescription = series.StoryPlot;
        return;
      }
      SeasonFilterItem season = SelectedItem as SeasonFilterItem;
      if (season != null)
      {
        MediaItem = season.MediaItem;
        SimpleTitle = season.SimpleTitle;
        ItemDescription = season.StoryPlot;
        return;
      }
      EpisodeItem episode = SelectedItem as EpisodeItem;
      if (episode != null)
      {
        MediaItem = episode.MediaItem;
        SimpleTitle = episode.Series;
        ItemDescription = episode.StoryPlot;
        return;
      }
      MovieFilterItem movieCollection = SelectedItem as MovieFilterItem;
      if (movieCollection != null)
      {
        MediaItem = movieCollection.MediaItem;
        SimpleTitle = movieCollection.SimpleTitle;
        ItemDescription = null;
        return;
      }
      MovieItem movie = SelectedItem as MovieItem;
      if (movie != null)
      {
        MediaItem = movie.MediaItem;
        SimpleTitle = movie.SimpleTitle;
        ItemDescription = movie.StoryPlot;
        return;
      }
      VideoItem video = SelectedItem as VideoItem;
      if (video != null)
      {
        MediaItem = video.MediaItem;
        SimpleTitle = video.SimpleTitle;
        ItemDescription = video.StoryPlot;
        return;
      }
      AlbumFilterItem albumItem = SelectedItem as AlbumFilterItem;
      if (albumItem != null)
      {
        MediaItem = albumItem.MediaItem;
        SimpleTitle = albumItem.SimpleTitle;
        ItemDescription = albumItem.Description;
        return;
      }
      AudioItem audioItem = SelectedItem as AudioItem;
      if (audioItem != null)
      {
        MediaItem = audioItem.MediaItem;
        SimpleTitle = audioItem.SimpleTitle;
        ItemDescription = string.Empty;
        return;
      }
      ActorFilterItem actorItem = SelectedItem as ActorFilterItem;
      if (actorItem != null)
      {
        MediaItem = actorItem.MediaItem;
        SimpleTitle = actorItem.SimpleTitle;
        ItemDescription = actorItem.Description;
        return;
      }
      DirectorFilterItem directorItem = SelectedItem as DirectorFilterItem;
      if (directorItem != null)
      {
        MediaItem = directorItem.MediaItem;
        SimpleTitle = directorItem.SimpleTitle;
        ItemDescription = directorItem.Description;
      }
      WriterFilterItem writerItem = SelectedItem as WriterFilterItem;
      if (writerItem != null)
      {
        MediaItem = writerItem.MediaItem;
        SimpleTitle = writerItem.SimpleTitle;
        ItemDescription = writerItem.Description;
      }
      ArtistFilterItem artisitItem = SelectedItem as ArtistFilterItem;
      if (artisitItem != null)
      {
        MediaItem = artisitItem.MediaItem;
        SimpleTitle = artisitItem.SimpleTitle;
        ItemDescription = artisitItem.Description;
      }
      ComposerFilterItem composerItem = SelectedItem as ComposerFilterItem;
      if (composerItem != null)
      {
        MediaItem = composerItem.MediaItem;
        SimpleTitle = composerItem.SimpleTitle;
        ItemDescription = composerItem.Description;
      }
      CharacterFilterItem characterItem = SelectedItem as CharacterFilterItem;
      if (characterItem != null)
      {
        MediaItem = characterItem.MediaItem;
        SimpleTitle = characterItem.SimpleTitle;
        ItemDescription = string.Empty;
      }
      CompanyFilterItem companyItem = SelectedItem as CompanyFilterItem;
      if (companyItem != null)
      {
        MediaItem = companyItem.MediaItem;
        SimpleTitle = companyItem.SimpleTitle;
        ItemDescription = companyItem.Description;
      }
      TVNetworkFilterItem tvNetworkItem = SelectedItem as TVNetworkFilterItem;
      if (tvNetworkItem != null)
      {
        MediaItem = tvNetworkItem.MediaItem;
        SimpleTitle = tvNetworkItem.SimpleTitle;
        ItemDescription = tvNetworkItem.Description;
      }
      FilterItem filterItem = SelectedItem as FilterItem;
      if (filterItem != null)
      {
        MediaItem = filterItem.MediaItem;
        SimpleTitle = filterItem.SimpleTitle;
        ItemDescription = string.Empty;
        NumItems = filterItem.NumItems;
        return;
      }
      ContainerItem containerItem = SelectedItem as ContainerItem;
      if (containerItem != null)
      {
        // Morpheus 2017-04-27: setting the media item for containers lead to unintended layout in views
        // MediaItem = containerItem.FirstMediaItem;
        MediaItem = null;
        if (Guid.TryParse(containerItem.Id, out Guid dirId))
          MediaItem = new MediaItem(dirId);
        SimpleTitle = containerItem.SimpleTitle;
        ItemDescription = string.Empty;
        NumItems = containerItem.NumItems;
        return;
      }
      FanArtMediaType = FanArtMediaTypes.Undefined;
      FanArtName = string.Empty;
      ItemDescription = string.Empty;
    }
  }
}
