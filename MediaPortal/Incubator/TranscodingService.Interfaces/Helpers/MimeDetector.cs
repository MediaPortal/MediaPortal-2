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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Helpers
{
  public class MimeDetector
  {
    public static string GetFileMime(ILocalFsResourceAccessor lfsra, string defaultMime = null)
    {
      // Impersonation
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
      {
        FileStream raf = null;
        try
        {
          raf = File.Open(lfsra.LocalFileSystemPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
          return MimeTypeDetector.GetMimeType(raf);
        }
        catch
        {
          return MimeTypeDetector.GetMimeTypeFromRegistry(lfsra.LocalFileSystemPath);
        }
        finally
        {
          if (raf != null) raf.Close();
        }
      }
    }

    public static string GetUrlMime(string url, string defaultMime = null)
    {
      //if(url.StartsWith("RTSP:", StringComparison.InvariantCultureIgnoreCase) == true ||
      //  url.StartsWith("MMS:", StringComparison.InvariantCultureIgnoreCase) == true)
      //{
      //  return "RTSP";
      //}
      //if (url.StartsWith("RTP:", StringComparison.InvariantCultureIgnoreCase) == true)
      //{
      //  return "RTP";
      //}
      //if (url.StartsWith("HTTP:", StringComparison.InvariantCultureIgnoreCase) == true)
      //{
      //  return "HTTP";
      //}
      //if (url.StartsWith("UDP:", StringComparison.InvariantCultureIgnoreCase) == true)
      //{
      //  return "UDP";
      //}
      return defaultMime;
    }
  }
}
