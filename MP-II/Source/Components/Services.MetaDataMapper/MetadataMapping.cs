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

using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Media.MetaData;
using MediaPortal.Presentation.Localisation;

namespace Components.Services.MetaDataMapper
{
  public class MetadataMapping : IMetadataMapping
  {
    StringId localizedName;
    List<IMetadataMappingItem> _items;
    #region IMetadataMapping Members

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataMapping"/> class.
    /// </summary>
    public MetadataMapping()
    {
      _items = new List<IMetadataMappingItem>();
    }

    /// <summary>
    /// Gets or sets the localized name for this mapping
    /// </summary>
    /// <value>The localized name.</value>
    public StringId LocalizedName
    {
      get
      {
        return localizedName;
      }
      set
      {
        localizedName = value;
      }
    }

    /// <summary>
    /// Gets the mapping items.
    /// </summary>
    /// <value>The mapping items.</value>
    public List<IMetadataMappingItem> Items
    {
      get { return _items; }
    }

    #endregion
  }
}
