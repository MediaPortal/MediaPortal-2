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
  /// interface definition for a single metadatafield mapping
  /// It defines how a specific metadatafield gets transformed into a gui representation
  /// </summary>
  public interface IMetadataMappingItem
  {
    /// <summary>
    /// Gets or sets the skin label.
    /// </summary>
    /// <value>The skin label.</value>
    string SkinLabel { get;set;}
    /// <summary>
    /// Gets or sets the formatter to use for this mapping item
    /// </summary>
    /// <value>The formatter.</value>
    IMetaDataFormatter Formatter { get;set;}

    /// <summary>
    /// Gets or sets the meta data field to use in this mapping.
    /// </summary>
    /// <value>The meta data field.</value>
    string MetaDataField { get;set;}

    /// <summary>
    /// Gets or sets the formatting text to use with the formatter
    /// </summary>
    /// <value>The formatting text.</value>
    string Formatting { get;set;}
  }
}
