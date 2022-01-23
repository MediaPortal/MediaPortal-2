#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System.Text;
using System.IO;
using System.Collections.Generic;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Encoders;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles;
using System;
using System.Linq;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Common;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg
{
  internal class FFMpegTranscodeData
  {
    private static readonly string BIN_TRANSCODER = FFMpegBinary.FFMpegPath;
    protected string _overrideParams = null;
    protected Dictionary<ResourcePath, string> _runtimeResourcePaths = new Dictionary<ResourcePath, string>();

    public FFMpegTranscodeData(string workPath)
    {
      TranscoderBinPath = BIN_TRANSCODER;
      WorkPath = workPath;
    }

    public string ClientId { get; set; }
    public string TranscodeId { get; set; }
    public string TranscoderBinPath { get; set; }
    public List<string> GlobalArguments { get; set; } = new List<string>();
    public Dictionary<int, List<string>> InputArguments { get; set; } = new Dictionary<int, List<string>>();
    public Dictionary<int, Dictionary<int, List<string>>> InputSubtitleArguments { get; set; } = new Dictionary<int, Dictionary<int, List<string>>>();
    public List<string> OutputArguments { get; set; } = new List<string>();
    public List<string> OutputFilter { get; set; } = new List<string>();
    public Dictionary<int, string> InputMediaFilePaths { get; set; } = new Dictionary<int, string>();
    public Dictionary<int, List<string>> InputSubtitleFilePaths { get; set; } = new Dictionary<int, List<string>>();
    public string OutputFilePath { get; set; }
    public bool IsLive { get; set; } = false;
    public bool IsStream { get; set; } = false;
    public bool ConcatedFileInput { get; set; } = false;
    public Stream LiveStream { get; set; } = null;
    public string WorkPath { get; set; } = null;
    public string SegmentPlaylist { get; set; } = null;
    public string SegmentBaseUrl { get; set; } = null;
    public Stream SegmentManifestData { get; set; } = null;
    public Stream SegmentPlaylistData { get; set; } = null;
    public Stream SegmentSubsPlaylistData { get; set; } = null;
    public FFMpegEncoderHandler.EncoderHandler Encoder { get; set; } = FFMpegEncoderHandler.EncoderHandler.Software;

    public void AddSubtitle(int mediaSourceIndex, string subtitle)
    {
      if (!InputSubtitleFilePaths.ContainsKey(mediaSourceIndex))
      {
        InputSubtitleFilePaths[mediaSourceIndex] = new List<string>();
        InputSubtitleArguments[mediaSourceIndex] = new Dictionary<int, List<string>>();
      }
      InputSubtitleFilePaths[mediaSourceIndex].Add(subtitle);
    }

    public void AddSubtitleArgument(int mediaSourceIndex, int subtitleIndex, string arg)
    {
      if (!InputSubtitleArguments[mediaSourceIndex].ContainsKey(subtitleIndex))
      {
        InputSubtitleArguments[mediaSourceIndex][subtitleIndex] = new List<string>();
      }
      InputSubtitleArguments[mediaSourceIndex][subtitleIndex].Add(arg);
    }

    public int FirstResourceIndex => InputMediaFilePaths.Any() ? InputMediaFilePaths.First().Key : -1;

    public IEnumerable<IResourceAccessor> GetResourceAccessors()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      List<IResourceAccessor> resources = new List<IResourceAccessor>();
      foreach (var res in InputMediaFilePaths)
      {
        var path = ResourcePath.Deserialize(res.Value);
        if (TranscodeLiveAccessor.TRANSCODE_LIVE_PROVIDER_ID == path.BasePathSegment.ProviderId)
        {
          resources.Add(new TranscodeLiveAccessor(Convert.ToInt32(path.BasePathSegment.Path)));
        }
        else if (path.TryCreateLocalResourceAccessor(out var accessor))
        {
          resources.Add(accessor);
        }
      }
      return resources;
    }

    public IResourceAccessor GetFirstResourceAccessor()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      List<IResourceAccessor> resources = new List<IResourceAccessor>();
      foreach (var res in InputMediaFilePaths)
      {
        var path = ResourcePath.Deserialize(res.Value);
        if (TranscodeLiveAccessor.TRANSCODE_LIVE_PROVIDER_ID == path.BasePathSegment.ProviderId)
        {
          return new TranscodeLiveAccessor(Convert.ToInt32(path.BasePathSegment.Path));
        }
        else if (path.TryCreateLocalResourceAccessor(out var accessor))

        {
          return accessor;
        }
      }
      return null;
    }

    public void ClearRuntimeResourcePaths()
    {
      _runtimeResourcePaths.Clear();
    }

    public void AddRuntimeResourcePath(ResourcePath resourcePath, string runtimePath)
    {
      if (!_runtimeResourcePaths.ContainsKey(resourcePath))
        _runtimeResourcePaths.Add(resourcePath, runtimePath);
    }

    public string TranscoderArguments
    {
      set
      {
        _overrideParams = value;
      }
      get
      {
        if (_overrideParams == null)
        {
          StringBuilder result = new StringBuilder();
          foreach (string arg in GlobalArguments)
          {
            result.Append(arg + " ");
          }
          if (InputMediaFilePaths?.Count > 0)
          {
            if (ConcatedFileInput)
            {
              List<string> concatList = new List<string>();
              foreach (int sourceMediaIndex in InputMediaFilePaths.Keys)
              {
                var path = ResourcePath.Deserialize(InputMediaFilePaths[sourceMediaIndex]);
                if (path.TryCreateLocalResourceAccessor(out var res))
                {
                  if (res is ILocalFsResourceAccessor fileRes)
                    concatList.Add(fileRes.LocalFileSystemPath);
                  else if (_runtimeResourcePaths.TryGetValue(path, out var runtimePath))
                    concatList.Add(runtimePath);
                }
              }
              result.Append($"-i concat:\"{string.Join("|", concatList)}\" ");
            }
            else
            {
              foreach (int sourceMediaIndex in InputMediaFilePaths.Keys)
              {
                foreach (string arg in InputArguments[sourceMediaIndex])
                {
                  result.Append(arg + " ");
                }

                var path = ResourcePath.Deserialize(InputMediaFilePaths[sourceMediaIndex]);
                if (TranscodeLiveAccessor.TRANSCODE_LIVE_PROVIDER_ID == path.BasePathSegment.ProviderId)
                {
                  if (_runtimeResourcePaths.TryGetValue(path, out var runtimePath))
                  {
                    var pathRuntitme = ResourcePath.Deserialize(runtimePath);
                    if (SlimTvResourceProvider.SLIMTV_RESOURCE_PROVIDER_ID == pathRuntitme.BasePathSegment.ProviderId)
                    {
                      using (var slimTvAccessor = SlimTvResourceProvider.GetResourceAccessor(pathRuntitme.BasePathSegment.Path))
                      {
                        if (slimTvAccessor is INetworkResourceAccessor slimTvNet)
                          result.Append("-i \"" + slimTvNet.URL + "\" ");
                      }
                    }
                  }
                  else
                  {
                    result.Append("-i pipe: ");
                  }
                }
                else if (path.IsNetworkResource)
                {
                  var resolvedUrl = UrlHelper.ResolveHostToIPv4Url(path.BasePathSegment.Path);
                  result.Append("-i \"" + resolvedUrl + "\" ");
                }
                else if (path.TryCreateLocalResourceAccessor(out var res))
                {
                  if (res is ILocalFsResourceAccessor fileRes)
                    result.Append("-i \"" + fileRes.LocalFileSystemPath + "\" ");
                  else if (_runtimeResourcePaths.TryGetValue(path, out var runtimePath))
                    result.Append("-i \"" + runtimePath + "\" ");
                }
              }
            }
          }
          if (InputSubtitleFilePaths?.Count > 0)
          {
            foreach (int sourceMediaIndex in InputSubtitleFilePaths.Keys)
            {
              for(int subIdx = 0; subIdx < InputSubtitleFilePaths[sourceMediaIndex].Count; subIdx++)
              {
                var path = ResourcePath.Deserialize(InputSubtitleFilePaths[sourceMediaIndex][subIdx]);
                if (path.TryCreateLocalResourceAccessor(out var res))
                {
                  try
                  {
                    string filePath;
                    if (res is ILocalFsResourceAccessor fileRes)
                      filePath = fileRes.LocalFileSystemPath;
                    else if (_runtimeResourcePaths.TryGetValue(path, out var runtimePath))
                      filePath = runtimePath;
                    else
                      continue;

                    foreach (string arg in InputSubtitleArguments[sourceMediaIndex][subIdx])
                    {
                      result.Append(arg + " ");
                    }

                    result.Append("-i \"" + filePath + "\" ");
                  }
                  finally
                  {
                    res.Dispose();
                  }
                }
              }
            }
          }
          
          foreach (string arg in OutputArguments)
          {
            result.Append(arg + " ");
          }
          if (OutputFilter.Count > 0)
          {
            result.Append("-filter_complex \"");
            foreach (string filter in OutputFilter)
            {
              result.Append(filter);
            }
            result.Append("\" ");
          }
          if (string.IsNullOrEmpty(OutputFilePath) == true)
          {
            result.Append("pipe: ");
          }
          else
          {
            result.Append("\"" + OutputFilePath + "\" ");
          }
          return result.ToString().Trim();
        }
        else
        {
          string arg = _overrideParams;
          if (InputMediaFilePaths?.Count > 0)
          {
            var path = ResourcePath.Deserialize(InputMediaFilePaths.Values.First());
            if (path.TryCreateLocalResourceAccessor(out var res))
            {
              if (res is ILocalFsResourceAccessor fileRes)
                arg = arg.Replace(TranscodeProfileManager.INPUT_FILE_TOKEN, "\"" + fileRes.LocalFileSystemPath) + "\"";
              else if (_runtimeResourcePaths.TryGetValue(path, out var runtimePath))
                arg = arg.Replace(TranscodeProfileManager.INPUT_FILE_TOKEN, "\"" + runtimePath) + "\"";

              res.Dispose();
            }
          }
          if (InputSubtitleFilePaths?.Count > 0)
          {
            var path = ResourcePath.Deserialize(InputSubtitleFilePaths.Values.First().First());
            if (path.TryCreateLocalResourceAccessor(out var res))
            {
              if (res is ILocalFsResourceAccessor fileRes)
                arg = arg.Replace(TranscodeProfileManager.SUBTITLE_FILE_TOKEN, "\"" + fileRes.LocalFileSystemPath) + "\"";
              else if (_runtimeResourcePaths.TryGetValue(path, out var runtimePath))
                arg = arg.Replace(TranscodeProfileManager.SUBTITLE_FILE_TOKEN, "\"" + runtimePath) + "\"";

              res.Dispose();
            }
          }
          if (string.IsNullOrEmpty(OutputFilePath) == false)
          {
            arg = arg.Replace(TranscodeProfileManager.OUTPUT_FILE_TOKEN, "\"" + OutputFilePath + "\"");
          }
          return arg;
        }
      }
    }
  }
}
