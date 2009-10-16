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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Services.MediaLibrary.QueryEngine
{
  public class CompiledFilter
  {
    protected IList<object> _statementParts;

    /// <summary>
    /// Placeholder object which will be replaced by method <see cref="CreateSqlFilterCondition"/> with the
    /// final outer join variable (or alias).
    /// </summary>
    protected object _outerMIIDJoinVariablePlaceHolder;

    protected ICollection<QueryAttribute> _filterAttributes;

    public CompiledFilter(IList<object> statementParts, ICollection<QueryAttribute> filterAttributes,
        object outerMIIDJointVariablePlaceHolder)
    {
      _statementParts = statementParts;
      _filterAttributes = filterAttributes;
      _outerMIIDJoinVariablePlaceHolder = outerMIIDJointVariablePlaceHolder;
    }

    public static CompiledFilter Compile(MIAM_Management miamManagement, IFilter filter)
    {
      object outerMIIDJointVariablePlaceHolder = new object();
      IList<object> statementParts = CompileStatementParts(miamManagement, filter, outerMIIDJointVariablePlaceHolder);
      ICollection<QueryAttribute> filterAttributes = new List<QueryAttribute>();
      foreach (object statementPart in statementParts)
      {
        QueryAttribute qa = statementPart as QueryAttribute;
        if (qa != null)
          filterAttributes.Add(qa);
      }
      return new CompiledFilter(statementParts, filterAttributes, outerMIIDJointVariablePlaceHolder);
    }

    protected static IList<object> CompileStatementParts(MIAM_Management miamManagement, IFilter filter,
        object outerMIIDJointVariablePlaceHolder)
    {
      if (filter == null)
        return new List<object>();

      BooleanCombinationFilter boolFilter = filter as BooleanCombinationFilter;
      if (boolFilter != null)
      {
        IList<object> result = new List<object>();
        IEnumerator enumOperands = boolFilter.Operands.GetEnumerator();
        if (!enumOperands.MoveNext())
          return result;
        result.Add("(");
        result.Add(enumOperands.Current);
        while (enumOperands.MoveNext())
        {
          switch (boolFilter.Operator)
          {
            case BooleanOperator.And:
              result.Add(" AND ");
              break;
            case BooleanOperator.Or:
              result.Add(" OR ");
              break;
            default:
              throw new NotImplementedException(string.Format(
                  "Boolean filter operator '{0}' isn't supported by the media library", boolFilter.Operator));
          }
          result.Add(CompileStatementParts(miamManagement, (IFilter) enumOperands.Current, outerMIIDJointVariablePlaceHolder));
        }
        result.Add(")");
        return result;
      }

      NotFilter notFilter = filter as NotFilter;
      if (notFilter != null)
      {
        IList<object> result = new List<object>
        {
          "NOT (",
          CompileStatementParts(miamManagement, notFilter.InnerFilter, outerMIIDJointVariablePlaceHolder),
          ")"
        };
        return result;
      }

      IAttributeFilter attributeFilter = filter as IAttributeFilter;
      if (attributeFilter != null)
      {
        // For attribute filters, we have to create different kinds of expressions, depending on the
        // cardinality of the attribute to be filtered.
        // For inline attributes, we simply create
        //
        // QA [Operator] [Comparison-Value]
        //
        // while for complex attributes, we create
        //
        // EXISTS(
        //  SELECT COLL_MIA_TABLE.MEDIA_ITEM_ID
        //  FROM [Complex-Attribute-Table] COLL_MIA_TABLE
        //  WHERE COLL_MIA_TABLE.MI_ID=[Outer-Join-Variable-Placeholder] AND COLL_MIA_TABLE.VALUE [Operator] [Comparison-Value])

        IList<object> result = new List<object>();
        object attributeOperand;
        MediaItemAspectMetadata.AttributeSpecification attributeType = attributeFilter.AttributeType;
        if (attributeType.Cardinality == Cardinality.Inline)
          attributeOperand = new QueryAttribute(attributeType);
        else
        {
          result.Add("EXISTS(");
          result.Add(" SELECT COLL_MIA_TABLE.");
          result.Add(MIAM_Management.MIAM_MEDIA_ITEM_ID_COL_NAME);
          result.Add(" FROM ");
          result.Add(miamManagement.GetMIAMCollectionAttributeTableName(attributeType));
          result.Add(" COLL_MIA_TABLE WHERE COLL_MIA_TABLE.");
          result.Add(MIAM_Management.MIAM_MEDIA_ITEM_ID_COL_NAME);
          result.Add("=");
          result.Add(outerMIIDJointVariablePlaceHolder);
          result.Add(" AND ");
          attributeOperand = "COLL_MIA_TABLE." + MIAM_Management.COLL_MIAM_VALUE_COL_NAME;
        }
        CollectionUtils.AddAll(result, BuildAttributeFilterExpression(attributeFilter, attributeOperand));
        if (attributeType.Cardinality != Cardinality.Inline)
          result.Add(")");
        return result;
      }
      throw new InvalidDataException("Filter type '{0}' isn't supported by the media library", filter.GetType().Name);
    }

    /// <summary>
    /// Builds the actual filter SQL expression <c>[Attribute-Operand] [Operator] [Comparison-Value]</c> for the given
    /// attribute <paramref name="filter"/>.
    /// </summary>
    /// <param name="filter">Attribute filter instance to create the SQL expression for.</param>
    /// <param name="attributeOperand">Comparison attribute to be used. Depending on the cardinality of the
    /// to-be-filtered attribute, this will be the inline attribute alias or the attribute alias of the collection
    /// attribute table.</param>
    protected static IList<object> BuildAttributeFilterExpression(IAttributeFilter filter, object attributeOperand)
    {
      IList<object> result = new List<object>();
      RelationalFilter relationalFilter = filter as RelationalFilter;
      if (relationalFilter != null)
      {
        result.Add(attributeOperand);
        switch (relationalFilter.Operator)
        {
          case RelationalOperator.EQ:
            result.Add("=");
            break;
          case RelationalOperator.NEQ:
            result.Add("<>");
            break;
          case RelationalOperator.LT:
            result.Add("<");
            break;
          case RelationalOperator.LE:
            result.Add("<=");
            break;
          case RelationalOperator.GT:
            result.Add(">");
            break;
          case RelationalOperator.GE:
            result.Add(">=");
            break;
          default:
            throw new NotImplementedException(string.Format(
                "Relational filter operator '{0}' isn't supported by the media library", relationalFilter.Operator));
        }
        result.Add(relationalFilter.FilterValue);
      }

      LikeFilter likeFilter = filter as LikeFilter;
      if (likeFilter != null)
      {
        result.Add(attributeOperand);
        result.Add(" LIKE ");
        result.Add(likeFilter.LikeExpression);
        result.Add(" ESCAPE '");
        result.Add(likeFilter.EscapeChar);
        result.Add("'");
      }

      BetweenFilter betweenFilter = filter as BetweenFilter;
      if (betweenFilter != null)
      {
        result.Add(attributeOperand);
        result.Add(" BETWEEN ");
        result.Add(betweenFilter.Value1);
        result.Add(" AND ");
        result.Add(betweenFilter.Value2);
      }

      InFilter inFilter = filter as InFilter;
      if (inFilter != null)
      {
        result.Add(attributeOperand);
        result.Add(" IN (");
        IEnumerator valueEnum = inFilter.Values.GetEnumerator();
        if (!valueEnum.MoveNext())
          throw new InvalidDataException("IN-filter doesn't provide any comparison values");
        result.Add(valueEnum.Current);
        while (valueEnum.MoveNext())
        {
          result.Add(",");
          result.Add(valueEnum.Current);
        }
        result.Add(")");
      }
      throw new InvalidDataException("Filter type '{0}' isn't supported by the media library", filter.GetType().Name);
    }

    public ICollection<QueryAttribute> FilterAttributes
    {
      get { return _filterAttributes; }
    }

    // outerMIIDJoinVariable is MEDIA_ITEMS.MEDIA_ITEM_ID (or its alias) for simple selects,
    // MIAM_TABLE_XXX.MEDIA_ITEM_ID (or alias) for complex selects, used for join conditions in complex filters
    public string CreateSqlFilterCondition(Namespace ns,
        IDictionary<QueryAttribute, CompiledQueryAttribute> compiledAttributes,
        string outerMIIDJoinVariable)
    {
      StringBuilder result = new StringBuilder(1000);
      foreach (object statementPart in _statementParts)
      {
        QueryAttribute qa = statementPart as QueryAttribute;
        if (qa != null)
          result.Append(compiledAttributes[qa].GetAlias(ns));
        else if (statementPart == _outerMIIDJoinVariablePlaceHolder)
          result.Append(statementPart.ToString());
      }
      return result.ToString();
    }
  }
}
