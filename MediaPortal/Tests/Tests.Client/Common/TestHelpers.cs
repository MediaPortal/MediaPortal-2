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
using System.Linq;
using MediaPortal.Common.MediaManagement.Helpers;
using NUnit.Framework;

namespace Test.Common
{
  [TestFixture]
  public class TestMerge
  {
    public MovieInfo _movie = null;
    public MovieInfo _movieSearch = null;

    [SetUp]
    public void SetUp()
    {
      _movie = new MovieInfo
      {
        MovieName = "Test Movie",
        OriginalName = "Original Movie Name",
        Budget = 100000,
        Awards = new List<string> { "Oscar" },
        AllocinebId = 100,
        Actors = new List<PersonInfo>(),
        CinePassionId = 200,
        Genres = new List<GenreInfo>(),
        Rating = new SimpleRating(5, 10),
        Runtime = 120,
        Thumbnail = new byte[] { 0x01, 0x02, 0x03 },
        Writers = new List<PersonInfo>(),
        Characters = new List<CharacterInfo>(),
        Languages = new List<string> { "en-US" },
        Directors = new List<PersonInfo>(),
        ProductionCompanies = new List<CompanyInfo>(),
      };
      _movie.Actors.Add(new PersonInfo
      {
        Name = "Actor Name Long",
        DateOfBirth = DateTime.Now,
        ImdbId = "nm00000",
        AudioDbId = 123,
        IsGroup = false,
        Occupation = "Actor",
      });
      _movie.Actors.Add(new PersonInfo
      {
        Name = "Actor Name Long 2",
        DateOfBirth = DateTime.Now,
        ImdbId = "nm00001",
        AudioDbId = 124,
        IsGroup = false,
        Occupation = "Actor",
      });
      _movie.Genres.Add(new GenreInfo
      {
        Id = 1,
        Name = "Action"
      });

      _movieSearch = new MovieInfo
      {
        MovieName = "Test Movie Long",
        Budget = 100000,
        Awards = new List<string> { "Oscar", "Emmy" },
        ImdbId = "tt0000",
        Actors = new List<PersonInfo>(),
        CollectionName = "Test Collection",
        CollectionMovieDbId = 122,
        CollectionNameId = "testcollection",
        MovieDbId = 222,
        Genres = new List<GenreInfo>(),
        Rating = new SimpleRating(7, 100),
        Runtime = 125,
        Thumbnail = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 },
        Writers = new List<PersonInfo>(),
        Characters = new List<CharacterInfo>(),
        Languages = new List<string> { "de" },
        Directors = new List<PersonInfo>(),
        ProductionCompanies = new List<CompanyInfo>(),
        MovieNameSort = "Movie Sort Name",
        Summary = "Movie Summery",
        Tagline = "Movie Tagline",
        Revenue = 500000,
        Certification = "PG-13"
      };
      _movieSearch.Actors.Add(new PersonInfo
      {
        Name = "Actor Name 2",
        DateOfBirth = DateTime.Now,
        ImdbId = "nm00001",
        AudioDbId = 124,
        IsGroup = false,
        Biography = "Actor Biography",
        DateOfDeath = DateTime.Now,
        AlternateName = "Alternate Name",
        MusicBrainzId = "7F1E039D-480E-4076-BD02-11F215C79C89",
        Occupation = "Actor",
        TvdbId = 324
      });
      _movieSearch.ProductionCompanies.Add(new CompanyInfo
      {
        Name = "Company Name",
        ImdbId = "cc10001",
        AudioDbId = 333,
        MusicBrainzId = "A19A198B-BBC2-426F-ACD1-EE545F16B2BC",
        Type = "Film Studio",
        TvdbId = 444
      });
      _movieSearch.Genres.Add(new GenreInfo
      {
        Id = 2,
        Name = "Adventure"
      });
    }

    [Test]
    public void TestMovieClone()
    {
      //Act
      MovieInfo clone = _movie.Clone();

      //Assert
      Assert.AreEqual(clone.MovieName.Text, _movie.MovieName.Text, "Error cloning string");
      Assert.AreEqual(clone.OriginalName, _movie.OriginalName, "Error cloning string");
      Assert.IsTrue(clone.Awards.SequenceEqual(_movie.Awards), "Error cloning list");
      Assert.IsTrue(clone.Genres.SequenceEqual(_movie.Genres), "Error cloning list");
      Assert.AreEqual(clone.Rating.RatingValue, _movie.Rating.RatingValue, "Error cloning rating");
      Assert.AreEqual(clone.Actors[1].Name, _movie.Actors[1].Name, "Error cloning list item");
      Assert.AreEqual(clone.Actors[1].TvdbId, _movie.Actors[1].TvdbId, "Error cloning list item");

      clone.Awards.Clear();
      clone.Actors.Clear();
      clone.Genres.Clear();
      clone.OriginalName = "";
      clone.MovieName = "";
      clone.Rating.RatingValue = 1;
      Assert.AreNotEqual(clone.Awards.Count, _movie.Awards.Count, "Error cloning list");
      Assert.AreNotEqual(clone.Actors.Count, _movie.Actors.Count, "Error cloning list");
      Assert.AreNotEqual(clone.Genres.Count, _movie.Genres.Count, "Error cloning list");
      Assert.AreNotEqual(clone.MovieName.Text, _movie.MovieName.Text, "Error cloning string");
      Assert.AreNotEqual(clone.OriginalName, _movie.OriginalName, "Error cloning string");
      Assert.AreNotEqual(clone.Rating.RatingValue, _movie.Rating.RatingValue, "Error cloning rating");
    }

    [Test]
    public void TestMovieMerge()
    {
      //Act
      MovieInfo clone = _movie.Clone();
      clone.MergeWith(_movieSearch);

      //Assert
      Assert.AreEqual(clone.MovieName.Text, _movieSearch.MovieName.Text, "Error merging strings by length");
      Assert.IsTrue(clone.Awards.Contains("Emmy"), "Error adding missing award");
      Assert.IsFalse(clone.Genres.Any(g => g.Name == "Adventure"), "Error with only merging if genre is empty");
      Assert.IsTrue(clone.ProductionCompanies.Any(p => p.Name == "Company Name" && p.AudioDbId == 333), "Error adding missing company");
      Assert.AreEqual(clone.Rating.RatingValue, _movieSearch.Rating.RatingValue, "Error updating rating");
      Assert.AreEqual(clone.MovieDbId, _movieSearch.MovieDbId, "Error merging integer id");
      Assert.AreEqual(clone.ImdbId, _movieSearch.ImdbId, "Error merging string id");
      Assert.IsTrue(!clone.Summary.IsEmpty, "Error updating empty string");
      Assert.AreEqual(clone.Actors[1].Name, "Actor Name Long 2", "Error merging strings by length");
      Assert.AreEqual(clone.Actors[1].TvdbId, _movieSearch.Actors[0].TvdbId, "Error merging actors");
      Assert.IsTrue(clone.Actors[1].DateOfDeath.HasValue, "Error merging actors");
    }
  }
}
