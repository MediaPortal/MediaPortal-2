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

namespace MediaPortal.SkinEngine.Models
{
  /// <summary>
  /// Encapsulates a model instance from the system to be used by skins. This is a data object which
  /// stores model metadata together with a model instance.
  /// </summary>
  public class Model
  {
    #region Protected fields

    protected readonly string _assembly;
    protected readonly string _className;
    protected readonly object _instance;

    #endregion

    public Model(string assembly, string className, object instance)
    {
      if (className == null)
        throw new ArgumentException("The model classname mustn't be null");
      if (instance == null)
        throw new ArgumentException("The model instance mustn't be null");
      _assembly = assembly;
      _className = className;
      _instance = instance;
    }


    public string Assembly
    {
      get { return _assembly; }
    }

    public string ClassName
    {
      get { return _className; }
    }

    public Type Type
    {
      get { return _instance.GetType(); }
    }

    public object Instance
    {
      get { return _instance; }
    }
  }
}
