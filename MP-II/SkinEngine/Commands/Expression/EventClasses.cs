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

namespace MediaPortal.SkinEngine.Commands.Expression
{
  #region "Event Handling Objects"

  /// <summary>
  /// Event Handling for Additional Functions
  /// </summary>
  public class AdditionalFunctionEventArgs : EventArgs
  {
    private string _name = "";
    private object[] _parameters;
    private object _return;

    /// <summary>
    /// This is the only constructor
    /// </summary>
    /// <param name="name">the Name of the function</param>
    /// <param name="a_params"></param>
    public AdditionalFunctionEventArgs(string name, object[] parameters)
    {
      _name = name;
      _parameters = parameters;
    }

    /// <summary>
    /// This is the name of the additional function
    /// </summary>
    public string Name { get { return _name; } }

    public object ReturnValue
    {
      get { return _return; }
      set { _return = value; }
    }

    /// <summary>
    /// This method will return an array of objects that are parameters.
    /// </summary>
    /// <returns>object array of function parameters</returns>
    public object[] GetParameters()
    {
      if (_parameters == null)
        return null;

      object[] ret = new object[_parameters.Length];
      Array.Copy(_parameters, ret, ret.Length);
      return ret;
    }
  }
  public class AdditionalVariableEventArgs : EventArgs
  {
    private string _name = "";
    private object[] _parameters;
    private object _return;

    /// <summary>
    /// This is the only constructor
    /// </summary>
    /// <param name="name">the Name of the function</param>
    /// <param name="a_params"></param>
    public AdditionalVariableEventArgs(string name, object[] parameters)
    {
      _name = name;
      _parameters = parameters;
    }

    /// <summary>
    /// This is the name of the additional function
    /// </summary>
    public string Name { get { return _name; } }

    public object ReturnValue
    {
      get { return _return; }
      set { _return = value; }
    }

    /// <summary>
    /// This method will return an array of objects that are parameters.
    /// </summary>
    /// <returns>object array of function parameters</returns>
    public object[] GetParameters()
    {
      if (_parameters == null)
        return null;

      object[] ret = new object[_parameters.Length];
      Array.Copy(_parameters, ret, ret.Length);
      return ret;
    }
  }
  #endregion

}
