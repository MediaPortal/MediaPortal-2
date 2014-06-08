#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Linq;
using System.Reflection;
using MediaPortal.Common.General;

namespace MediaPortal.PackageServer.Initialization.Core
{
  public static class ConfigurationTaskRunner
  {
    public static void Execute()
    {
      var assemblies = new[] { Assembly.GetExecutingAssembly() };
      Execute(assemblies);
    }

    public static void Execute(params Type[] assemblyTypes)
    {
      var assemblies = assemblyTypes.Select(Assembly.GetAssembly).Distinct().ToList();
      Execute(assemblies);
    }

    public static void Execute(params string[] assemblyNames)
    {
      var assemblies = assemblyNames.Select(Assembly.Load).Distinct().ToList();
      Execute(assemblies);
    }

    public static void Execute(IEnumerable<Assembly> assemblies)
    {
      var tasks = assemblies.SelectMany(a => a.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.Implements<IConfigurationTask>() && t.GetConstructor(new Type[0]) != null)
        .Select(Activator.CreateInstance)
        .Cast<IConfigurationTask>()).ToList();

      var priorityTasks = tasks.Where(t => t is IPrioritizedConfigurationTask).Cast<IPrioritizedConfigurationTask>().OrderBy(t => t.Priority);
      var otherTasks = tasks.Where(t => ! (t is IPrioritizedConfigurationTask));
      priorityTasks.ForEach(t => t.Configure());
      otherTasks.ForEach(t => t.Configure());
    }
  }
}