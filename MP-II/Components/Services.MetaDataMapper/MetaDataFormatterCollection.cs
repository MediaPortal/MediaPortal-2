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

using Components.Services.MetaDataMapper.Formatters;

namespace Components.Services.MetaDataMapper
{
  public class MetaDataFormatterCollection : IMetaDataFormatterCollection
  {
    List<IMetaDataFormatter> _formatters;
    /// <summary>
    /// Initializes a new instance of the <see cref="MetaDataFormatterCollection"/> class.
    /// </summary>
    public MetaDataFormatterCollection()
    {
      _formatters = new List<IMetaDataFormatter>();
      _formatters.Add(new TextFormatter());
      _formatters.Add(new DateTimeFormatter());
      _formatters.Add(new TimeSpanFormatter());
      _formatters.Add(new FileSizeFormatter());
    }

    #region IMetaDataFormatterCollection Members

    /// <summary>
    /// Gets the list of all formatters registered
    /// </summary>
    /// <value>The formatters.</value>
    public List<IMetaDataFormatter> Formatters
    {
      get { return _formatters; }
    }

    /// <summary>
    /// Gets the formatter with the specified name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>the formatter for the name</returns>
    public IMetaDataFormatter Get(string name)
    {
      foreach (IMetaDataFormatter formatter in _formatters)
      {
        if (formatter.Name == name) return formatter;
      }
      return null;
    }

    /// <summary>
    /// Adds a new formatter
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="formatter">The formatter.</param>
    public void Add(string name, IMetaDataFormatter formatter)
    {
      _formatters.Add(formatter);
    }

    /// <summary>
    /// Determines whether the collection contains a formatter with the name specified
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    /// 	<c>true</c> if the collection contains a formatter with the name specified; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(string name)
    {
      foreach (IMetaDataFormatter formatter in _formatters)
      {
        if (formatter.Name == name) return true;
      }
      return false;
    }

    #endregion
  }
}
