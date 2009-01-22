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

namespace MediaPortal.Configuration
{
  /// <summary>
  /// Setting metadata structure. Holds all values to describe a plugin's setting.
  /// </summary>
  public class ConfigSettingMetadata : ConfigBaseMetadata
  {
    protected string _className;
    protected string _helpText;
    protected ICollection<string> _listenTo;
    protected IDictionary<string, string> _additionalData = null;
    protected IDictionary<string, Type> _additionalTypes = null;

    public ConfigSettingMetadata(string location, string text, string className,
        string helpText, ICollection<string> listenTo) : base(location, text)
    {
      _className = className;
      _helpText = helpText;
      _listenTo = listenTo;
    }

    public string ClassName
    {
      get { return _className; }
    }

    public string HelpText
    {
      get { return _helpText; }
    }

    public ICollection<string> ListenTo
    {
      get { return _listenTo; }
    }

    /// <summary>
    /// Additional data used for complex settings.
    /// </summary>
    public IDictionary<string, string> AdditionalData
    {
      get { return _additionalData; }
      set { _additionalData = value; }
    }

    /// <summary>
    /// Additional types used for complex settings.
    /// </summary>
    public IDictionary<string, Type> AdditionalTypes
    {
      get { return _additionalTypes; }
      set { _additionalTypes = value; }
    }
  }
}