#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata class representing a single review or comment. This class is used in the
  /// <see cref="PluginSocialInfo"/> metadata model.
  /// </summary>
  public class PluginReview
  {
    #region Review Details
    /// <summary>
    /// The name of the author of the review.
    /// </summary>
    public string Author { get; private set; }

    /// <summary>
    /// The date and time (in UTC time zone) the review was posted. Use the <see cref="DateTimeOffset"/> 
    /// class to obtain the date and time in the users local time zone.
    /// </summary>
    public DateTime Created { get; private set; }

    /// <summary>
    /// The language (specified as a .NET culture string) used for <see cref="Title"/> and
    /// <see cref="Body"/> of the review. If the language is unknown or the review has no
    /// textual content (no title or body), this property will return <c>null</c>.
    /// </summary>
    public string LanguageCulture { get; private set; }

    /// <summary>
    /// The title of the review (optional).
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// The body of the review (optional).
    /// </summary>
    public string Body { get; private set; }

    /// <summary>
    /// The version of the plugin associated with this review.
    /// </summary>
    public string PluginVersion { get; private set; }

    /// <summary>
    /// The rating given to the plugin by the author of the review.
    /// </summary>
    public int Rating { get; private set; }
    #endregion

    #region Ctor
		public PluginReview( string author, DateTime created, string languageCulture, string title, string body, string pluginVersion, int rating )
    {
      Author = author;
      Created = created;
      LanguageCulture = languageCulture;
      Title = title;
      Body = body;
      PluginVersion = pluginVersion;
		  Rating = rating;
    }
    #endregion
  }
}
