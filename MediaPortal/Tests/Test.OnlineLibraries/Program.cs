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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Extensions.MetadataExtractors;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Utilities;

namespace Test.OnlineLibraries
{
  class Program
  {
    private static void ShowMIAs(IDictionary<Guid, IList<MediaItemAspect>> aspects, IMediaItemAspectTypeRegistration registration)
    {
      foreach (Guid mia in aspects.Keys)
      {
        MediaItemAspectMetadata metadata = registration.LocallyKnownMediaItemAspectTypes[mia];
        foreach (MediaItemAspect aspect in aspects[mia])
        {
          Console.WriteLine(" {0}:", metadata.Name);
          int count = 0;
          string sb = " ";
          foreach (MediaItemAspectMetadata.AttributeSpecification spec in metadata.AttributeSpecifications.Values)
          {
            object value = aspect[spec];
            if (value != null)
            {
              if (count > 0)
                sb += ",";
              if (value is IList)
              {
                sb += string.Format(" {0}({1}/{2})=[{3}]", spec.AttributeName, spec.AttributeType.Name, spec.Cardinality, string.Join(",", (IList)value));
              }
              else
                sb += string.Format(" {0}={1}", spec.AttributeName, value.ToString());
              count++;
            }
          }
          Console.WriteLine(sb);
        }
      }
    }

    private static void TestMusicBrainz(string title, string artist, string album, string genre, int year, int trackNum, string language)
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("DATA", "_Test/data");
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", "_Test/log");
      ServiceRegistration.Get<IPathManager>().SetPath("CONFIG", "_Test/config");
      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));
      ServiceRegistration.Set<ILocalization>(new NoLocalization());

      MusicBrainzMatcher matcher = new MusicBrainzMatcher();
      matcher.Init();

      TrackInfo track = new TrackInfo();
      track.Title = title;
      track.ArtistName = artist;
      track.AlbumName = album;
      track.Genre = genre;
      track.Year = year;
      track.TrackNum = trackNum;
      if (matcher.FindAndUpdateTrack(track))
        Console.WriteLine("Found track title={0} artist={1} album={2} genre={3} year={4} trackNum={5} language={6}:\n{7}", title, artist, album, genre, year, trackNum, language, track);
      else
      {
        Console.WriteLine("Cannot find track title={0} artist={1} album={2} genre={3} year={4} trackNum={5} language={6}", title, artist, album, genre, year, trackNum, language);
      }
    }

    private static void TestRecording(string filename)
    {
      if(Directory.Exists("_Test"))
        Directory.Delete("_Test", true);

      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("DATA", "_Test/Data");

      ServiceRegistration.Set<ILocalization>(new NoLocalization());
      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));

      ServiceRegistration.Set<IMediaAccessor>(new MockMediaAccessor());
      ServiceRegistration.Set<IMediaItemAspectTypeRegistration>(new MockMediaItemAspectTypeRegistration());

      ApplicationCore.RegisterDefaultMediaItemAspectTypes();

      ServiceRegistration.Set<SeriesTvDbMatcher>(new SeriesTvDbMatcher());

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      string ext = StringUtils.TrimToEmpty(ProviderPathHelper.GetExtension(filename)).ToLowerInvariant();
      if (ext != ".xml")
      {
        Console.WriteLine("Filetype must be XML");
        return;
      }
      XmlSerializer serializer = new XmlSerializer(typeof(Tve3RecordingMetadataExtractor.Tags));
      using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
      {
        Tve3RecordingMetadataExtractor.Tags tags = (Tve3RecordingMetadataExtractor.Tags)serializer.Deserialize(stream);
        Tve3RecordingMetadataExtractor.SimpleTag tag = tags.Tag.Find(t => t.Name == "TITLE");
        MediaItemAspect.SetAttribute(aspects, MediaAspect.ATTR_TITLE, tag.Value);
      }

      IMediaItemAspectTypeRegistration registration = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();

      Console.WriteLine("Before extract:");
      ShowMIAs(aspects, registration);

      IMetadataExtractor extractor = new Tve3RecordingMetadataExtractor();
      IResourceAccessor accessor = new MockLocalFsResourceAccessor(ProviderPathHelper.ChangeExtension(filename, ".ts"));
      extractor.TryExtractMetadata(accessor, aspects, false);

      Console.WriteLine("After extract:");
      ShowMIAs(aspects, registration);

      string value;
      if (MediaItemAspect.TryGetExternalAttribute(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out value))
      {
        TvdbSeries seriesDetail;
        SeriesTvDbMatcher.Instance.TryGetSeries(Int32.Parse(value), out seriesDetail);
        Console.WriteLine("{0}: {1}", seriesDetail.SeriesName, seriesDetail.Overview);
        foreach (TvdbEpisode episode in seriesDetail.Episodes)
        {
          Console.WriteLine("S{0}E{1}({2}): {3}", episode.SeasonNumber, episode.EpisodeNumber, episode.SeasonId, episode.EpisodeName);
        }
      }

      SeriesTvDbMatcher.Instance.EndDownloads();
    }

    static void Usage()
    {
      Console.WriteLine("Usage: Test.OnlineLibraries musicbrainz <title> <artist> <album> <genre> <year> <track #>");
      Console.WriteLine("Usage:                      recording <TVE XML file>");
      Environment.Exit(1);
    }

    static void Main(string[] args)
    {
      try
      {
        if (args.Length >= 1)
        {
          if (args[0] == "musicbrainz" && args.Length == 7)
            TestMusicBrainz(args[1], args[2], args[3], args[4], Int32.Parse(args[5]), Int32.Parse(args[6]), "GB");

          else if (args[0] == "recording" && args.Length == 2)
            TestRecording(args[1]);

          else
            Usage();
        }
        else
        {
          Usage();
        }
      }
      catch (Exception e)
      {
        Console.Error.WriteLine("Error running test:\n{0}", e);
        Environment.Exit(1);
      }

      Environment.Exit(0);
    }
  }
}
