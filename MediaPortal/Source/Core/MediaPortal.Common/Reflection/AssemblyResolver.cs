#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MediaPortal.Common.Reflection
{
  /// <summary>
  /// Custom Assembly resolver class. It checks all dll inside the process folder including all subdirectories. When an assembly is requested,
  /// it will be checked against the list of found assemblies, but without checking the exact version numbers. This helps to avoid very many
  /// bindingRedirects inside app.config, especially if plugins bring in a long list of (maybe conflicting) dependencies.
  /// </summary>
  public static class AssemblyResolver
  {
    private static readonly IDictionary<string, string> _additional = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
    private static readonly IDictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>(StringComparer.CurrentCultureIgnoreCase);

    public static void RedirectAllAssemblies()
    {
      var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
      foreach (var assemblyName in Directory.GetFiles(dir, @"*.dll", SearchOption.AllDirectories))
      {
        var key = Path.GetFileNameWithoutExtension(assemblyName);
        if (key != null && !_additional.ContainsKey(key))
        {
          _additional.Add(key, assemblyName);
        }
      }

      AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ResolveAssembly;
      AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_ResolveAssembly;
    }

    private static Assembly CurrentDomain_ResolveAssembly(object sender, ResolveEventArgs e)
    {
      var name = e.Name.Contains(",") ? e.Name.Substring(0, e.Name.IndexOf(',')) : e.Name;
      if (_loadedAssemblies.TryGetValue(name, out Assembly assembly))
        return assembly;

      if (_additional.TryGetValue(name, out var fullPathToAssembly))
      {
        _loadedAssemblies[name] = assembly = Assembly.LoadFile(fullPathToAssembly);
        return assembly;
      }
      return null;
    }
  }
}
