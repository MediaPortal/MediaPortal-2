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
using System.Reflection;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace Presentation.SkinEngine
{
  public class Model
  {
    #region variables

    private readonly string _assembly;
    private readonly string _className;
    private readonly Type _type;
    private readonly object _instance;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="className">Name of the class.</param>
    /// <param name="type">The type.</param>
    /// <param name="instance">The instance.</param>
    public Model(string assembly, string className, Type type, object instance)
    {
      _assembly = assembly;
      _className = className;
      _type = type;
      _instance = instance;
    }


    /// <summary>
    /// Gets the assembly.
    /// </summary>
    /// <value>The assembly.</value>
    public string Assembly
    {
      get { return _assembly; }
    }

    /// <summary>
    /// Gets the name of the class.
    /// </summary>
    /// <value>The name of the class.</value>
    public string ClassName
    {
      get { return _className; }
    }

    /// <summary>
    /// Gets the type.
    /// </summary>
    /// <value>The type.</value>
    public Type Type
    {
      get { return _type; }
    }

    /// <summary>
    /// returns an instance of the model
    /// </summary>
    /// <value>The instance.</value>
    public object Instance
    {
      get { return _instance; }
    }

    /// <summary>
    /// Invokes a property on the model
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns></returns>
    public object InvokePropertyGet(string propertyName)
    {
      try
      {
        string[] parts = propertyName.Split(new char[] {'.'});
        int partNr = 0;
        object obj = _instance;
        while (partNr < parts.Length)
        {
          obj =
            _type.InvokeMember(parts[partNr],
                               BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                               BindingFlags.GetProperty, Type.DefaultBinder, obj, null);
          partNr++;
        }
        return obj;
      }
      catch (Exception ex)
      {
        ILogger logger = ServiceScope.Get<ILogger>();
        logger.Error("ModelManager: Exception while calling {0}.{1}.{2}", Assembly, ClassName, propertyName);
        logger.Error(ex);
      }
      return null;
    }
  }
}
