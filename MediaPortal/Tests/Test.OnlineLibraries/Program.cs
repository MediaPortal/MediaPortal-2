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
using MediaPortal.Mock;
using MediaPortal.Utilities;
using System.Threading;
using MediaPortal.Extensions.OnlineLibraries.Matchers;

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

    private static void TestMusicBrainz(string title, string artist, string album, int year, int trackNum)
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
      track.TrackName = title;
      track.Artists.Add(new PersonInfo() { Name = artist });
      track.Album = album;
      track.ReleaseDate = new DateTime(year, 1, 1);
      track.TrackNum = trackNum;
      if (matcher.FindAndUpdateTrack(track, false))
      {
        Console.WriteLine("Found track title={0} artist={1} album={2} year={3} trackNum={4}:\nTitle={5} Artists={6} Album={7} Year={8} Track={9}",
          title, artist, album, year, trackNum, track.TrackName, string.Join(", ", track.Artists), track.Album, track.ReleaseDate, track.TrackNum);

        Thread.Sleep(5000); //Let fanart download
      }
      else
      {
        Console.WriteLine("Cannot find track title={0} artist={1} album={2} year={3} trackNum={4}",
          title, artist, album, year, trackNum);
      }
    }

    private static void TestFreeDB(string cdDbId, string title)
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("DATA", "_Test/data");
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", "_Test/log");
      ServiceRegistration.Get<IPathManager>().SetPath("CONFIG", "_Test/config");
      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));
      ServiceRegistration.Set<ILocalization>(new NoLocalization());

      CDFreeDbMatcher matcher = new CDFreeDbMatcher();
      matcher.Init();

      TrackInfo track = new TrackInfo();
      track.AlbumCdDdId = cdDbId;
      track.TrackName = title;
      if (matcher.FindAndUpdateTrack(track, false))
      {
        Console.WriteLine("Found track CDDB ID={0} title={1}:\nTitle={2} Artists={3} Album={4} Year={5} Track={6}",
          cdDbId, title, track.TrackName, string.Join(", ", track.Artists), track.Album, track.ReleaseDate, track.TrackNum);

        Thread.Sleep(5000); //Let fanart download
      }
      else
      {
        Console.WriteLine("Cannot find track CDDB ID={0} title={1}", cdDbId, title);
      }
    }

    private static void TestAudioDB(string title, string artist, string album, int year, int trackNum)
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("DATA", "_Test/data");
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", "_Test/log");
      ServiceRegistration.Get<IPathManager>().SetPath("CONFIG", "_Test/config");
      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));
      ServiceRegistration.Set<ILocalization>(new NoLocalization());

      MusicTheAudioDbMatcher matcher = new MusicTheAudioDbMatcher();
      matcher.Init();

      TrackInfo track = new TrackInfo();
      track.TrackName = title;
      track.Artists.Add(new PersonInfo() { Name = artist });
      track.Album = album;
      track.ReleaseDate = new DateTime(year, 1, 1);
      track.TrackNum = trackNum;
      if (matcher.FindAndUpdateTrack(track, false))
      {
        Console.WriteLine("Found track title={0} artist={1} album={2} year={3} trackNum={4}:\nTitle={5} Artists={6} Album={7} Year={8} Track={9}",
          title, artist, album, year, trackNum, track.TrackName, string.Join(", ", track.Artists), track.Album, track.ReleaseDate, track.TrackNum);

        Thread.Sleep(5000); //Let fanart download
      }
      else
      {
        Console.WriteLine("Cannot find track title={0} artist={1} album={2} year={3} trackNum={4}",
          title, artist, album, year, trackNum);
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

      ServiceRegistration.Set<IMediaAccessor>(new TestMediaAccessor());
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
        SeriesInfo seriesInfo = new SeriesInfo()
        {
          TvdbId = Int32.Parse(value)
        };
        SeriesTvDbMatcher.Instance.UpdateSeries(seriesInfo, false, false);
        Console.WriteLine("{0}: {1}", seriesInfo.SeriesName, seriesInfo.Description);
      }

      SeriesTvDbMatcher.Instance.EndDownloads();
    }

    static void Usage()
    {
      Console.WriteLine("Usage: Test.OnlineLibraries musicbrainz <title> <artist> <album> <year> <track #>");
      Console.WriteLine("Usage: Test.OnlineLibraries audiodb <title> <artist> <album> <year> <track #>");
      Console.WriteLine("Usage: Test.OnlineLibraries freedb <CDDB ID> <title>");
      Console.WriteLine("Usage:                      recording <TVE XML file>");
      Environment.Exit(1);
    }

    static void Main(string[] args)
    {
      try
      {
        if (args.Length >= 1)
        {
          if (args[0] == "musicbrainz" && args.Length == 6)
            TestMusicBrainz(args[1], args[2], args[3], Int32.Parse(args[4]), Int32.Parse(args[5]));

          else if (args[0] == "audiodb" && args.Length == 6)
            TestAudioDB(args[1], args[2], args[3], Int32.Parse(args[4]), Int32.Parse(args[5]));

          else if (args[0] == "freedb" && args.Length == 3)
            TestFreeDB(args[1], args[2]);

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
