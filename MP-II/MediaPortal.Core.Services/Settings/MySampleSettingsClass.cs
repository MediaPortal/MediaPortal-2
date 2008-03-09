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
using ProjectInfinity.Settings;

namespace ProjectInfinity.Settings
{
  /// <summary>
  /// Sample settings class wich will implement your own settings object in your code/plugin
  /// Only public properties are stored/retrieved
  /// </summary>
  public class MySampleSettingsClass 
  {
    private int _myInt;
    private string _myString;
    private string _anotherString;
    private List<int> _alist = new List<int>();

    /// <summary>
    /// Default Ctor
    /// </summary>
    public MySampleSettingsClass()
    {
    }
    /// <summary>
    /// Scope and default value attribute
    /// </summary>
    [Setting(SettingScope.Global,"55555")]
    public int MyInt
    {
      get { return this._myInt; }
      set { this._myInt = value; }
    }
    [Setting(SettingScope.User,"myStringDefaultValue")]
    public string MyString
    {
      get { return this._myString; }
      set { this._myString = value; }
    }
    [Setting(SettingScope.User, "anotherStringDefaultValue")]
    public string AnotherString
    {
      get { return this._anotherString; }
      set { this._anotherString = value; }
    }
    [Setting(SettingScope.User, "")]
    public List<int> AList
    {
      get { return this._alist; }
      set { this._alist = value; }
    }
  }
}
