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

using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseMPEG2TSInfo
  {
    internal static void ParseMPEG2TSInfo(ref MetadataContainer info)
    {
      if (info.Metadata.VideoContainerType == VideoContainer.Mpeg2Ts || info.Metadata.VideoContainerType == VideoContainer.M2Ts)
      {
        info.Video.TimestampType = Timestamp.None;
        FileStream raf = null;
        ILocalFsResourceAccessor lfsra = (ILocalFsResourceAccessor)info.Metadata.Source;

        // Impersonation
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
        {
          try
          {
            raf = File.Open(lfsra.LocalFileSystemPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] packetBuffer = new byte[193];
            raf.Read(packetBuffer, 0, packetBuffer.Length);
            if (packetBuffer[0] == 0x47) //Sync byte (Standard MPEG2 TS)
            {
              info.Video.TimestampType = Timestamp.None;
              if (Logger != null) Logger.Debug("MediaAnalyzer: Successfully found MPEG2TS timestamp in transport stream: {0}", info.Video.TimestampType);
            }
            else if (packetBuffer[4] == 0x47 && packetBuffer[192] == 0x47) //Sync bytes (BluRay MPEG2 TS)
            {
              if (packetBuffer[0] == 0x00 && packetBuffer[1] == 0x00 && packetBuffer[2] == 0x00 && packetBuffer[3] == 0x00)
              {
                info.Video.TimestampType = Timestamp.Zeros;
              }
              else
              {
                info.Video.TimestampType = Timestamp.Valid;
              }
              if (Logger != null) Logger.Debug("MediaAnalyzer: Successfully found MPEG2TS timestamp in transport stream: {0}", info.Video.TimestampType);
            }
            else
            {
              info.Video.TimestampType = Timestamp.None;
              if (Logger != null) Logger.Error("MediaAnalyzer: Failed to retreive MPEG2TS timestamp for resource '{0}'", info.Metadata.Source);
            }
          }
          finally
          {
            if (raf != null) raf.Close();
          }
        }
      }
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
