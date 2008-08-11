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
using System.Xml.Serialization;
using MediaPortal.Core.ExtensionManager;

namespace MediaPortal.Services.ExtensionManager
{
  /// <summary>
  /// Implement the IMPIFileItem interface, store one file dat included in package
  /// </summary>
  [Serializable]
  public class ExtensionFileItem : IExtensionFileItem
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionFileItem"/> class.
    /// </summary>
    public ExtensionFileItem()
    {
      FileName = string.Empty;
      Action = string.Empty;
      Param1 = string.Empty;
      Param2 = string.Empty;
      Param3 = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionFileItem"/> class.
    /// </summary>
    /// <param name="file">The file name.</param>
    /// <param name="action">The action.</param>
    /// <param name="param1">The param1.</param>
    /// <param name="param2">The param2.</param>
    /// <param name="param3">The param3.</param>
    public ExtensionFileItem(string file, string action, string param1, string param2, string param3)
    {
      FileName = file;
      Action = action;
      Param1 = param1;
      Param2 = param2;
      Param3 = param3;
    }

    string _filename;
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    /// <value>The name of the file.</value>
    public string FileName
    {
      get
      {
        return _filename;
      }
      set
      {
        _filename = value;
      }
    }

    string _action;
    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    /// <value>The action.</value>
    [XmlAttribute()]
    public string Action
    {
      get
      {
        return _action;
      }
      set
      {
        _action = value;
      }
    }

    string _param1;
    /// <summary>
    /// Gets or sets the param1.
    /// </summary>
    /// <value>The value  param1.</value>
    [XmlAttribute()]
    public string Param1
    {
      get
      {
        return _param1;
      }
      set
      {
        _param1 = value;
      }
    }

    string _param2;
    /// <summary>
    /// Gets or sets the param2.
    /// </summary>
    /// <value>The value  param2.</value>
    [XmlAttribute()]
    public string Param2
    {
      get
      {
        return _param2;
      }
      set
      {
        _param2 = value;
      }
    }

    string _param3;
    /// <summary>
    /// Gets or sets the param3.
    /// </summary>
    /// <value>The value  param3.</value>
    [XmlAttribute()]
    public string Param3
    {
      get
      {
        return _param3;
      }
      set
      {
        _param3 = value;
      }
    }
   
  }
}
