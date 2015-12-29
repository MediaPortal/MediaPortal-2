using System;
using System.Collections.Generic;
using System.Linq;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UPnP;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.Messages.Playlist;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using MediaPortal.UiComponents.Media.Actions;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPlaylist
  {
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      string action = (string)message["PlaylistAction"];
      string playlistType = (message["PlaylistType"] != null) ? (string)message["PlaylistType"] : "music";
      bool shuffle = (message["Shuffle"] != null) ? (bool)message["Shuffle"] : false;
      bool autoPlay = (message["AutoPlay"] != null) ? (bool)message["AutoPlay"] : false;
      bool showPlaylist = (message["ShowPlaylist"] != null) ? (bool)message["ShowPlaylist"] : true;

      if (action.Equals("new") || action.Equals("append"))
      {
        //new playlist or append to playlist
        int insertIndex = 0;
        if (message["InsertIndex"] != null)
        {
          insertIndex = (int)message["InsertIndex"];
        }

        // Add items from JSON or SQL
        JArray array = (message["PlaylistItems"] != null) ? (JArray)message["PlaylistItems"] : null;
        JObject sql = (message["PlayListSQL"] != null) ? (JObject)message["PlayListSQL"] : null;
        if (array != null || sql != null)
        {
          if (action.Equals("new"))
          {
            ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist.Clear();
          }

          int index = insertIndex;

          if (array != null)
          {
            // Add items from JSON
            foreach (JObject o in array)
            {
              if (o["FileName"] == null)
                continue;

              Guid itemid;
              if (!Guid.TryParse((string)o["FileName"], out itemid))
              {
                ServiceRegistration.Get<ILogger>().Warn("ParserPlaylist: Couldn't parse Filename to Guid");
                continue;
              }

              MediaItem item = Helper.GetMediaItemById(itemid);
              if (item == null)
              {
                ServiceRegistration.Get<ILogger>().Warn("ParserPlaylist: Not media item found");
                continue;
              }

              ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist.Insert(index, item);

              index++;
            }

            // TODO: Add shuffle
            /*if (shuffle)
            {
              PlaylistHelper.Shuffle(playlistType);
            }*/
          }

          if (autoPlay)
          {
            if (message["StartPosition"] != null)
            {
              int startPos = (int)message["StartPosition"];
              insertIndex += startPos;
            }
            // TODO
            //PlaylistHelper.StartPlayingPlaylist(playlistType, insertIndex, showPlaylist);
          }
        }
      }
      else if (action.Equals("load"))
      {
        //load a playlist
        string playlistName = (string)message["PlayListName"];
        string playlistPath = (string)message["PlaylistPath"];

        Guid playlistId;

        if ((!string.IsNullOrEmpty(playlistName) || !string.IsNullOrEmpty(playlistPath)) && Guid.TryParse(playlistPath, out playlistId))
        {
          // TODO: does this work?!
          IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
          Guid[] necessaryMIATypes = new Guid[]
          {
              ProviderResourceAspect.ASPECT_ID,
              MediaAspect.ASPECT_ID,
          };
          Guid[] optionalMIATypes = new Guid[]
          {
              AudioAspect.ASPECT_ID,
              VideoAspect.ASPECT_ID,
              ImageAspect.ASPECT_ID,
          };

          PlaylistRawData playlistData = cd.ExportPlaylist(playlistId);
          PlayItemsModel.CheckQueryPlayAction(() => CollectionUtils.Cluster(playlistData.MediaItemIds, 1000).SelectMany(itemIds =>
                cd.LoadCustomPlaylist(itemIds, necessaryMIATypes, optionalMIATypes)), AVType.None); // AvType?!
          /*PlaylistHelper.LoadPlaylist(playlistType, (!string.IsNullOrEmpty(playlistName)) ? playlistName : playlistPath, shuffle);
          if (autoPlay)
          {
            PlaylistHelper.StartPlayingPlaylist(playlistType, 0, showPlaylist);
          }*/
        }
      }
      else if (action.Equals("get"))
      {
        //get all playlist items of the currently active playlist
        IPlaylist playlist = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist;
        IList<MediaItem> items = playlist.ItemList;

        MessagePlaylistDetails returnPlaylist = new MessagePlaylistDetails
        {
          PlaylistType = playlistType,
          //PlaylistName = PlaylistHelper.GetPlaylistName(playlistType),
          PlaylistRepeat = playlist.RepeatMode != RepeatMode.None,
          // TODO: Fill
          PlaylistItems = new List<PlaylistEntry>()
        };

        SendMessageToAllClients.Send(returnPlaylist, ref SocketServer.Instance.connectedSockets);
      }
      else if (action.Equals("remove"))
      {
        //remove an item from the playlist
        int indexToRemove = (message["Index"] != null) ? (int)message["Index"] : 0;

        ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist.RemoveAt(indexToRemove);
      }
      else if (action.Equals("move"))
      {
        //move a playlist item to a new index
        int oldIndex = (message["OldIndex"] != null) ? (int)message["OldIndex"] : 0;
        int newIndex = (message["NewIndex"] != null) ? (int)message["NewIndex"] : 0;
        // TODO
        //PlaylistHelper.ChangePlaylistItemPosition(playlistType, oldIndex, newIndex);
      }
      else if (action.Equals("play"))
      {
        //start playback of a playlist item
        int index = (message["Index"] != null) ? (int)message["Index"] : 0;
        ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist.ItemListIndex = index;
        ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Play();
      }
      else if (action.Equals("clear"))
      {
        //clear the playlist
        ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist.Clear();
      }
      else if (action.Equals("list"))
      {
        //get a list of all available playlists
        List<PlaylistInformationData> playLists = ServerPlaylists.GetPlaylists().ToList();

        MessagePlaylists returnList = new MessagePlaylists { PlayLists = playLists.Select(x => x.Name).ToList() };
        SendMessageToAllClients.Send(returnList, ref SocketServer.Instance.connectedSockets);
      }
      else if (action.Equals("save"))
      {
        //save the current playlist to file
        String name = (message["Name"] != null) ? (String)message["Name"] : null;
        if (name != null)
        {
          // TODO
          //PlaylistHelper.SaveCurrentPlaylist(name);
        }
        else
        {
          Logger.Warn("Must specify a name to save a playlist");
        }
      }
      else if (action.Equals("shuffle"))
      {
        var playMode = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist.PlayMode == PlayMode.Shuffle ? PlayMode.Continuous : PlayMode.Shuffle;
        ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist.PlayMode = playMode;
      }
      else if (action.Equals("repeat"))
      {
        Logger.Debug("Playlist action repeat");
        if (message["Repeat"] != null)
        {
          bool repeat = (bool)message["Repeat"];
          RepeatMode repeatMode;
          if (repeat)
            repeatMode = RepeatMode.All;
          else
            repeatMode = RepeatMode.None;
          ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist.RepeatMode = repeatMode;
        }
        else
        {
          Logger.Warn("Must specify repeat to change playlist repeat mode");
        }
      }

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}