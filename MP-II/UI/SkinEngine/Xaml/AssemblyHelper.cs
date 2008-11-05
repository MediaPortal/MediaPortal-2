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
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using MediaPortal.SkinEngine.Xaml.Exceptions;

namespace MediaPortal.SkinEngine.Xaml
{
  /// <summary>
  /// Helper class for assembly methods.
  /// </summary>
  public class AssemblyHelper
  {
    protected static IDictionary<string, Assembly> _loadedAssemblies =
      new Dictionary<string, Assembly>();

    /// <summary>
    /// Finds the assembly with the specified <paramref name="name"/>. This method
    /// searches all assemblies currently loaded in the current
    /// <see cref="AppDomain.CurrentDomain">application domain</see>. If the specified
    /// assembly was not loaded yet, this method tries to find the assembly dll in the
    /// directory of the mscorlib assembly.
    /// </summary>
    /// <param name="name">Short name of the assembly to load.</param>
    /// <returns>Assembly with the specified sohrt name.</returns>
    /// <exception cref="XamlLoadException">If the assembly with the specified name
    /// was not found.</exception>
    public static Assembly LoadAssembly(string name)
    {
      AssemblyName assemblyName = new AssemblyName(name);
      string assemblyShortName = assemblyName.Name;
      assemblyShortName = assemblyShortName.ToUpper(CultureInfo.InvariantCulture);

      if (_loadedAssemblies.ContainsKey(assemblyShortName))
        return _loadedAssemblies[assemblyShortName];

      Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
      foreach (Assembly ass in assemblies)
      {
        if (String.Compare(ass.GetName().Name, assemblyShortName, StringComparison.OrdinalIgnoreCase) == 0)
        {
          _loadedAssemblies[assemblyShortName] = ass;
          return ass;
        }
      }

      string fullpath = Path.GetDirectoryName(typeof(string).Assembly.Location) + Path.DirectorySeparatorChar + name + ".dll"; // Find core assembly
      if (File.Exists(fullpath))
      {
        Assembly ass = Assembly.LoadFile(fullpath);
        _loadedAssemblies[assemblyShortName] = ass;
        return ass;
      }

      throw new XamlLoadException("Assembly '{0}' not found", name);
    }
  }
}
