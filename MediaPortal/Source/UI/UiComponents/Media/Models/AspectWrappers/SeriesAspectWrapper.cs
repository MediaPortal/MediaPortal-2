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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UiComponents.Media.Models.AspectWrappers
{
/// <summary>
/// SeriesAspectWrapper wraps the contents of <see cref="SeriesAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class SeriesAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _seriesNameProperty;
protected AbstractProperty _origNameProperty;
protected AbstractProperty _descriptionProperty;
protected AbstractProperty _awardsProperty;
protected AbstractProperty _actorsProperty;
protected AbstractProperty _charactersProperty;
protected AbstractProperty _networksProperty;
protected AbstractProperty _companiesProperty;
protected AbstractProperty _certificationProperty;
protected AbstractProperty _isEndedProperty;
protected AbstractProperty _nextSeasonProperty;
protected AbstractProperty _nextEpisodeProperty;
protected AbstractProperty _nextEpisodeNameProperty;
protected AbstractProperty _nextAirDateProperty;
protected AbstractProperty _popularityProperty;
protected AbstractProperty _scoreProperty;
protected AbstractProperty _totalRatingProperty;
protected AbstractProperty _ratingCountProperty;
protected AbstractProperty _availEpisodesProperty;
protected AbstractProperty _availSeasonsProperty;
protected AbstractProperty _numEpisodesProperty;
protected AbstractProperty _numSeasonsProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty SeriesNameProperty
{
  get{ return _seriesNameProperty; }
}

public string SeriesName
{
  get { return (string) _seriesNameProperty.GetValue(); }
  set { _seriesNameProperty.SetValue(value); }
}

public AbstractProperty OrigNameProperty
{
  get{ return _origNameProperty; }
}

public string OrigName
{
  get { return (string) _origNameProperty.GetValue(); }
  set { _origNameProperty.SetValue(value); }
}

public AbstractProperty DescriptionProperty
{
  get{ return _descriptionProperty; }
}

public string Description
{
  get { return (string) _descriptionProperty.GetValue(); }
  set { _descriptionProperty.SetValue(value); }
}

public AbstractProperty AwardsProperty
{
  get{ return _awardsProperty; }
}

public IEnumerable<string> Awards
{
  get { return (IEnumerable<string>) _awardsProperty.GetValue(); }
  set { _awardsProperty.SetValue(value); }
}

public AbstractProperty ActorsProperty
{
  get{ return _actorsProperty; }
}

public IEnumerable<string> Actors
{
  get { return (IEnumerable<string>) _actorsProperty.GetValue(); }
  set { _actorsProperty.SetValue(value); }
}

public AbstractProperty CharactersProperty
{
  get{ return _charactersProperty; }
}

public IEnumerable<string> Characters
{
  get { return (IEnumerable<string>) _charactersProperty.GetValue(); }
  set { _charactersProperty.SetValue(value); }
}

public AbstractProperty NetworksProperty
{
  get{ return _networksProperty; }
}

public IEnumerable<string> Networks
{
  get { return (IEnumerable<string>) _networksProperty.GetValue(); }
  set { _networksProperty.SetValue(value); }
}

public AbstractProperty CompaniesProperty
{
  get{ return _companiesProperty; }
}

public IEnumerable<string> Companies
{
  get { return (IEnumerable<string>) _companiesProperty.GetValue(); }
  set { _companiesProperty.SetValue(value); }
}

public AbstractProperty CertificationProperty
{
  get{ return _certificationProperty; }
}

public string Certification
{
  get { return (string) _certificationProperty.GetValue(); }
  set { _certificationProperty.SetValue(value); }
}

public AbstractProperty IsEndedProperty
{
  get{ return _isEndedProperty; }
}

public bool? IsEnded
{
  get { return (bool?) _isEndedProperty.GetValue(); }
  set { _isEndedProperty.SetValue(value); }
}

public AbstractProperty NextSeasonProperty
{
  get{ return _nextSeasonProperty; }
}

public int? NextSeason
{
  get { return (int?) _nextSeasonProperty.GetValue(); }
  set { _nextSeasonProperty.SetValue(value); }
}

public AbstractProperty NextEpisodeProperty
{
  get{ return _nextEpisodeProperty; }
}

public int? NextEpisode
{
  get { return (int?) _nextEpisodeProperty.GetValue(); }
  set { _nextEpisodeProperty.SetValue(value); }
}

public AbstractProperty NextEpisodeNameProperty
{
  get{ return _nextEpisodeNameProperty; }
}

public string NextEpisodeName
{
  get { return (string) _nextEpisodeNameProperty.GetValue(); }
  set { _nextEpisodeNameProperty.SetValue(value); }
}

public AbstractProperty NextAirDateProperty
{
  get{ return _nextAirDateProperty; }
}

public DateTime? NextAirDate
{
  get { return (DateTime?) _nextAirDateProperty.GetValue(); }
  set { _nextAirDateProperty.SetValue(value); }
}

public AbstractProperty PopularityProperty
{
  get{ return _popularityProperty; }
}

public float? Popularity
{
  get { return (float?) _popularityProperty.GetValue(); }
  set { _popularityProperty.SetValue(value); }
}

public AbstractProperty ScoreProperty
{
  get{ return _scoreProperty; }
}

public float? Score
{
  get { return (float?) _scoreProperty.GetValue(); }
  set { _scoreProperty.SetValue(value); }
}

public AbstractProperty TotalRatingProperty
{
  get{ return _totalRatingProperty; }
}

public double? TotalRating
{
  get { return (double?) _totalRatingProperty.GetValue(); }
  set { _totalRatingProperty.SetValue(value); }
}

public AbstractProperty RatingCountProperty
{
  get{ return _ratingCountProperty; }
}

public int? RatingCount
{
  get { return (int?) _ratingCountProperty.GetValue(); }
  set { _ratingCountProperty.SetValue(value); }
}

public AbstractProperty AvailEpisodesProperty
{
  get{ return _availEpisodesProperty; }
}

public int? AvailEpisodes
{
  get { return (int?) _availEpisodesProperty.GetValue(); }
  set { _availEpisodesProperty.SetValue(value); }
}

public AbstractProperty AvailSeasonsProperty
{
  get{ return _availSeasonsProperty; }
}

public int? AvailSeasons
{
  get { return (int?) _availSeasonsProperty.GetValue(); }
  set { _availSeasonsProperty.SetValue(value); }
}

public AbstractProperty NumEpisodesProperty
{
  get{ return _numEpisodesProperty; }
}

public int? NumEpisodes
{
  get { return (int?) _numEpisodesProperty.GetValue(); }
  set { _numEpisodesProperty.SetValue(value); }
}

public AbstractProperty NumSeasonsProperty
{
  get{ return _numSeasonsProperty; }
}

public int? NumSeasons
{
  get { return (int?) _numSeasonsProperty.GetValue(); }
  set { _numSeasonsProperty.SetValue(value); }
}

public AbstractProperty MediaItemProperty
{
  get{ return _mediaItemProperty; }
}

public MediaItem MediaItem
{
  get { return (MediaItem) _mediaItemProperty.GetValue(); }
  set { _mediaItemProperty.SetValue(value); }
}

#endregion

#region Constructor

public SeriesAspectWrapper()
{
  _seriesNameProperty = new SProperty(typeof(string));
  _origNameProperty = new SProperty(typeof(string));
  _descriptionProperty = new SProperty(typeof(string));
  _awardsProperty = new SProperty(typeof(IEnumerable<string>));
  _actorsProperty = new SProperty(typeof(IEnumerable<string>));
  _charactersProperty = new SProperty(typeof(IEnumerable<string>));
  _networksProperty = new SProperty(typeof(IEnumerable<string>));
  _companiesProperty = new SProperty(typeof(IEnumerable<string>));
  _certificationProperty = new SProperty(typeof(string));
  _isEndedProperty = new SProperty(typeof(bool?));
  _nextSeasonProperty = new SProperty(typeof(int?));
  _nextEpisodeProperty = new SProperty(typeof(int?));
  _nextEpisodeNameProperty = new SProperty(typeof(string));
  _nextAirDateProperty = new SProperty(typeof(DateTime?));
  _popularityProperty = new SProperty(typeof(float?));
  _scoreProperty = new SProperty(typeof(float?));
  _totalRatingProperty = new SProperty(typeof(double?));
  _ratingCountProperty = new SProperty(typeof(int?));
  _availEpisodesProperty = new SProperty(typeof(int?));
  _availSeasonsProperty = new SProperty(typeof(int?));
  _numEpisodesProperty = new SProperty(typeof(int?));
  _numSeasonsProperty = new SProperty(typeof(int?));
  _mediaItemProperty = new SProperty(typeof(MediaItem));
  _mediaItemProperty.Attach(MediaItemChanged);
}

#endregion

#region Members

private void MediaItemChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}

