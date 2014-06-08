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

using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;
using MediaPortal.PackageServer.Domain.Infrastructure.Interfaces;

namespace MediaPortal.PackageServer.Domain.Infrastructure.ContentProviders
{
  internal abstract class AbstractContentProvider : IContentProvider
  {
    public int Order { get; private set; }

    protected AbstractContentProvider(int order)
    {
      Order = order;
    }

    public abstract void CreateContent(DataContext context);

    public virtual void SaveChanges(DataContext context)
    {
      try
      {
        context.SaveChanges();
      }
      catch (DbEntityValidationException ex)
      {
        foreach (var errors in ex.EntityValidationErrors.Where(e => !e.IsValid))
        {
          foreach (var error in errors.ValidationErrors)
          {
            Debug.Write(string.Format("{0}: {1}", error.PropertyName, error.ErrorMessage));
          }
        }
        throw;
      }
    }
  }
}