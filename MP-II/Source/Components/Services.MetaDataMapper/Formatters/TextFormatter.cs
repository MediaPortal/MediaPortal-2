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

namespace Components.Services.MetaDataMapper.Formatters
{
  public class TextFormatter : IMetaDataFormatter
  {
    #region IMetaDataFormatter Members

    /// <summary>
    /// Gets or sets the name for the formatter
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get
      {
        return "text";
      }
      set
      {
      }
    }
    string GetText(object metaData)
    {
      if (metaData == null) return "";
      return metaData.ToString();
    }

    /// <summary>
    /// Formats the specified metadata object into the correct representation
    /// </summary>
    /// <param name="metaData">The metadata object.</param>
    /// <param name="formatting">The formatting to use.</param>
    /// <returns>
    /// string containing the formatted metadata object
    /// </returns>
    public string Format(object metaData, string formatting)
    {
      return GetText(metaData);
    }
    public int CompareTo(object metaData1, object metaData2)
    {
      string t1 = GetText(metaData1);
      string t2 = GetText(metaData2);
      return t1.CompareTo(t2);
    }

    #endregion
  }
}
