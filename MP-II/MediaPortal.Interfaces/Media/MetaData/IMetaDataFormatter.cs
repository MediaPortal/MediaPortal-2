#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion


namespace MediaPortal.Media.MetaData
{
  /// <summary>
  /// Interface definition for a formatter
  /// A formatter is a class which can format a metadata value into a specific representation
  /// E.g. you might have formatters which format a date, a file-size or a timespan
  /// </summary>
  public interface IMetaDataFormatter
  {
    /// <summary>
    /// Gets or sets the name for the formatter
    /// </summary>
    /// <value>The name.</value>
    string Name { get;set;}

    /// <summary>
    /// Formats the specified metadata object into the correct representation
    /// </summary>
    /// <param name="metaData">The metadata object.</param>
    /// <param name="formatting">The formatting to use.</param>
    /// <returns>string containing the formatted metadata object</returns>
    string Format(object metaData, string formatting);

    int CompareTo(object metaData1, object metaData2);
  }
}
