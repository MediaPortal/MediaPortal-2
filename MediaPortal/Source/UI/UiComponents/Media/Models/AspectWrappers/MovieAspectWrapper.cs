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
/// MovieAspectWrapper wraps the contents of <see cref="MovieAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class MovieAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _movieNameProperty;
protected AbstractProperty _origNameProperty;
protected AbstractProperty _collectionNameProperty;
protected AbstractProperty _runtimeProperty;
protected AbstractProperty _certificationProperty;
protected AbstractProperty _taglineProperty;
protected AbstractProperty _awardsProperty;
protected AbstractProperty _companiesProperty;
protected AbstractProperty _popularityProperty;
protected AbstractProperty _budgetProperty;
protected AbstractProperty _revenueProperty;
protected AbstractProperty _scoreProperty;
protected AbstractProperty _totalRatingProperty;
protected AbstractProperty _ratingCountProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty MovieNameProperty
{
  get{ return _movieNameProperty; }
}

public string MovieName
{
  get { return (string) _movieNameProperty.GetValue(); }
  set { _movieNameProperty.SetValue(value); }
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

public AbstractProperty CollectionNameProperty
{
  get{ return _collectionNameProperty; }
}

public string CollectionName
{
  get { return (string) _collectionNameProperty.GetValue(); }
  set { _collectionNameProperty.SetValue(value); }
}

public AbstractProperty RuntimeProperty
{
  get{ return _runtimeProperty; }
}

public int? Runtime
{
  get { return (int?) _runtimeProperty.GetValue(); }
  set { _runtimeProperty.SetValue(value); }
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

public AbstractProperty TaglineProperty
{
  get{ return _taglineProperty; }
}

public string Tagline
{
  get { return (string) _taglineProperty.GetValue(); }
  set { _taglineProperty.SetValue(value); }
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

public AbstractProperty CompaniesProperty
{
  get{ return _companiesProperty; }
}

public IEnumerable<string> Companies
{
  get { return (IEnumerable<string>) _companiesProperty.GetValue(); }
  set { _companiesProperty.SetValue(value); }
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

public AbstractProperty BudgetProperty
{
  get{ return _budgetProperty; }
}

public long? Budget
{
  get { return (long?) _budgetProperty.GetValue(); }
  set { _budgetProperty.SetValue(value); }
}

public AbstractProperty RevenueProperty
{
  get{ return _revenueProperty; }
}

public long? Revenue
{
  get { return (long?) _revenueProperty.GetValue(); }
  set { _revenueProperty.SetValue(value); }
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

public MovieAspectWrapper()
{
  _movieNameProperty = new SProperty(typeof(string));
  _origNameProperty = new SProperty(typeof(string));
  _collectionNameProperty = new SProperty(typeof(string));
  _runtimeProperty = new SProperty(typeof(int?));
  _certificationProperty = new SProperty(typeof(string));
  _taglineProperty = new SProperty(typeof(string));
  _awardsProperty = new SProperty(typeof(IEnumerable<string>));
  _companiesProperty = new SProperty(typeof(IEnumerable<string>));
  _popularityProperty = new SProperty(typeof(float?));
  _budgetProperty = new SProperty(typeof(long?));
  _revenueProperty = new SProperty(typeof(long?));
  _scoreProperty = new SProperty(typeof(float?));
  _totalRatingProperty = new SProperty(typeof(double?));
  _ratingCountProperty = new SProperty(typeof(int?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, MovieAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  MovieName = (string) aspect[MovieAspect.ATTR_MOVIE_NAME];
  OrigName = (string) aspect[MovieAspect.ATTR_ORIG_MOVIE_NAME];
  CollectionName = (string) aspect[MovieAspect.ATTR_COLLECTION_NAME];
  Runtime = (int?) aspect[MovieAspect.ATTR_RUNTIME_M];
  Certification = (string) aspect[MovieAspect.ATTR_CERTIFICATION];
  Awards = (IEnumerable<string>) aspect[MovieAspect.ATTR_AWARDS] ?? EMPTY_STRING_COLLECTION;
  Tagline = (string) aspect[MovieAspect.ATTR_TAGLINE];
  Companies = (IEnumerable<string>) aspect[MovieAspect.ATTR_COMPANIES] ?? EMPTY_STRING_COLLECTION;
  Popularity = (float?) aspect[MovieAspect.ATTR_POPULARITY];
  Budget = (long?) aspect[MovieAspect.ATTR_BUDGET];
  Revenue = (long?) aspect[MovieAspect.ATTR_REVENUE];
  Score = (float?) aspect[MovieAspect.ATTR_SCORE];
  TotalRating = (double?) aspect[MovieAspect.ATTR_TOTAL_RATING];
  RatingCount = (int?) aspect[MovieAspect.ATTR_RATING_COUNT];
}

public void SetEmpty()
{
  MovieName = null;
  OrigName = null;
  CollectionName = null;
  Runtime = null;
  Certification = null;
  Awards = EMPTY_STRING_COLLECTION;
  Tagline = null;
  Companies = EMPTY_STRING_COLLECTION;
  Popularity = null;
  Budget = null;
  Revenue = null;
  Score = null;
  TotalRating = null;
  RatingCount = null;
}

#endregion

}

}
