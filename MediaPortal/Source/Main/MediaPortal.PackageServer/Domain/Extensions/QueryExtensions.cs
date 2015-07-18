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
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;

namespace MediaPortal.PackageServer.Domain.Extensions
{
  public static class QueryExtensions
  {
    /// <summary>
    ///   Extension method to allow for Contains-like queries using Entity Framework
    /// </summary>
    public static IQueryable<TEntity> WhereIn<TEntity, TValue>(this ObjectQuery<TEntity> query,
      Expression<Func<TEntity, TValue>> selector, IEnumerable<TValue> collection)
    {
      if (selector == null)
      {
        throw new ArgumentNullException("selector");
      }
      if (collection == null)
      {
        throw new ArgumentNullException("collection");
      }
      if (!collection.Any())
      {
        return query;
      }

      ParameterExpression p = selector.Parameters.Single();
      IEnumerable<Expression> equals = collection.Select(value =>
        (Expression)Expression.Equal(selector.Body, Expression.Constant(value, typeof(TValue))));
      Expression body = equals.Aggregate(Expression.Or);
      return query.Where(Expression.Lambda<Func<TEntity, bool>>(body, p));
    }

    public static IQueryable<TEntity> WhereNotIn<TEntity, TValue>(this ObjectQuery<TEntity> query,
      Expression<Func<TEntity, TValue>> selector, IEnumerable<TValue> collection)
    {
      if (selector == null)
      {
        throw new ArgumentNullException("selector");
      }
      if (collection == null)
      {
        throw new ArgumentNullException("collection");
      }
      if (!collection.Any())
      {
        return query;
      }

      ParameterExpression p = selector.Parameters.Single();
      IEnumerable<Expression> equals = collection.Select(value =>
        (Expression)Expression.NotEqual(selector.Body, Expression.Constant(value, typeof(TValue))));
      Expression body = equals.Aggregate(Expression.And);
      return query.Where(Expression.Lambda<Func<TEntity, bool>>(body, p));
    }
  }
}