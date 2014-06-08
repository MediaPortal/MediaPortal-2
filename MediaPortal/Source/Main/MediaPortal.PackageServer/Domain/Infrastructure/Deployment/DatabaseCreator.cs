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
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using MediaPortal.Common.General;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;
using MediaPortal.PackageServer.Domain.Infrastructure.Interfaces;

namespace MediaPortal.PackageServer.Domain.Infrastructure.Deployment
{
  public class DatabaseCreator
  {
    private const string PRODUCTION_DATABASE = "MediaPortal.PackageServer";
    private const string TEST_DATABASE = "MediaPortal.PackageServer.Test";

    public void CreateAndPopulateDatabase()
    {
      CreateAndPopulateDatabase(PRODUCTION_DATABASE, true);
    }

    public void CreateAndPopulateDatabaseForTest()
    {
      CreateAndPopulateDatabase(TEST_DATABASE, true);
    }

    #region Helpers

    public void CreateAndPopulateDatabase(string databaseName, bool overwrite)
    {
      if (CreateDatabase(databaseName, overwrite))
        PopulateDatabase(databaseName);
    }

    public bool CreateDatabase(string databaseName, bool overwrite)
    {
      databaseName = databaseName ?? TEST_DATABASE;
      using (var context = new DataContext(databaseName))
      {
        if (overwrite && context.Database.Exists())
        {
          context.Database.Delete();
        }
        var created = context.Database.CreateIfNotExists();
        Debug.Assert(context.Database.CompatibleWithModel(false));
        return created;
      }
    }

    public void PopulateDatabase(string databaseName)
    {
      databaseName = databaseName ?? TEST_DATABASE;
      using (var context = new DataContext(databaseName))
      {
        Debug.Assert(context.Database.Exists());
        Debug.Assert(context.Database.CompatibleWithModel(false));

        var contentProviders = GetType().Assembly.CreateInstances<IContentProvider>().OrderBy(cp => cp.Order);
        contentProviders.ForEach(p => p.CreateContent(context));

        var errors = context.GetValidationErrors();
        if (errors.Any(e => !e.IsValid))
          throw new ValidationException(string.Join(Environment.NewLine, errors.Select(e => string.Join("", e.ValidationErrors.SelectMany(ve => ve.ErrorMessage)))));

        context.SaveChanges();
      }
    }

    #endregion
  }
}