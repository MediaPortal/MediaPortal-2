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

namespace TvMosaic.Shared
{
  public static class GenreMapper
  {
    public static List<String> MapGenres(this TvMosaic.API.Program tvMosaicProgram)
    {
      List<string> genres = new List<string>();
      // Genres
      if (tvMosaicProgram.IsAction)
        genres.Add("Action");
      if (tvMosaicProgram.IsComedy)
        genres.Add("Comedy");
      if (tvMosaicProgram.IsReality)
        genres.Add("Reality");
      if (tvMosaicProgram.IsAdult)
        genres.Add("Adult");
      if (tvMosaicProgram.IsDocumentary)
        genres.Add("Documentary");
      if (tvMosaicProgram.IsDrama)
        genres.Add("Drama");
      if (tvMosaicProgram.IsEducational)
        genres.Add("Educational");
      if (tvMosaicProgram.IsHorror)
        genres.Add("Horror");
      if (tvMosaicProgram.IsKids)
        genres.Add("Kids");
      if (tvMosaicProgram.IsMovie)
        genres.Add("Movie");
      if (tvMosaicProgram.IsMusic)
        genres.Add("Music");
      if (tvMosaicProgram.IsRomance)
        genres.Add("Romance");
      if (tvMosaicProgram.IsNews)
        genres.Add("News");
      if (tvMosaicProgram.IsScifi)
        genres.Add("Science Fiction");
      if (tvMosaicProgram.IsSoap)
        genres.Add("Soap");
      if (tvMosaicProgram.IsSports)
        genres.Add("Sports");
      if (tvMosaicProgram.IsSpecial)
        genres.Add("Special");
      if (tvMosaicProgram.IsThriller)
        genres.Add("Thriller");

      return genres;
    }
    public static List<String> MapGenres(this TvMosaic.API.VideoInfo videoInfo)
    {
      List<string> genres = new List<string>();
      // Genres according to TvMosaic documentation
      if (videoInfo.CatAction)
        genres.Add("Action");
      if (videoInfo.CatComedy)
        genres.Add("Comedy");
      if (videoInfo.CatDocumentary)
        genres.Add("Documentary");
      if (videoInfo.CatDrama)
        genres.Add("Drama");
      if (videoInfo.CatEducational)
        genres.Add("Educational");
      if (videoInfo.CatHorror)
        genres.Add("Horror");
      if (videoInfo.CatKids)
        genres.Add("Kids");
      if (videoInfo.CatMovie)
        genres.Add("Movie");
      if (videoInfo.CatMusic)
        genres.Add("Music");
      if (videoInfo.CatNews)
        genres.Add("News");
      if (videoInfo.CatReality)
        genres.Add("Reality");
      if (videoInfo.CatRomance)
        genres.Add("Romance");
      if (videoInfo.CatScifi)
        genres.Add("Science Fiction");
      if (videoInfo.CatSerial)
        genres.Add("Series");
      if (videoInfo.CatSoap)
        genres.Add("Soap");
      if (videoInfo.CatSports)
        genres.Add("Sports");
      if (videoInfo.CatSpecial)
        genres.Add("Special");
      if (videoInfo.CatThriller)
        genres.Add("Thriller");
      if (videoInfo.CatAdult)
        genres.Add("Adult");

      return genres;
    }
  }
}
