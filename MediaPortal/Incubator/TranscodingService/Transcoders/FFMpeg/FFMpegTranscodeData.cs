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

using System.Text;
using System.IO;
using System.Collections.Generic;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Encoders;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles;
using System;
using System.Linq;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg
{
  internal class FFMpegTranscodeData
  {
    private static readonly string BIN_TRANSCODER = FFMpegBinary.FFMpegPath;
    protected string _overrideParams = null;

    public FFMpegTranscodeData(string workPath)
    {
      TranscoderBinPath = BIN_TRANSCODER;
      WorkPath = workPath;
    }

    public string ClientId;
    public string TranscodeId;
    public string TranscoderBinPath;
    public List<string> GlobalArguments = new List<string>();
    public Dictionary<int, List<string>> InputArguments = new Dictionary<int, List<string>>();
    public Dictionary<int, List<string>> InputSubtitleArguments = new Dictionary<int, List<string>>();
    public List<string> OutputArguments = new List<string>();
    public List<string> OutputFilter = new List<string>();
    public Dictionary<int, IResourceAccessor> InputResourceAccessor;
    public Dictionary<int, string> InputSubtitleFilePath = new Dictionary<int, string>();
    public string OutputFilePath;
    public bool IsLive = false;
    public bool IsStream = false;
    public Stream LiveStream = null;
    public string WorkPath = null;
    public string SegmentPlaylist = null;
    public string SegmentBaseUrl = null;
    public Stream SegmentManifestData = null;
    public Stream SegmentPlaylistData = null;
    public Stream SegmentSubsPlaylistData = null;
    public FFMpegEncoderHandler.EncoderHandler Encoder = FFMpegEncoderHandler.EncoderHandler.Software;

    public int FirstResourceIndex => InputResourceAccessor == null ? -1 : InputResourceAccessor.First().Key;
    public IResourceAccessor FirstResourceAccessor => InputResourceAccessor?.FirstOrDefault().Value;
    public string FirstSubtitleFilePath => InputSubtitleFilePath.FirstOrDefault().Value;

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
          if (InputResourceAccessor != null && InputResourceAccessor.Count > 0)
          {
            foreach (int sourceMediaIndex in InputResourceAccessor.Keys)
            {
              foreach (string arg in InputArguments[sourceMediaIndex])
              {
                result.Append(arg + " ");
              }
              if (InputResourceAccessor[sourceMediaIndex] is ITranscodeLiveAccessor)
              {
                result.Append("-i pipe: ");
              }
              else if (InputResourceAccessor[sourceMediaIndex] is ILocalFsResourceAccessor)
              {
                result.Append("-i \"" + ((ILocalFsResourceAccessor)InputResourceAccessor[sourceMediaIndex]).LocalFileSystemPath + "\" ");
              }
              else if (InputResourceAccessor[sourceMediaIndex] is INetworkResourceAccessor)
              {
                var resolvedUrl = UrlHelper.ResolveHostToIPv4Url(((INetworkResourceAccessor)InputResourceAccessor[sourceMediaIndex]).URL);
                result.Append("-i \"" + resolvedUrl + "\" ");
              }
            }
          }
          if (InputSubtitleFilePath != null && InputSubtitleFilePath.Count > 0)
          {
            foreach (int sourceMediaIndex in InputSubtitleFilePath.Keys)
            {
              if (string.IsNullOrEmpty(InputSubtitleFilePath[sourceMediaIndex]) == false)
              {
                foreach (string arg in InputSubtitleArguments[sourceMediaIndex])
                {
                  result.Append(arg + " ");
                }
                result.Append("-i \"" + InputSubtitleFilePath[sourceMediaIndex] + "\" ");
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
          if (InputResourceAccessor != null && InputResourceAccessor.Count > 0)
          {
            arg = arg.Replace(TranscodeProfileManager.INPUT_FILE_TOKEN, "\"" + ((ILocalFsResourceAccessor)FirstResourceAccessor).LocalFileSystemPath + "\"");
          }
          if (InputSubtitleFilePath != null && InputSubtitleFilePath.Count > 0  && string.IsNullOrEmpty(FirstSubtitleFilePath) == false)
          {
            arg = arg.Replace(TranscodeProfileManager.SUBTITLE_FILE_TOKEN, "\"" + FirstSubtitleFilePath) + "\"";
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