public void Init(MediaItem mediaItem)
{
  SingleMediaItemAspect aspect;
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, SeriesAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  SeriesName = (string) aspect[SeriesAspect.ATTR_SERIES_NAME];
  OrigName = (string) aspect[SeriesAspect.ATTR_ORIG_SERIES_NAME];
  Description = (string) aspect[SeriesAspect.ATTR_DESCRIPTION];
  Actors = (IEnumerable<string>) aspect[SeriesAspect.ATTR_ACTORS] ?? EMPTY_STRING_COLLECTION;
  Characters = (IEnumerable<string>) aspect[SeriesAspect.ATTR_CHARACTERS] ?? EMPTY_STRING_COLLECTION;
  Awards = (IEnumerable<string>) aspect[SeriesAspect.ATTR_AWARDS] ?? EMPTY_STRING_COLLECTION;
  Networks = (IEnumerable<string>) aspect[SeriesAspect.ATTR_NETWORKS] ?? EMPTY_STRING_COLLECTION;
  Companies = (IEnumerable<string>) aspect[SeriesAspect.ATTR_COMPANIES] ?? EMPTY_STRING_COLLECTION;
  Certification = (string) aspect[SeriesAspect.ATTR_CERTIFICATION];
  IsEnded = (bool?) aspect[SeriesAspect.ATTR_ENDED];
  NextSeason = (int?) aspect[SeriesAspect.ATTR_NEXT_SEASON];
  NextEpisode = (int?) aspect[SeriesAspect.ATTR_NEXT_EPISODE];
  NextEpisodeName = (string) aspect[SeriesAspect.ATTR_NEXT_EPISODE_NAME];
  NextAirDate = (DateTime?) aspect[SeriesAspect.ATTR_NEXT_AIR_DATE];
  Popularity = (float?) aspect[SeriesAspect.ATTR_POPULARITY];
  Score = (float?) aspect[SeriesAspect.ATTR_SCORE];
  TotalRating = (double?) aspect[SeriesAspect.ATTR_TOTAL_RATING];
  RatingCount = (int?) aspect[SeriesAspect.ATTR_RATING_COUNT];
  AvailEpisodes = (int?) aspect[SeriesAspect.ATTR_AVAILABLE_EPISODES];
  AvailSeasons = (int?) aspect[SeriesAspect.ATTR_AVAILABLE_SEASONS];
  NumEpisodes = (int?) aspect[SeriesAspect.ATTR_NUM_EPISODES];
  NumSeasons = (int?) aspect[SeriesAspect.ATTR_NUM_SEASONS];
}

public void SetEmpty()
{
  SeriesName = null;
  OrigName = null;
  Description = null;
  Actors = EMPTY_STRING_COLLECTION;
  Characters = EMPTY_STRING_COLLECTION;
  Awards = EMPTY_STRING_COLLECTION;
  Networks = EMPTY_STRING_COLLECTION;
  Companies = EMPTY_STRING_COLLECTION;
  Certification = null;
  IsEnded = null;
  NextSeason = null;
  NextEpisode = null;
  NextEpisodeName = null;
  NextAirDate = null;
  Popularity = null;
  Score = null;
  TotalRating = null;
  RatingCount = null;
  AvailEpisodes = null;
  AvailSeasons = null;
  NumEpisodes = null;
  NumSeasons = null;
}

#endregion

}

}
