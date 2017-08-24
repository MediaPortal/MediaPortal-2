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

using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Runtime;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.SystemResolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MediaPortal.DevTools
{
  class ServerConnectionReceiver : IMessageReceiver
  {
    private AutoResetEvent connectedEvent = new AutoResetEvent(false);
    private ILogger logger = null;

    public ServerConnectionReceiver()
    {
      logger = ServiceRegistration.Get<ILogger>();
      if(logger == null)
      {
        throw new ArgumentNullException("No logger in service registry. You need to call ServiceRegistration.Set<ILogger>(__YOUR__LOGGER__HERE__)");
      }
    }

    public void Receive(SystemMessage message)
    {
      logger.Info("Received message {0} {1}({2}) [{3}]", message.ChannelName, message.MessageType, message.MessageType.GetType().Name, string.Join(";", message.MessageData.Select(x => x.Key + "=" + x.Value)));

      if ("HomeServerConnected" == message.MessageType.ToString())
      {
        logger.Info("Home server is connected");

        connectedEvent.Set();
      }
    }

    public bool Connect()
    {
      return connectedEvent.WaitOne(10000);
    }
  }

  public class Client
  {
    private ILogger logger = null;
    private SystemStateService stateService = null;
    private IServerConnectionManager connectionManager = null;

    public bool Connected { get; set; }

    public Client()
    {
      logger = ServiceRegistration.Get<ILogger>();
    }

    public bool Connect()
    {
      try
      {
        stateService = new SystemStateService();
        ServiceRegistration.Set<ISystemStateService>(stateService);

        stateService.SwitchSystemState(SystemState.Initializing, false);

        logger.Info("Initialising core services");
        ApplicationCore.RegisterVitalCoreServices(false);
        ApplicationCore.RegisterCoreServices();

        logger.Info("Starting localization");
        ServiceRegistration.Set<ILocalization>(new NoLocalization());

        logger.Info("Initialising system resolver");
        ServiceRegistration.Set<ISystemResolver>(new SystemResolver());

        ServerConnectionReceiver reciever = new ServerConnectionReceiver();

        ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(ServerConnectionMessaging.CHANNEL, reciever);

        logger.Info("Initialising server connection manger");
        connectionManager = new ServerConnectionManager();
        ServiceRegistration.Set<IServerConnectionManager>(connectionManager);

        logger.Info("Starting up server connection manger");
        connectionManager.Startup();

        bool connected = reciever.Connect();

        if(connected)
        {
          logger.Info("Initialising media item aspect type registration");
          ServiceRegistration.Set<IMediaItemAspectTypeRegistration>(new MediaItemAspectTypeRegistration());

          ICollection<Guid> types = GetAllManagedMediaItemAspectTypes();
          foreach (Guid type in types)
          {
            MediaItemAspectMetadata metadata = GetMediaItemAspectMetadata(type);
            logger.Info("Registering media item {0}...", metadata.Name);
            ServiceRegistration.Get<IMediaItemAspectTypeRegistration>().RegisterLocallyKnownMediaItemAspectType(metadata);
          }
        }

        Connected = connected;

        return connected;
      }
      catch (Exception e)
      {
        logger.Error("Cannot connect", e);
        return false;
      }
    }

    public void Disconnect()
    {
      try
      {
        stateService.SwitchSystemState(SystemState.ShuttingDown, true);

        ServiceRegistration.IsShuttingDown = true;

        logger.Info("Shutting down server connection manger");
        connectionManager.Shutdown();

        logger.Info("Disposing core services");
        ApplicationCore.DisposeCoreServices();

        stateService.SwitchSystemState(SystemState.Ending, false);
      }
      catch (Exception e)
      {
        logger.Error("Cannot disconnect", e);
      }
    }

    public string GetSystemId()
    {
      return connectionManager.HomeServerSystemId;
    }

    public IContentDirectory GetContentDirectory()
    {
      return connectionManager.ContentDirectory;
    }

    public ICollection<Guid> GetAllManagedMediaItemAspectTypes()
    {
      return GetContentDirectory().GetAllManagedMediaItemAspectTypes();
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid type)
    {
      return GetContentDirectory().GetMediaItemAspectMetadata(type);
    }

    public int GetVideoCount()
    {
      Guid[] types = new Guid[] { VideoStreamAspect.ASPECT_ID };
      IFilter filter = new LikeFilter(MediaAspect.ATTR_TITLE, "%", null, false);

      return GetContentDirectory().CountMediaItems(types, filter, true, true);
    }

    public int GetSeriesCount()
    {
      Guid[] types = new Guid[] { SeriesAspect.ASPECT_ID };
      IFilter filter = new LikeFilter(SeriesAspect.ATTR_SERIES_NAME, "%", null, false);

      return GetContentDirectory().CountMediaItems(types, filter, true, true);
    }

    public IList<MediaItem> SearchSeries(string text)
    {
      Guid[] types = new Guid[] { MediaAspect.ASPECT_ID, VideoStreamAspect.ASPECT_ID };
      IFilter titleFilter = new LikeFilter(MediaAspect.ATTR_TITLE, string.Format("%{0}%", text), null, false);
      IFilter actorsFilter = new LikeFilter(VideoAspect.ATTR_ACTORS, string.Format("%{0}%", text), null, false);
      IFilter directorsFilter = new LikeFilter(VideoAspect.ATTR_DIRECTORS, string.Format("%{0}%", text), null, false);
      IFilter writersFilter = new LikeFilter(VideoAspect.ATTR_WRITERS, string.Format("%{0}%", text), null, false);
      IFilter episodeFilter = new LikeFilter(EpisodeAspect.ATTR_EPISODE_NAME, string.Format("%{0}%", text), null, false);
      IFilter seriesFilter = new LikeFilter(EpisodeAspect.ATTR_SERIES_NAME, string.Format("%{0}%", text), null, false);
      IFilter filter = new BooleanCombinationFilter(BooleanOperator.Or, new IFilter[] { titleFilter, actorsFilter, directorsFilter, writersFilter, episodeFilter, seriesFilter });

      return GetContentDirectory().Search(new MediaItemQuery(types, new Guid[] { SeriesAspect.ASPECT_ID }, filter), true, null, true);
    }
  }
}
