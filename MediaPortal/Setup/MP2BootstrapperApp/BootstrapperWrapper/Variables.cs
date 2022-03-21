#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  /// <summary>
  /// Implementation of <see cref="IVariables{T}"/> that wraps an instance of <see cref="Engine.Variables{T}"/>.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class Variables<T> : IVariables<T>
  {
    protected Engine.Variables<T> _variables;

    public Variables(Engine.Variables<T> variables)
    {
      _variables = variables;
    }

    public T this[string name]
    {
      get { return _variables[name]; }
      set { _variables[name] = value; }
    }

    public bool Contains(string name)
    {
      return _variables.Contains(name);
    }
  }
}
