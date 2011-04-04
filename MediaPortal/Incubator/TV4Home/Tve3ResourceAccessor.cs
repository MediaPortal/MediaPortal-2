#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Utilities;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using TV4Home.Server.TVEInteractionLibrary.Interfaces;

namespace MediaPortal.Media.MediaProviders.Tve3MediaProvider
{
  public class Tve3ResourceAccessor : ILocalFsResourceAccessor
  {
    private bool _isDirectory = false;

    protected Tve3MediaProvider _provider;
    protected string _path;
    protected ITVEInteraction _tvServer;
    private readonly WebChannelGroup _channelGroup;
    private readonly WebChannel _channel;
    private string _localFsPath;


    public Tve3ResourceAccessor(Tve3MediaProvider provider, string path)
    {
      try
      {
        InitTvServer();

        _provider = provider;
        _path = path;
        if (_path != null)
        {
          if (_path == "/")
          {
            _isDirectory = true;
          }
          else
          {
            String[] parts = _path.Split('/');

            // root is the 0th index, skip it
            // first part: tvgroup
            if (parts.Length >= 2)
            {
              List<WebChannelGroup> allGroups = _tvServer.GetGroups();
              foreach (WebChannelGroup group in allGroups)
              {
                if (parts[1].Equals(group.GroupName, StringComparison.CurrentCultureIgnoreCase))
                {
                  _channelGroup = group;
                  _isDirectory = true;
                  break;
                }
              }
            }
            // second part: channel name
            if (parts.Length >= 3 && _channelGroup != null)
            {
              List<WebChannel> allChannels = _tvServer.GetChannels(_channelGroup.IdGroup); 
              foreach (WebChannel channel in allChannels)
              {
                if (parts[2].Equals(channel.DisplayName, StringComparison.CurrentCultureIgnoreCase))
                {
                  _channel = channel;
                  _isDirectory = false;
                  break;
                }
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Debug("Error in Tve3ResourceAccessor: {0}", e.ToString());
      }
    }

    private void InitTvServer()
    {
      if (_tvServer == null)
        _tvServer = ChannelFactory<ITVEInteraction>.CreateChannel(
          new NetNamedPipeBinding {MaxReceivedMessageSize = 10000000},
          new EndpointAddress("net.pipe://localhost/TV4Home.Server.CoreService/TVEInteractionService")
          );
    }

    public Tve3ResourceAccessor(Tve3MediaProvider provider, WebChannelGroup group, WebChannel channel)
    {
      try
      {
        InitTvServer();
        _provider = provider;
        _channelGroup = group;
        _channel = channel;
        if (channel != null)
        {
          _path = String.Format("/{0}/{1}", group.GroupName, channel.DisplayName);
        }
        else
        {
          _path = String.Format("/{0}", group.GroupName);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Debug("Error in Tve3ResourceAccessor: {0}", e.ToString());
      }
    }

    #region ILocalFsResourceAccessor implementation

    public IMediaProvider ParentProvider
    {
      get { return _provider; }
    }

    public string LocalFileSystemPath
    {
      get
      {
        if (String.IsNullOrEmpty(_localFsPath))
        {
          _localFsPath = _tvServer.SwitchTVServerToChannelAndGetTimeshiftFilename(_channel.IdChannel);
          //_localFsPath = _tvServer.SwitchTVServerToChannelAndGetStreamingUrl(_channel.IdChannel);
        }
        return _localFsPath;
      }
    }

    public void Dispose()
    {
      if (_channel != null)
      {
        _tvServer.CancelCurrentTimeShifting();
        _tvServer.Disconnect();
      }
    }

    public ResourcePath LocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(Tve3MediaProvider.TVE3_MEDIA_PROVIDER_ID, _path); }
    }

    public DateTime LastChanged
    {
      get
      {
        string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
        if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
          return DateTime.MinValue;
        return File.GetLastWriteTime(dosPath);
      }
    }

    public bool Exists(string path)
    {
      return _provider.IsResource(path);
    }

    public Stream OpenRead()
    {
      string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      return File.OpenRead(dosPath);
    }

    public Stream OpenWrite()
    {
      string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      return File.OpenWrite(dosPath);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      if (string.IsNullOrEmpty(_path))
        return null;
      if (_path == "/")
        // No files at root level - there are only logical drives
        return new List<IFileSystemResourceAccessor>();

      ICollection<IFileSystemResourceAccessor> result = new List<IFileSystemResourceAccessor>();
      if (_channelGroup != null)
      {
        List<WebChannel> allChannels = _tvServer.GetChannels(_channelGroup.IdGroup); //server.GetAllChannels();
        foreach (WebChannel channel in allChannels)
        {
          result.Add(new Tve3ResourceAccessor(_provider, _channelGroup, channel));
        }
      }
      return result;
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      if (string.IsNullOrEmpty(_path))
        return null;
      // top level: TvGroups
      if (_path == "/")
      {
        List<WebChannelGroup> allGroups = _tvServer.GetGroups();

        ICollection<IFileSystemResourceAccessor> result = new List<IFileSystemResourceAccessor>();
        foreach (WebChannelGroup group in allGroups)
        {
          result.Add(new Tve3ResourceAccessor(_provider, group, null));
        }
        return result;
      }
      // sub level: doesn't contain more groups, only channels (== files)
      return null; 
    }

    public bool IsFile
    {
      get
      {
        return !string.IsNullOrEmpty(_path) && !_isDirectory;
      }
    }

    public bool IsDirectory
    {
      get
      {
        return !string.IsNullOrEmpty(_path) && _isDirectory;
      }
    }

    public string ResourceName
    {
      get
      {
        if (string.IsNullOrEmpty(_path))
          return null;
        if (_path == "/")
          return "/";
        if (!_path.StartsWith("/"))
          return null;
        string path = _path.Substring(1);
        if (path.EndsWith(":/"))
        {
          DriveInfo di = new DriveInfo(path);
          return di.IsReady ? string.Format("[{0}] {1}", path, di.VolumeLabel) : path;
        }
        path = StringUtils.RemoveSuffixIfPresent(path, "/");
        return Path.GetFileName(path);
      }
    }

    public string ResourcePathName
    {
      get
      {
        if (string.IsNullOrEmpty(_path))
          return null;
        return _path == "/" ? "/" : LocalFsMediaProviderBase.ToDosPath(_path);
      }
    }

    #endregion
 
    #region Base overrides

    public override string ToString()
    {
      return ResourcePathName;
    }

    #endregion

    #region IFileSystemResourceAccessor Member


    public IResourceAccessor GetResource(string path)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region IResourceAccessor Member


    public long Size
    {
      get { throw new NotImplementedException(); }
    }

    public void PrepareStreamAccess()
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
