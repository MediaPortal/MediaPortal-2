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

namespace Components.Services.MetaDataMapper
{
  public class MetadataMappingItem : IMetadataMappingItem
  {
    #region IMetadataMappingItem Members
    IMetaDataFormatter _formatter;
    string _metaDataField;
    string _formatting;
    string _skinlabel;

    /// <summary>
    /// Gets or sets the skin label.
    /// </summary>
    /// <value>The skin label.</value>
    public string SkinLabel
    {
      get
      {
        return _skinlabel;
      }
      set
      {
        _skinlabel = value;
      }
    }

    /// <summary>
    /// Gets or sets the formatter to use for this mapping item
    /// </summary>
    /// <value>The formatter.</value>
    public IMetaDataFormatter Formatter
    {
      get
      {
        return _formatter;
      }
      set
      {
        _formatter = value;
      }
    }

    /// <summary>
    /// Gets or sets the meta data field to use in this mapping.
    /// </summary>
    /// <value>The meta data field.</value>
    public string MetaDataField
    {
      get
      {
        return _metaDataField;
      }
      set
      {
        _metaDataField = value;
      }
    }

    /// <summary>
    /// Gets or sets the formatting text to use with the formatter
    /// </summary>
    /// <value>The formatting text.</value>
    public string Formatting
    {
      get
      {
        return _formatting;
      }
      set
      {
        _formatting = value;
      }
    }

    #endregion
  }
}
