#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Extensions.MetadataExtractors.GenreProvider;
using NUnit.Framework;

namespace Test.Common
{
  [TestFixture]
  public class TestGenres
  {
    protected readonly List<IGenreProvider> _providers = new List<IGenreProvider>();

    [SetUp]
    public void SetUp()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
      InitProviders();
    }

    public void InitProviders()
    {
      _providers.Add(new GenreProvider());
    }

    [Test]
    public void TestGenreMovieId()
    {
      //Arrange
      var exceptions = new List<VideoGenre>
      {
        VideoGenre.Unknown,
        VideoGenre.Game,
        VideoGenre.News,
        VideoGenre.Reality,
        VideoGenre.Soap,
        VideoGenre.Talk,
        VideoGenre.SciFi,
        VideoGenre.Politics
      };

      foreach (VideoGenre genre in Enum.GetValues(typeof(VideoGenre)))
        if(!exceptions.Contains(genre))
          TestGenre(genre.ToString(), GenreCategory.Movie, (int)genre);
    }

    [Test]
    public void TestGenreSeriesId()
    {
      //Arrange
      var exceptions = new List<VideoGenre>
      {
        VideoGenre.Unknown,
        VideoGenre.TvMovie,
        VideoGenre.SciFi,
        VideoGenre.Noir
      };

      foreach (VideoGenre genre in Enum.GetValues(typeof(VideoGenre)))
        if (!exceptions.Contains(genre))
          TestGenre(genre.ToString(), GenreCategory.Series, (int)genre);
    }

    [Test]
    public void TestGenreMusicId()
    {
      //Arrange
      var exceptions = new List<AudioGenre>
      {
        AudioGenre.Unknown,
        AudioGenre.NewAge,
        AudioGenre.EasyListening
      };

      foreach (AudioGenre genre in Enum.GetValues(typeof(AudioGenre)))
        if (!exceptions.Contains(genre))
          TestGenre(genre.ToString(), GenreCategory.Music, (int)genre);
    }

    [Test]
    public void TestGenreEpgId()
    {
      //Arrange
      var exceptions = new List<EpgGenre>
      {
        EpgGenre.Unknown
      };

      foreach (EpgGenre genre in Enum.GetValues(typeof(EpgGenre)))
        if (!exceptions.Contains(genre))
          TestGenre(genre.ToString(), GenreCategory.Epg, (int)genre);
    }

    private void TestGenre(string genreName, string genreCategory, int expectedGenreId)
    {
      //Act
      int genreId = 0;
      bool result = false;
      IGenreProvider lastUsedProvider = null;
      foreach (IGenreProvider provider in _providers)
      {
        // We know that not all providers can support all formats, so we allow all to be tried.
        lastUsedProvider = provider;
        result = provider.GetGenreId(genreName, genreCategory, "en", out genreId);
        if (result)
          break;
      }

      //Assert
      Assert.AreEqual(true, result, $"{lastUsedProvider?.GetType().Name}: Genre detection failed for genre ({genreName}, {genreCategory})");
      Assert.AreEqual(genreId, expectedGenreId, $"{lastUsedProvider?.GetType().Name}: Genre detection success, but genre was wrong ({genreName}, {genreCategory}, {genreId}, {expectedGenreId})");
    }
  }
}
