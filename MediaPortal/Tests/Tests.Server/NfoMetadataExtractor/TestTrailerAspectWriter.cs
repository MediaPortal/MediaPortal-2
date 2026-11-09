#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Utilities;
using NUnit.Framework;

namespace Tests.Server.NfoMetadataExtractor
{
  /// <summary>
  /// Checks which trailers an import notes.
  /// </summary>
  /// <remarks>
  /// Both halves of an import - the trailers a metadata file names and the trailer file standing in the film's
  /// folder - go through <see cref="TrailerAspectWriter"/>, so what is refused here never reaches the database.
  /// Refusing is worth testing on its own because a stored trailer is a promise: the shelf offers it, and a
  /// trailer standing at a site that answers no more cannot be played at all.
  /// </remarks>
  [TestFixture]
  public class TestTrailerAspectWriter
  {
    [OneTimeSetUp]
    public void Init()
    {
      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));
    }

    [Test]
    public void TestAddRefusesATrailerStandingAtASiteThatAnswersNoMore()
    {
      //Arrange
      var extractedAspectData = new Dictionary<Guid, IList<MediaItemAspect>>();

      //Act
      bool plain = TrailerAspectWriter.Add(extractedAspectData,
          "https://movietrailers.apple.com/movies/paramount/top-gun/top-gun-trailer-1_h720p.mov",
          TrailerAspect.KIND_STREAM, "Trailer");
      bool behindPlugin = TrailerAspectWriter.AddDetected(extractedAspectData,
          "plugin://plugin.video.hdtrailers_net/video/apple.com/"
          + "https%3A%2F%2Fmovietrailers.apple.com%2Fmovies%2Ffox%2Favatar%2Favatar-trailer_h720p.mov");

      //Assert
      Assert.IsFalse(plain);
      Assert.IsFalse(behindPlugin);
      IList<MultipleMediaItemAspect> stored;
      Assert.IsFalse(MediaItemAspect.TryGetAspects(extractedAspectData, TrailerAspect.Metadata, out stored),
          "A trailer that cannot be played must not be noted at all.");
    }

    [Test]
    public void TestAddNotesATrailerStandingAtASiteThatAnswers()
    {
      //Arrange
      var extractedAspectData = new Dictionary<Guid, IList<MediaItemAspect>>();

      //Act
      bool fromSite = TrailerAspectWriter.AddDetected(extractedAspectData,
          "plugin://plugin.video.youtube/play/?video_id=VWqJifMMgZE");
      bool fromFolder = TrailerAspectWriter.Add(extractedAspectData,
          "file://192.168.0.10/filme/Alien/trailer.mp4", TrailerAspect.KIND_FILE, null);

      //Assert
      Assert.IsTrue(fromSite);
      Assert.IsTrue(fromFolder);
      IList<MultipleMediaItemAspect> stored;
      Assert.IsTrue(MediaItemAspect.TryGetAspects(extractedAspectData, TrailerAspect.Metadata, out stored));
      Assert.AreEqual(2, stored.Count);
      Assert.AreEqual(TrailerAspect.KIND_YOUTUBE, stored[0].GetAttributeValue<int>(TrailerAspect.ATTR_KIND));
      Assert.AreEqual(TrailerAspect.KIND_FILE, stored[1].GetAttributeValue<int>(TrailerAspect.ATTR_KIND));
    }
  }
}
