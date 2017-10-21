#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
    public List<string> InputArguments = new List<string>();
    public List<string> InputSubtitleArguments = new List<string>();
    public List<string> OutputArguments = new List<string>();
    public List<string> OutputFilter = new List<string>();
    public IResourceAccessor InputResourceAccessor;
    public string InputSubtitleFilePath;
    public string OutputFilePath;
    public bool IsLive = false;
    public bool IsStream = false;
    public Stream LiveStream = null;
    public string WorkPath = null;
    public string SegmentPlaylist = null;
    public string SegmentBaseUrl = null;
    public byte[] SegmentManifestData = null;
    public byte[] SegmentPlaylistData = null;
    public byte[] SegmentSubsPlaylistData = null;
    public FFMpegEncoderHandler.EncoderHandler Encoder = FFMpegEncoderHandler.EncoderHandler.Software;

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
          if (InputResourceAccessor != null)
          {
            foreach (string arg in InputArguments)
            {
              result.Append(arg + " ");
            }
            if(InputResourceAccessor is IFFMpegLiveAccessor)
            {
              result.Append("-i pipe: ");
            }
            else if (InputResourceAccessor is ILocalFsResourceAccessor)
            {
              result.Append("-i \"" + ((ILocalFsResourceAccessor)InputResourceAccessor).LocalFileSystemPath + "\" ");
            }
            else if (InputResourceAccessor is INetworkResourceAccessor)
            {
              result.Append("-i \"" + ((INetworkResourceAccessor)InputResourceAccessor).URL + "\" ");
            }
          }
          if (string.IsNullOrEmpty(InputSubtitleFilePath) == false)
          {
            foreach (string arg in InputSubtitleArguments)
            {
              result.Append(arg + " ");
            }
            result.Append("-i \"" + InputSubtitleFilePath + "\" ");
          }
          
          foreach (string arg in OutputArguments)
          {
            result.Append(arg + " ");
          }
          if (OutputFilter.Count > 0)
          {
            result.Append("-vf \"");
            bool firstFilter = true;
            foreach (string filter in OutputFilter)
            {
              if (firstFilter == false) result.Append(",");
              result.Append(filter);
              firstFilter = false;
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
          if (InputResourceAccessor != null)
          {
            arg = arg.Replace(TranscodeProfileManager.INPUT_FILE_TOKEN, "\"" + ((ILocalFsResourceAccessor)InputResourceAccessor).LocalFileSystemPath + "\"");
          }
          if (string.IsNullOrEmpty(InputSubtitleFilePath) == false)
          {
            arg = arg.Replace(TranscodeProfileManager.SUBTITLE_FILE_TOKEN, "\"" + InputSubtitleFilePath) + "\"";
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
