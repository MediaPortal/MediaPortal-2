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

using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseMPEG2TSInfo
  {
    internal static void ParseMPEG2TSInfo(IResourceAccessor res, MetadataContainer info)
    {
      if (info.Metadata[Editions.DEFAULT_EDITION].VideoContainerType == VideoContainer.Mpeg2Ts || info.Metadata[Editions.DEFAULT_EDITION].VideoContainerType == VideoContainer.M2Ts)
      {
        info.Video[Editions.DEFAULT_EDITION].TimestampType = Timestamp.None;
        FileStream raf = null;
        if (!(res is ILocalFsResourceAccessor fileRes) || !fileRes.IsFile)
          return;

        // Impersonation
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(res.CanonicalLocalResourcePath))
        {
          try
          {
            raf = File.Open(fileRes.LocalFileSystemPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] packetBuffer = new byte[193];
            raf.Read(packetBuffer, 0, packetBuffer.Length);
            if (packetBuffer[0] == 0x47) //Sync byte (Standard MPEG2 TS)
            {
              info.Video[Editions.DEFAULT_EDITION].TimestampType = Timestamp.None;
              Logger.Debug("MediaAnalyzer: Successfully found MPEG2TS timestamp in transport stream: {0}", info.Video[Editions.DEFAULT_EDITION].TimestampType);
            }
            else if (packetBuffer[4] == 0x47 && packetBuffer[192] == 0x47) //Sync bytes (BluRay MPEG2 TS)
            {
              if (packetBuffer[0] == 0x00 && packetBuffer[1] == 0x00 && packetBuffer[2] == 0x00 && packetBuffer[3] == 0x00)
              {
                info.Video[Editions.DEFAULT_EDITION].TimestampType = Timestamp.Zeros;
              }
              else
              {
                info.Video[Editions.DEFAULT_EDITION].TimestampType = Timestamp.Valid;
              }
              Logger.Debug("MediaAnalyzer: Successfully found MPEG2TS timestamp in transport stream: {0}", info.Video[Editions.DEFAULT_EDITION].TimestampType);
            }
            else
            {
              info.Video[Editions.DEFAULT_EDITION].TimestampType = Timestamp.None;
              Logger.Error("MediaAnalyzer: Failed to retreive MPEG2TS timestamp for resource '{0}'", res);
            }
          }
          finally
          {
            raf?.Close();
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
