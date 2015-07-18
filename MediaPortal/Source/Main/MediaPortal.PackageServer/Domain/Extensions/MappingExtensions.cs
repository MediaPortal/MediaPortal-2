#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MediaPortal.PackageServer.Domain.Extensions
{
  public static class MappingExtensions
  {
    public static void AddUniqueConstraints<T>(this Database database, params Expression<Func<T, object>>[] propertySelectors) where T : class
    {
      try
      {
        if (propertySelectors == null || propertySelectors.Length == 0)
        {
          throw new ArgumentException("Must specify at least one property to add a constraint for.", "uniqueProperties");
        }
        var properties = propertySelectors.Select(GetProperty).ToList();

        // TODO should check for pluralizing convention
        string tableName = typeof(T).Name;

        foreach (var property in properties)
        {
          string cmd = String.Format("ALTER TABLE [{0}] ADD CONSTRAINT uc_{0}_{1} UNIQUE({1})", tableName, property.Name);
          database.ExecuteSqlCommand(cmd);
        }
      }
      catch (SqlException)
      {
        // ignore errors, assume it is because constraint already exists
      }
    }

    public static void AddIndex<T>(this Database database, bool unique, params Expression<Func<T, object>>[] propertySelectors) where T : class
    {
      try
      {
        if (propertySelectors == null || propertySelectors.Length == 0)
        {
          throw new ArgumentException("Must specify at least one property when creating an index.", "propertySelectors");
        }
        var properties = propertySelectors.Select(GetProperty).ToList();

        // TODO should check for pluralizing convention
        string tableName = typeof(T).Name;

        string cmd = String.Format("CREATE {0}INDEX IX_{1}_{2} ON [{1}]( {3} )",
          unique ? "UNIQUE " : string.Empty,
          tableName,
          String.Join("_", properties.Select(p => p.Name)),
          String.Join(", ", properties.Select(p => p.Name)));
        database.ExecuteSqlCommand(cmd);
      }
      catch (SqlException)
      {
        // ignore errors, assume it is because index already exists
      }
    }

    private static PropertyInfo GetProperty(LambdaExpression lamdbaexpression)
    {
      var expr = (MemberExpression)lamdbaexpression.Body;
      return expr.Member as PropertyInfo;
    }
  }
}