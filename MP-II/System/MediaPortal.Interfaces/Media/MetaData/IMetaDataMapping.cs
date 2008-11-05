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

using System.Collections.Generic;
using MediaPortal.Presentation.Localisation;

namespace MediaPortal.Media.MetaData
{
  /// <summary>
  /// Interface definition for a mapping
  /// A mapping specifies which metadata fields of a mediaitem will be shown in the GUI
  /// and how they will be formatted
  /// </summary>
  public interface IMetadataMapping
  {
    /// <summary>
    /// Gets or sets the localized name for this mapping
    /// </summary>
    /// <value>The localized name.</value>
    StringId LocalizedName { get;set;}

    /// <summary>
    /// Gets the mapping items.
    /// </summary>
    /// <value>The mapping items.</value>
    List<IMetadataMappingItem> Items { get;}
  }
}
