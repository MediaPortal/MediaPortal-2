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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;
using MediaPortal.Plugins.Transcoding.Interfaces.Analyzers;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Helpers
{
  public class Subtitles
  {
    public const int NO_SUBTITLE = -2;
    public const int AUTO_SUBTITLE = -1;

    public static List<SubtitleStream> FindExternalSubtitles(ILocalFsResourceAccessor lfsra, string subtitleDefaultEncoding, string subtitleDefaultLanguage)
    {
      List<SubtitleStream> externalSubtitles = new List<SubtitleStream>();
      if (lfsra.Exists)
      {
        // Impersonation
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
        {
          string[] files = Directory.GetFiles(Path.GetDirectoryName(lfsra.LocalFileSystemPath), Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath) + "*.*");
          foreach (string file in files)
          {
            SubtitleStream sub = new SubtitleStream();
            sub.Codec = SubtitleCodec.Unknown;
            if (string.Compare(Path.GetExtension(file), ".srt", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.Srt;
            }
            else if (string.Compare(Path.GetExtension(file), ".smi", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.Smi;
            }
            else if (string.Compare(Path.GetExtension(file), ".ass", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.Ass;
            }
            else if (string.Compare(Path.GetExtension(file), ".ssa", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.Ssa;
            }
            else if (string.Compare(Path.GetExtension(file), ".sub", true, CultureInfo.InvariantCulture) == 0)
            {
              if (File.Exists(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".idx")) == true)
              {
                sub.Codec = SubtitleCodec.VobSub;
              }
              else
              {
                string subContent = File.ReadAllText(file);
                if (subContent.Contains("[INFORMATION]")) sub.Codec = SubtitleCodec.SubView;
                else if (subContent.Contains("}{")) sub.Codec = SubtitleCodec.MicroDvd;
              }
            }
            else if (string.Compare(Path.GetExtension(file), ".vtt", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.WebVtt;
            }
            if (sub.Codec != SubtitleCodec.Unknown)
            {
              sub.Source = file;
              if (SubtitleAnalyzer.IsImageBasedSubtitle(sub.Codec) == false )
              {
                sub.Language = SubtitleAnalyzer.GetLanguage(lfsra, file, subtitleDefaultEncoding, subtitleDefaultLanguage);
                sub.CharacterEncoding = SubtitleAnalyzer.GetEncoding(lfsra, file, subtitleDefaultLanguage, subtitleDefaultEncoding);
              }
              sub.StreamIndex = -(externalSubtitles.Count + 100);
              externalSubtitles.Add(sub);
            }
          }
        }
      }
      return externalSubtitles;
    }

    public static bool SubtitleIsUnicode(string encoding)
    {
      if (string.IsNullOrEmpty(encoding))
      {
        return false;
      }
      if (encoding.ToUpperInvariant().StartsWith("UTF-") || encoding.ToUpperInvariant().StartsWith("UNICODE"))
      {
        return true;
      }
      return false;
    }

    private static bool IsExternalSubtitleAvailable(ILocalFsResourceAccessor lfsra)
    {
      if (lfsra.Exists)
      {
        // Impersonation
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
        {
          string[] files = Directory.GetFiles(Path.GetDirectoryName(lfsra.LocalFileSystemPath), Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath) + "*.*");
          foreach (string file in files)
          {
            if (string.Compare(Path.GetExtension(file), ".srt", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".smi", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".ass", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".ssa", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".sub", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".vtt", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    public static bool IsSubtitleAvailable(VideoTranscoding video)
    {
      if (video.SourceSubtitles != null && video.SourceSubtitles.Count > 0) return true;
      if (video.SourceMedia is ILocalFsResourceAccessor)
      {
        if (IsExternalSubtitleAvailable((ILocalFsResourceAccessor)video.SourceMedia)) return true;
      }
      return false;
    }

    public static List<SubtitleStream> GetSubtitleStreams(VideoTranscoding video, string subtitleDefaultEncoding, string subtitleDefaultLanguage)
    {
      List<SubtitleStream> allSubs = new List<SubtitleStream>();
      if (video.SourceSubtitles != null && video.SourceSubtitles.Count > 0)
      {
        //Only add embedded subtitles
        allSubs.AddRange(video.SourceSubtitles.Where(sub => sub.IsEmbedded == true));
      }

      //Refresh external subtitles
      if (video.SourceMedia is ILocalFsResourceAccessor)
      {
        ILocalFsResourceAccessor lfsra = (ILocalFsResourceAccessor)video.SourceMedia;
        allSubs.AddRange(FindExternalSubtitles(lfsra, subtitleDefaultEncoding, subtitleDefaultLanguage));
      }
      return allSubs;
    }

    public static string GetSubtitleMime(SubtitleCodec codec)
    {
      switch (codec)
      {
        case SubtitleCodec.Srt:
          return "text/srt";
        case SubtitleCodec.MicroDvd:
          return "text/microdvd";
        case SubtitleCodec.SubView:
          return "text/plain";
        case SubtitleCodec.Ass:
          return "text/x-ass";
        case SubtitleCodec.Ssa:
          return "text/x-ssa";
        case SubtitleCodec.Smi:
          return "smi/caption";
        case SubtitleCodec.WebVtt:
          return "text/vtt";
      }
      return "text/plain";
    }
  }
}
