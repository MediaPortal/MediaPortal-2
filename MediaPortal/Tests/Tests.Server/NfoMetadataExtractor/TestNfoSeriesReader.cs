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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using NUnit.Framework;

namespace Tests.Server.NfoMetadataExtractor
{
  [TestFixture]
  public class TestNfoSeriesReader
  {
    [OneTimeSetUp]
    public void Init()
    {
      ServiceRegistration.Set<ILocalization>(new NoLocalization());
    }

    [Test]
    public void TestNfoSeriesEpisodeReaderWriteEpisodeActors()
    {
      //Arrange
      IList<IDictionary<Guid, IList<MediaItemAspect>>> aspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      NfoEpisodeReaderForTests reader = new NfoEpisodeReaderForTests(true);

      //Act
      reader.TryWriteActorMetadata(aspects);

      //Assert
      CollectionAssert.AreEquivalent(reader.EpisodeStub.Actors.Select(a => a.Name),
        aspects.Select(a => MediaItemAspect.GetAspect(a, PersonAspect.Metadata).GetAttributeValue<string>(PersonAspect.ATTR_PERSON_NAME)));
    }

    [Test]
    public void TestNfoSeriesEpisodeReaderWriteEpisodeSeriesActors()
    {
      //Arrange
      IList<IDictionary<Guid, IList<MediaItemAspect>>> aspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      NfoEpisodeReaderForTests reader = new NfoEpisodeReaderForTests(false);

      //Act
      reader.TryWriteActorMetadata(aspects);

      //Assert
      CollectionAssert.AreEquivalent(reader.SeriesStub.Actors.Select(a => a.Name),
        aspects.Select(a => MediaItemAspect.GetAspect(a, PersonAspect.Metadata).GetAttributeValue<string>(PersonAspect.ATTR_PERSON_NAME)));
    }

    [Test]
    public void TestNfoSeriesEpisodeReaderWriteEpisodeCharacters()
    {
      //Arrange
      IList<IDictionary<Guid, IList<MediaItemAspect>>> aspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      NfoEpisodeReaderForTests reader = new NfoEpisodeReaderForTests(true);

      //Act
      reader.TryWriteCharacterMetadata(aspects);

      //Assert
      CollectionAssert.AreEquivalent(reader.EpisodeStub.Actors.Select(a => a.Role),
        aspects.Select(a => MediaItemAspect.GetAspect(a, CharacterAspect.Metadata).GetAttributeValue<string>(CharacterAspect.ATTR_CHARACTER_NAME)));
    }

    [Test]
    public void TestNfoSeriesEpisodeReaderWriteEpisodeSeriesCharacters()
    {
      //Arrange
      IList<IDictionary<Guid, IList<MediaItemAspect>>> aspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      NfoEpisodeReaderForTests reader = new NfoEpisodeReaderForTests(false);

      //Act
      reader.TryWriteCharacterMetadata(aspects);

      //Assert
      CollectionAssert.AreEquivalent(reader.SeriesStub.Actors.Select(a => a.Role),
        aspects.Select(a => MediaItemAspect.GetAspect(a, CharacterAspect.Metadata).GetAttributeValue<string>(CharacterAspect.ATTR_CHARACTER_NAME)));
    }
  }
}
