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

using System.Configuration;
using System.Data.Entity;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;
using MediaPortal.PackageServer.Domain.Infrastructure.Deployment;
using MediaPortal.PackageServer.Initialization.Core;

namespace MediaPortal.PackageServer.Initialization
{
  public class EntityFrameworkConfig : IPrioritizedConfigurationTask
  {
    #region IPrioritizedConfigurationTask

    public void Configure()
    {
      // this makes EF bypass checking metadata and schema compatibility on startup
      Database.SetInitializer(new DataContextInitializer());

      // ensure we have an database (populated with initial content)
      var databaseName = ConfigurationManager.AppSettings["db.name"] ?? "MediaPortal.PackageServer";
      new DatabaseCreator().CreateAndPopulateDatabase(databaseName, overwrite: false);
    }

    public int Priority
    {
      get { return (int)TaskPriority.Database; }
    }

    #endregion
  }
}