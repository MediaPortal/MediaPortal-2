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

using MediaPortal.Common.Certifications;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.TransientAspects;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  public abstract class INfoRelationshipExtractor
  {
    public static bool UpdatePersons(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<PersonInfo> infoPersons, bool forSeries)
    {
      if (aspects.ContainsKey(TempActorAspect.ASPECT_ID))
      {
        IList<MultipleMediaItemAspect> persons;
        if (MediaItemAspect.TryGetAspects(aspects, TempActorAspect.Metadata, out persons))
        {
          foreach (MultipleMediaItemAspect person in persons)
          {
            if (person.GetAttributeValue<bool>(TempActorAspect.ATTR_FROMSERIES) == forSeries)
            {
              PersonInfo info = infoPersons.Find(p => p.Name.Equals(person.GetAttributeValue<string>(TempActorAspect.ATTR_NAME), StringComparison.InvariantCultureIgnoreCase) &&
                  p.Occupation == person.GetAttributeValue<string>(TempActorAspect.ATTR_OCCUPATION) && string.IsNullOrEmpty(person.GetAttributeValue<string>(TempActorAspect.ATTR_CHARACTER)));
              if (info != null)
              {
                if(string.IsNullOrEmpty(info.ImdbId))
                  info.ImdbId = person.GetAttributeValue<string>(TempActorAspect.ATTR_IMDBID);
                if (info.Biography.IsEmpty)
                  info.Biography = person.GetAttributeValue<string>(TempActorAspect.ATTR_BIOGRAPHY);
                if (string.IsNullOrEmpty(info.Orign))
                  info.Orign = person.GetAttributeValue<string>(TempActorAspect.ATTR_ORIGIN);
                if (!info.DateOfBirth.HasValue)
                  info.DateOfBirth = person.GetAttributeValue<DateTime?>(TempActorAspect.ATTR_DATEOFBIRTH);
                if (!info.DateOfDeath.HasValue)
                  info.DateOfDeath = person.GetAttributeValue<DateTime?>(TempActorAspect.ATTR_DATEOFDEATH);
                if (!info.Order.HasValue)
                  info.Order = person.GetAttributeValue<int?>(TempActorAspect.ATTR_ORDER);
              }
            }
          }
          return true;
        }
      }
      return false;
    }

    public static void StorePersons(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<PersonInfo> infoPersons, bool forSeries)
    {
      foreach (PersonInfo person in infoPersons)
      {
        MultipleMediaItemAspect personAspect = MediaItemAspect.CreateAspect(aspects, TempActorAspect.Metadata);
        personAspect.SetAttribute(TempActorAspect.ATTR_IMDBID, person.ImdbId);
        personAspect.SetAttribute(TempActorAspect.ATTR_NAME, person.Name);
        personAspect.SetAttribute(TempActorAspect.ATTR_OCCUPATION, person.Occupation);
        personAspect.SetAttribute(TempActorAspect.ATTR_BIOGRAPHY, person.Biography);
        personAspect.SetAttribute(TempActorAspect.ATTR_DATEOFBIRTH, person.DateOfBirth);
        personAspect.SetAttribute(TempActorAspect.ATTR_DATEOFDEATH, person.DateOfDeath);
        personAspect.SetAttribute(TempActorAspect.ATTR_ORDER, person.Order);
        personAspect.SetAttribute(TempActorAspect.ATTR_ORIGIN, person.Orign);
        personAspect.SetAttribute(TempActorAspect.ATTR_FROMSERIES, forSeries);
      }
    }

    public static bool UpdateCharacters(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<CharacterInfo> infoCharacters, bool forSeries)
    {
      if (aspects.ContainsKey(TempActorAspect.ASPECT_ID))
      {
        IList<MultipleMediaItemAspect> persons;
        if (MediaItemAspect.TryGetAspects(aspects, TempActorAspect.Metadata, out persons))
        {
          foreach (MultipleMediaItemAspect person in persons)
          {
            if (person.GetAttributeValue<bool>(TempActorAspect.ATTR_FROMSERIES) == forSeries)
            {
              CharacterInfo info = infoCharacters.Find(p => p.Name.Equals(person.GetAttributeValue<string>(TempActorAspect.ATTR_CHARACTER), StringComparison.InvariantCultureIgnoreCase));
              if (info != null)
              {
                if (string.IsNullOrEmpty(info.ActorImdbId))
                  info.ActorImdbId = person.GetAttributeValue<string>(TempActorAspect.ATTR_IMDBID);
                if (string.IsNullOrEmpty(info.ActorName))
                  info.ActorName = person.GetAttributeValue<string>(TempActorAspect.ATTR_NAME);
                  if (!info.Order.HasValue)
                  info.Order = person.GetAttributeValue<int?>(TempActorAspect.ATTR_ORDER);
              }
            }
          }
          return true;
        }
      }
      return false;
    }

    public static void StoreCharacters(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<CharacterInfo> infoCharacters, bool forSeries)
    {
      foreach (CharacterInfo character in infoCharacters)
      {
        MultipleMediaItemAspect personAspect = MediaItemAspect.CreateAspect(aspects, TempActorAspect.Metadata);
        personAspect.SetAttribute(TempActorAspect.ATTR_IMDBID, character.ActorImdbId);
        personAspect.SetAttribute(TempActorAspect.ATTR_NAME, character.ActorName);
        personAspect.SetAttribute(TempActorAspect.ATTR_CHARACTER, character.Name);
        personAspect.SetAttribute(TempActorAspect.ATTR_ORDER, character.Order);
        personAspect.SetAttribute(TempActorAspect.ATTR_FROMSERIES, forSeries);
      }
    }

    public static void StorePersonAndCharacter(IDictionary<Guid, IList<MediaItemAspect>> aspects, PersonStub person, string occupation, bool forSeries)
    {
      MultipleMediaItemAspect personAspect = MediaItemAspect.CreateAspect(aspects, TempActorAspect.Metadata);
      personAspect.SetAttribute(TempActorAspect.ATTR_IMDBID, person.ImdbId);
      personAspect.SetAttribute(TempActorAspect.ATTR_NAME, person.Name);
      personAspect.SetAttribute(TempActorAspect.ATTR_OCCUPATION, occupation);
      personAspect.SetAttribute(TempActorAspect.ATTR_CHARACTER, person.Role);
      personAspect.SetAttribute(TempActorAspect.ATTR_BIOGRAPHY, !string.IsNullOrEmpty(person.Biography) ? person.Biography : person.MiniBiography);
      personAspect.SetAttribute(TempActorAspect.ATTR_DATEOFBIRTH, person.Birthdate);
      personAspect.SetAttribute(TempActorAspect.ATTR_DATEOFDEATH, person.Deathdate);
      personAspect.SetAttribute(TempActorAspect.ATTR_ORDER, person.Order);
      personAspect.SetAttribute(TempActorAspect.ATTR_ORIGIN, person.Birthplace);
      personAspect.SetAttribute(TempActorAspect.ATTR_FROMSERIES, forSeries);
    }

    public static bool UpdateSeries(IDictionary<Guid, IList<MediaItemAspect>> aspects, SeriesInfo info)
    {
      if (aspects.ContainsKey(TempSeriesAspect.ASPECT_ID))
      {
        string text;
        double? number;
        DateTime? date;
        int? integer;
        bool? boolean;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_TVDBID, out integer) && integer.HasValue)
          info.TvdbId = integer.Value;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_NAME, out text) && !string.IsNullOrEmpty(text))
          info.SeriesName.Text = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_SORT_NAME, out text) && !string.IsNullOrEmpty(text))
          info.SeriesNameSort.Text = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_ENDED, out boolean) && boolean.HasValue)
          info.IsEnded = boolean.Value;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_PLOT, out text) && !string.IsNullOrEmpty(text))
          info.Description.Text = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_CERTIFICATION, out text) && !string.IsNullOrEmpty(text))
          info.Certification = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_PREMIERED, out date) && date.HasValue)
          info.FirstAired = date;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_RATING, out number) && number.HasValue)
        {
          info.Rating.RatingValue = number;
          info.Rating.VoteCount = null;
          if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_VOTES, out integer) && integer.HasValue)
            info.Rating.VoteCount = integer.Value;
        }

        if(info.Networks.Count == 0)
        {
          string station;
          if(MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_STATION, out station) && !string.IsNullOrEmpty(station))
            info.Networks.Add(new CompanyInfo { Name = station, Type = CompanyAspect.COMPANY_TV_NETWORK });
        }
        if(info.Genres.Count == 0)
        {
          IEnumerable collection;
          if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_GENRES, out collection))
            info.Genres.AddRange(collection.Cast<object>().Select(s => new GenreInfo { Name = s.ToString() }));
        }
        return true;
      }
      return false;
    }

    public static void StoreSeries(IDictionary<Guid, IList<MediaItemAspect>> aspects, SeriesStub series)
    {
      SingleMediaItemAspect seriesAspect = MediaItemAspect.GetOrCreateAspect(aspects, TempSeriesAspect.Metadata);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_TVDBID, series.Id.HasValue ? series.Id.Value : 0);
      string title = !string.IsNullOrEmpty(series.Title) ? series.Title : series.ShowTitle;
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_NAME, title);
      if(!string.IsNullOrEmpty(series.SortTitle))
        seriesAspect.SetAttribute(TempSeriesAspect.ATTR_SORT_NAME, series.SortTitle);
      else
        seriesAspect.SetAttribute(TempSeriesAspect.ATTR_SORT_NAME, BaseInfo.GetSortTitle(title));

      CertificationMapping cert = null;
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_CERTIFICATION, null);
      if (series.Mpaa != null && series.Mpaa.Any())
      {
        foreach (string certification in series.Mpaa)
        {
          if (CertificationMapper.TryFindSeriesCertification(certification, out cert))
          {
            seriesAspect.SetAttribute(TempSeriesAspect.ATTR_CERTIFICATION, cert.CertificationId);
            break;
          }
        }
      }
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_ENDED, !string.IsNullOrEmpty(series.Status) ? series.Status.Contains("End") : false);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_PLOT, !string.IsNullOrEmpty(series.Plot) ? series.Plot : series.Outline);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_PREMIERED, series.Premiered.HasValue ? series.Premiered.Value : series.Year.HasValue ? series.Year.Value : default(DateTime?));
      seriesAspect.SetCollectionAttribute(TempSeriesAspect.ATTR_GENRES, series.Genres);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_RATING, series.Rating.HasValue ? Convert.ToDouble(series.Rating.Value) : 0.0);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_VOTES, series.Votes.HasValue ? series.Votes.Value : series.Rating.HasValue ? 1 : 0);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_STATION, series.Studio);
    }
  }
}
