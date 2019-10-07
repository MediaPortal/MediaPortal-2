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
using System.Collections.Generic;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Extensions.MediaServer.DIDL;
using MediaPortal.Extensions.MediaServer.Objects;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Objects.MediaLibrary;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using NUnit.Framework;

namespace Tests.Server.MediaServer
{
  class TestMediaLibraryAlbumItem : MediaLibraryAlbumItem
  {
    public TestMediaLibraryAlbumItem(MediaItem item, EndPointSettings settings) : base(item, settings) { }
  }

  [TestFixture]
  class TestMessageBuilder
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
      ServiceRegistration.Set<IResourceServer>(new TestResourceServer());
      ServiceRegistration.Set<ILocalization>(new NoLocalization());
      ServiceRegistration.Set<ISettingsManager>(new NoSettingsManager());
      ServiceRegistration.Set<IMediaLibrary>(new TestMediaLibrary());
      ServiceRegistration.Set<IUserProfileDataManagement>(new TestUserProfileDataManagement());
      ServiceRegistration.Set<IMediaAnalyzer>(new TestAudioAnalyzer());
      ServiceRegistration.Set<ITranscodeProfileManager>(new TestTranscodeProfileManager());
    }

    [Test]
    public void TestAudioItem()
    {
      IList<IDirectoryObject> objects = new List<IDirectoryObject>();

      BasicContainer root = new BasicContainer("TEST", new EndPointSettings
      {
        PreferredSubtitleLanguages = new string[] { "EN" },
        PreferredAudioLanguages = new string[] { "EN" },
        Profile = new EndPointProfile()
      });

      Guid id = new Guid("11111111-aaaa-aaaa-aaaa-111111111111");
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      SingleMediaItemAspect aspect1 = new SingleMediaItemAspect(MediaAspect.Metadata);
      aspect1.SetAttribute(MediaAspect.ATTR_TITLE, "The Track");
      MediaItemAspect.SetAspect(aspects, aspect1);

      MultipleMediaItemAspect aspect2 = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      aspect2.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\file.mp3");
      MediaItemAspect.AddOrUpdateAspect(aspects, aspect2);

      SingleMediaItemAspect aspect3 = new SingleMediaItemAspect(AudioAspect.Metadata);
      MediaItemAspect.SetAspect(aspects, aspect3);

      MediaItem item = new MediaItem(id, aspects);

      objects.Add(MediaLibraryHelper.InstansiateMediaLibraryObject(item, root, "Test"));

      GenericDidlMessageBuilder builder = new GenericDidlMessageBuilder();
      builder.BuildAll("*", objects);

      string xml = builder.ToString();
      Console.WriteLine("XML: {0}", xml);
    }

    [Test]
    public void TestAlbumItem()
    {
      EndPointSettings settings = new EndPointSettings
      {
        PreferredSubtitleLanguages = new string[] { "EN" },
        PreferredAudioLanguages = new string[] { "EN" },
        Profile = new EndPointProfile()
      };

      IList<IDirectoryObject> objects = new List<IDirectoryObject>();

      Guid albumId = new Guid("11111111-aaaa-aaaa-aaaa-100000000001");

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      SingleMediaItemAspect aspect = new SingleMediaItemAspect(MediaAspect.Metadata);
      aspect.SetAttribute(MediaAspect.ATTR_TITLE, "The Album");
      MediaItemAspect.SetAspect(aspects, aspect);

      MediaItem album = new MediaItem(albumId, aspects);

      MediaLibraryAlbumItem item = new TestMediaLibraryAlbumItem(album, settings);
      item.Initialise();
      objects.Add(item);

      GenericDidlMessageBuilder builder = new GenericDidlMessageBuilder();
      builder.BuildAll("*", objects);

      string xml = builder.ToString();
      Console.WriteLine("XML: {0}", xml);
    }
  }
}
