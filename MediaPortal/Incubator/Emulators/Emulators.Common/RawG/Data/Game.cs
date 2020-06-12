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

using System.Runtime.Serialization;

namespace Emulators.Common.RawG.Data
{
 [DataContract]
  public class Game : NamedItem
  {
    [DataMember(Name = "released")]
    public string Released { get; set; }

    [DataMember(Name = "tba")]
    public bool? Tba { get; set; }

    [DataMember(Name = "background_image")]
    public string BackgroundImageUrl { get; set; }

    [DataMember(Name = "rating")]
    public double Rating { get; set; }

    [DataMember(Name = "rating_top")]
    public double? RatingTop { get; set; }

    [DataMember(Name = "ratings")]
    public Rating[] Ratings { get; set; }

    [DataMember(Name = "ratings_count")]
    public int? RatingsCount { get; set; }

    [DataMember(Name = "reviews_text_count")]
    public int? ReviewsTextCount { get; set; }

    [DataMember(Name = "added")]
    public int? Added { get; set; }

    [DataMember(Name = "added_by_status")]
    public AddedStatus AddedByStatus { get; set; }

    [DataMember(Name = "metacritic")]
    public int? MetacriticRating { get; set; }

    [DataMember(Name = "playtime")]
    public int? Playtime { get; set; }

    [DataMember(Name = "suggestions_count")]
    public int? SuggestionsCount { get; set; }

    [DataMember(Name = "reviews_count")]
    public int? ReviewsCount { get; set; }

    [DataMember(Name = "saturated_color")]
    public string SaturatedColor { get; set; }

    [DataMember(Name = "dominant_color")]
    public string DominantColor { get; set; }

    [DataMember(Name = "platforms")]
    public Release[] Releases { get; set; }

    [DataMember(Name = "parent_platforms")]
    public ParentPlatform[] ParentPlatforms { get; set; }

    [DataMember(Name = "genres")]
    public Genre[] Genres { get; set; }

    [DataMember(Name = "stores")]
    public StoreLink[] Stores { get; set; }

    [DataMember(Name = "clip")]
    public Clip Clip { get; set; }

    [DataMember(Name = "tags")]
    public Genre[] Tags { get; set; }

    [DataMember(Name = "short_screenshots")]
    public Screenshot[] ShortScreenshots { get; set; }
  }
}
