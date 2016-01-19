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
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg
{
  internal class FFMpegTranscodeData : TranscodeData
  {
    private static readonly string BIN_TRANSCODER = FFMpegBinary.FFMpegPath;

    public EncoderHandler Encoder { get; set; } 

    public FFMpegTranscodeData(string workPath) : base(BIN_TRANSCODER, workPath) 
    {
      Encoder = EncoderHandler.Software;
    }

    public override string TranscoderArguments
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
            if (InputResourceAccessor is ILocalFsResourceAccessor)
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
          if (string.IsNullOrEmpty(OutputFilePath) == false)
          {
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
            result.Append("\"" + OutputFilePath + "\" ");
          }
          return result.ToString().Trim();
        }
        else
        {
          string arg = _overrideParams;
          if (InputResourceAccessor != null)
          {
            arg = arg.Replace(MediaConverter.INPUT_FILE_TOKEN, "\"" + ((ILocalFsResourceAccessor)InputResourceAccessor).LocalFileSystemPath + "\"");
          }
          if (string.IsNullOrEmpty(InputSubtitleFilePath) == false)
          {
            arg = arg.Replace(MediaConverter.SUBTITLE_FILE_TOKEN, "\"" + InputSubtitleFilePath) + "\"";
          }
          if (string.IsNullOrEmpty(OutputFilePath) == false)
          {
            arg = arg.Replace(MediaConverter.OUTPUT_FILE_TOKEN, "\"" + OutputFilePath + "\"");
          }
          return arg;
        }
      }
    }
  }
}
