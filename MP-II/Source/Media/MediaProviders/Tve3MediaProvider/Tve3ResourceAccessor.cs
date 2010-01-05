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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Services.MediaManagement;
using MediaPortal.Utilities;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Media.MediaProviders.Tve3MediaProvider.Tve3WebService;

namespace MediaPortal.Media.MediaProviders.Tve3MediaProvider
{
  public class Tve3ResourceAccessor : ResourceAccessorBase, ILocalFsResourceAccessor
  {
    private bool _isDirectory = false;

    protected Tve3MediaProvider _provider;
    protected string _path;
    protected Tve3WebService.ServiceInterface _server;
    protected Tve3WebService.WebChannel _channel;
    protected Tve3WebService.WebChannelGroup _channelGroup;
    protected Tve3WebService.WebTvResult _result;

    public Tve3ResourceAccessor(Tve3MediaProvider provider, string path)
    {
      _server = new Tve3WebService.ServiceInterface();
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
            WebChannelGroup[] allGroups = _server.GetTvChannelGroups();
            foreach (WebChannelGroup group in allGroups)
            {
              if (parts[1].Equals(group.name, StringComparison.CurrentCultureIgnoreCase))
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
            WebChannel[] allChannels = _server.GetChannelsInTvGroup(_channelGroup.idGroup); //server.GetAllChannels();
            foreach (WebChannel channel in allChannels)
            {
              if (parts[2].Equals(channel.displayName, StringComparison.CurrentCultureIgnoreCase))
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

    public Tve3ResourceAccessor(Tve3MediaProvider provider, WebChannelGroup group, WebChannel channel)
    {
      _server = new Tve3WebService.ServiceInterface();
      _provider = provider;
      _channelGroup = group;
      _channel = channel;
      if (channel != null)
      {
        _path = String.Format("/{0}/{1}", group.name, channel.displayName);
      }
      else
      {
        _path = String.Format("/{0}", group.name);
      }
    }

    #region ILocalFsResourceAccessor implementation

    public IMediaProvider ParentProvider
    {
      get { return _provider; }
    }

    public string LocalFileSystemPath
    {
      get { 
        if (_channel != null)
        {
          if (_result == null || String.IsNullOrEmpty(_result.timeshiftFile))
          {
            _result = _server.StartTimeShifting(_channel.idChannel);
          }
          return _result.timeshiftFile; // TS file mode
          //return _result.rtspURL; // RTSP Url
        }
        return String.Empty;
      }
    }

    public override void Dispose()
    {
      base.Dispose();
      if (_channel != null)
      {
        if (_result != null && !String.IsNullOrEmpty(_result.timeshiftFile))
        {
          _server.StopTimeShifting(_channel.idChannel, _result.user.idCard, _result.user.name);
        }
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
        WebChannel[] allChannels = _server.GetChannelsInTvGroup(_channelGroup.idGroup); //server.GetAllChannels();
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
        WebChannelGroup[] allGroups = _server.GetTvChannelGroups();

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
  }
}
