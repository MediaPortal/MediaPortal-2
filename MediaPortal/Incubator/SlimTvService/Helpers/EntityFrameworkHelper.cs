#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace MediaPortal.Plugins.SlimTv.Service.Helpers
{
  class EntityFrameworkHelper
  {
    /// <summary>
    /// Makes sure that the given <paramref name="dbProviderFactory"/> is registered inside global <see cref="DbProviderFactories"/>.
    /// It queries the needed information from the runtime type and creates a new entry inside <c>system.data</c> configuration section.
    /// Additionally the path of the factory is added to <see cref="AppDomain.CurrentDomain"/> as "PrivatePath" to allow 
    /// proper assembly lookup inside subfolder.
    /// </summary>
    /// <param name="dbProviderFactory">DbProviderFactory instance</param>
    public static void AssureKnownFactory(DbProviderFactory dbProviderFactory)
    {
      var dataSet = ConfigurationManager.GetSection("system.data") as DataSet;
      if (dataSet == null)
        return;

      Type factoryType = dbProviderFactory.GetType();
      string invariantName = factoryType.Namespace;
      DataTable dt = dataSet.Tables[0];
      if (dt.Rows.Cast<DataRow>().Any(row => row["InvariantName"].ToString() == invariantName))
        return;

      dt.Rows.Add(invariantName, "Temporary added factory for EF use", invariantName, factoryType.AssemblyQualifiedName);
      AppDomain.CurrentDomain.AppendPrivatePath(Path.GetDirectoryName(factoryType.Assembly.Location));
    }
  }
}
