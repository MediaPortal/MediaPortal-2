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
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;

namespace MediaPortal.PackageServer.Domain.Services.Core
{
  public abstract class ServiceBase
  {
    protected DataContext Context { get; private set; }

    protected ServiceBase(DataContext context)
    {
      Context = context;
    }

    public bool SaveChanges()
    {
      try
      {
        Context.SaveChanges();
        return true;
      }
      catch (DbEntityValidationException)
      {
        //Log.Error( "{0} while saving changes: {1}", ex.GetType().Name, ex.Message );
        //foreach( var entityError in ex.EntityValidationErrors.Where( e => !e.IsValid ) )
        //{
        //  Log.Error( "Entity validation failed for {0} (ID: {1})", entityError.Entry.Entity.GetType().Name, entityError.Entry.Entity.GetPropertyValue( "ID" ) );
        //  foreach( var validationError in entityError.ValidationErrors )
        //  {
        //    Log.Debug( "    Property {0}: {1}", validationError.PropertyName, validationError.ErrorMessage );
        //  }
        //}
        return false;
      }
      catch (Exception)
      {
        // TODO Log.Error( "{0} while saving changes: {1}", ex.GetType().Name, ex.Message );
        return false;
      }
    }

    protected virtual IQueryable<T> Query<T>(DbSet<T> entities, params Expression<Func<T, object>>[] includes) where T : class
    {
      IQueryable<T> query = entities;
      if (includes != null)
      {
        query = includes.Aggregate(query, (current, path) => path != null ? current.Include(path) : current);
      }
      return query;
    }
  }
}