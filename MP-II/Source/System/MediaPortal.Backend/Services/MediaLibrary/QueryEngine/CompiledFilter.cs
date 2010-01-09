#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  public class CompiledFilter
  {
    protected IList<object> _statementParts;
    protected IList<object> _statementValues;

    /// <summary>
    /// Placeholder object which will be replaced by method <see cref="CreateSqlFilterCondition"/> with the
    /// final outer join variable (or alias).
    /// </summary>
    protected object _outerMIIDJoinVariablePlaceHolder;

    protected readonly ICollection<QueryAttribute> _filterAttributes;

    public CompiledFilter(IList<object> statementParts, IList<object> statementValues, ICollection<QueryAttribute> filterAttributes,
        object outerMIIDJoinVariablePlaceHolder)
    {
      _statementParts = statementParts;
      _statementValues = statementValues;
      _filterAttributes = filterAttributes;
      _outerMIIDJoinVariablePlaceHolder = outerMIIDJoinVariablePlaceHolder;
    }

    public static CompiledFilter Compile(MIA_Management miaManagement, IFilter filter)
    {
      object outerMIIDJoinVariablePlaceHolder = new object();
      IList<object> statementParts = new List<object>();
      IList<object> statementValues = new List<object>();
      CompileStatementParts(miaManagement, filter, outerMIIDJoinVariablePlaceHolder, statementParts, statementValues);
      ICollection<QueryAttribute> filterAttributes = new List<QueryAttribute>();
      foreach (object statementPart in statementParts)
      {
        QueryAttribute qa = statementPart as QueryAttribute;
        if (qa != null)
          filterAttributes.Add(qa);
      }
      return new CompiledFilter(statementParts, statementValues, filterAttributes, outerMIIDJoinVariablePlaceHolder);
    }

    protected static void CompileStatementParts(MIA_Management miaManagement, IFilter filter,
        object outerMIIDJoinVariablePlaceHolder, IList<object> resultParts, IList<object> resultValues)
    {
      if (filter == null)
        return;

      BooleanCombinationFilter boolFilter = filter as BooleanCombinationFilter;
      if (boolFilter != null)
      {
        int numOperands = boolFilter.Operands.Length;
        IEnumerator enumOperands = boolFilter.Operands.GetEnumerator();
        if (!enumOperands.MoveNext())
          return;
        if (numOperands > 1)
          resultParts.Add("(");
        CompileStatementParts(miaManagement, (IFilter) enumOperands.Current, outerMIIDJoinVariablePlaceHolder, resultParts, resultValues);
        while (enumOperands.MoveNext())
        {
          switch (boolFilter.Operator)
          {
            case BooleanOperator.And:
              resultParts.Add(" AND ");
              break;
            case BooleanOperator.Or:
              resultParts.Add(" OR ");
              break;
            default:
              throw new NotImplementedException(string.Format(
                  "Boolean filter operator '{0}' isn't supported by the media library", boolFilter.Operator));
          }
          CompileStatementParts(miaManagement, (IFilter)enumOperands.Current, outerMIIDJoinVariablePlaceHolder, resultParts, resultValues);
        }
        if (numOperands > 1)
          resultParts.Add(")");
        return;
      }

      NotFilter notFilter = filter as NotFilter;
      if (notFilter != null)
      {
        resultParts.Add("NOT (");
        CompileStatementParts(miaManagement, notFilter.InnerFilter, outerMIIDJoinVariablePlaceHolder, resultParts, resultValues);
        resultParts.Add(")");
        return;
      }

      // Must be done before checking IAttributeFilter - EmptyFilter is also an IAttributeFilter but must be
      // compiled in a different way
      EmptyFilter emptyFilter = filter as EmptyFilter;
      if (emptyFilter != null)
      {
        MediaItemAspectMetadata.AttributeSpecification attributeType = emptyFilter.AttributeType;
        Cardinality cardinality = attributeType.Cardinality;
        if (cardinality == Cardinality.Inline || cardinality == Cardinality.ManyToOne)
        {
          resultParts.Add(new QueryAttribute(attributeType));
          resultParts.Add(" IS NULL"); // MTO attributes are joined with left outer joins and thus can also be checked for NULL
        }
        else if (cardinality == Cardinality.OneToMany)
        {
          resultParts.Add("NOT EXISTS(");
          resultParts.Add("SELECT VAL.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add(" FROM ");
          resultParts.Add(miaManagement.GetMIACollectionAttributeTableName(attributeType));
          resultParts.Add(" VAL WHERE VAL.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add("=");
          resultParts.Add(outerMIIDJoinVariablePlaceHolder);
          resultParts.Add(")");
        }
        else if (cardinality == Cardinality.ManyToMany)
        {
          resultParts.Add("NOT EXISTS(");
          resultParts.Add("SELECT NM.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add(" FROM ");
          resultParts.Add(miaManagement.GetMIACollectionAttributeNMTableName(attributeType));
          resultParts.Add(" NM INNER JOIN ");
          resultParts.Add(miaManagement.GetMIACollectionAttributeTableName(attributeType));
          resultParts.Add(" VAL ON NM.");
          resultParts.Add(MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME);
          resultParts.Add(" = VAL.");
          resultParts.Add(MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME);
          resultParts.Add(" WHERE NM.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add("=");
          resultParts.Add(outerMIIDJoinVariablePlaceHolder);
          resultParts.Add(")");
        }
        return;
      }

      IAttributeFilter attributeFilter = filter as IAttributeFilter;
      if (attributeFilter != null)
      {
        // For attribute filters, we have to create different kinds of expressions, depending on the
        // cardinality of the attribute to be filtered.
        // For Inline and MTO attributes, we simply create
        //
        // QA [Operator] [Comparison-Value]
        //
        // for OTM attributes, we create
        //
        // EXISTS(
        //  SELECT VAL.MEDIA_ITEM_ID
        //  FROM [OTM-Value-Table] VAL
        //  WHERE VAL.MI_ID=[Outer-Join-Variable-Placeholder] AND VAL.VALUE [Operator] [Comparison-Value])
        //
        // for MTM attributes, we create
        //
        // EXISTS(
        //  SELECT NM.MEDIA_ITEM_ID
        //  FROM [MTM-NM-Table] NM
        //  INNER JOIN [MTM-Value-Table] VAL ON NM.ID = VAL.ID
        //  WHERE NM.MI_ID=[Outer-Join-Variable-Placeholder] AND VAL.VALUE [Operator] [Comparison-Value])

        MediaItemAspectMetadata.AttributeSpecification attributeType = attributeFilter.AttributeType;
        Cardinality cardinality = attributeType.Cardinality;
        if (cardinality == Cardinality.Inline || cardinality == Cardinality.ManyToOne)
          BuildAttributeFilterExpression(attributeFilter, new QueryAttribute(attributeType), resultParts, resultValues);
        else if (cardinality == Cardinality.OneToMany)
        {
          resultParts.Add("EXISTS(");
          resultParts.Add("SELECT VAL.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add(" FROM ");
          resultParts.Add(miaManagement.GetMIACollectionAttributeTableName(attributeType));
          resultParts.Add(" VAL WHERE VAL.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add("=");
          resultParts.Add(outerMIIDJoinVariablePlaceHolder);
          resultParts.Add(" AND ");
          BuildAttributeFilterExpression(attributeFilter, "VAL." + MIA_Management.COLL_ATTR_VALUE_COL_NAME, resultParts, resultValues);
          resultParts.Add(")");
        }
        else if (cardinality == Cardinality.ManyToMany)
        {
          resultParts.Add("EXISTS(");
          resultParts.Add("SELECT NM.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add(" FROM ");
          resultParts.Add(miaManagement.GetMIACollectionAttributeNMTableName(attributeType));
          resultParts.Add(" NM INNER JOIN ");
          resultParts.Add(miaManagement.GetMIACollectionAttributeTableName(attributeType));
          resultParts.Add(" VAL ON NM.");
          resultParts.Add(MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME);
          resultParts.Add(" = VAL.");
          resultParts.Add(MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME);
          resultParts.Add(" WHERE NM.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add("=");
          resultParts.Add(outerMIIDJoinVariablePlaceHolder);
          resultParts.Add(" AND ");
          BuildAttributeFilterExpression(attributeFilter, "VAL." + MIA_Management.COLL_ATTR_VALUE_COL_NAME, resultParts, resultValues);
          resultParts.Add(")");
        }
        return;
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
    /// <param name="resultParts">Statement parts for the attribute filter.</param>
    /// <param name="resultValues">Statement values for the attribute filter.</param>
    protected static void BuildAttributeFilterExpression(IAttributeFilter filter, object attributeOperand,
        IList<object> resultParts, IList<object> resultValues)
    {
      RelationalFilter relationalFilter = filter as RelationalFilter;
      if (relationalFilter != null)
      {
        resultParts.Add(attributeOperand);
        switch (relationalFilter.Operator)
        {
          case RelationalOperator.EQ:
            resultParts.Add("=");
            break;
          case RelationalOperator.NEQ:
            resultParts.Add("<>");
            break;
          case RelationalOperator.LT:
            resultParts.Add("<");
            break;
          case RelationalOperator.LE:
            resultParts.Add("<=");
            break;
          case RelationalOperator.GT:
            resultParts.Add(">");
            break;
          case RelationalOperator.GE:
            resultParts.Add(">=");
            break;
          default:
            throw new NotImplementedException(string.Format(
                "Relational filter operator '{0}' isn't supported by the media library", relationalFilter.Operator));
        }
        resultParts.Add("?");
        resultValues.Add(relationalFilter.FilterValue);
        return;
      }

      LikeFilter likeFilter = filter as LikeFilter;
      if (likeFilter != null)
      {
        resultParts.Add(attributeOperand);
        resultParts.Add(" LIKE ?");
        resultValues.Add(likeFilter.Expression);
        resultParts.Add(" ESCAPE '");
        resultParts.Add(likeFilter.EscapeChar);
        resultParts.Add("'");
        return;
      }

      SimilarToFilter similarToFilter = filter as SimilarToFilter;
      if (similarToFilter != null)
      {
        resultParts.Add(attributeOperand);
        resultParts.Add(" SIMILAR TO ?");
        resultValues.Add(similarToFilter.Expression);
        resultParts.Add(" ESCAPE '");
        resultParts.Add(similarToFilter.EscapeChar);
        resultParts.Add("'");
        return;
      }

      BetweenFilter betweenFilter = filter as BetweenFilter;
      if (betweenFilter != null)
      {
        resultParts.Add(attributeOperand);
        resultParts.Add(" BETWEEN ?");
        resultValues.Add(betweenFilter.Value1);
        resultParts.Add(" AND ?");
        resultValues.Add(betweenFilter.Value2);
        return;
      }

      InFilter inFilter = filter as InFilter;
      if (inFilter != null)
      {
        resultParts.Add(attributeOperand);
        resultParts.Add(" IN (");
        IEnumerator valueEnum = inFilter.Values.GetEnumerator();
        if (!valueEnum.MoveNext())
          throw new InvalidDataException("IN-filter doesn't provide any comparison values");
        resultParts.Add("?");
        resultValues.Add(valueEnum.Current);
        while (valueEnum.MoveNext())
        {
          resultParts.Add(",?");
          resultValues.Add(valueEnum.Current);
        }
        resultParts.Add(")");
        return;
      }
      throw new InvalidDataException("Filter type '{0}' isn't supported by the media library", filter.GetType().Name);
    }

    public ICollection<QueryAttribute> FilterAttributes
    {
      get { return _filterAttributes; }
    }

    // outerMIIDJoinVariable is MEDIA_ITEMS.MEDIA_ITEM_ID (or its alias) for simple selects,
    // MIAM_TABLE_XXX.MEDIA_ITEM_ID (or alias) for complex selects, used for join conditions in complex filters
    public void CreateSqlFilterCondition(Namespace ns,
        IDictionary<QueryAttribute, RequestedAttribute> requestedAttributes,
        string outerMIIDJoinVariable, out string filterStr, out IList<object> values)
    {
      StringBuilder filterBuilder = new StringBuilder(1000);
      foreach (object statementPart in _statementParts)
      {
        QueryAttribute qa = statementPart as QueryAttribute;
        if (qa != null)
          filterBuilder.Append(requestedAttributes[qa].GetQualifiedName(ns));
        else if (statementPart == _outerMIIDJoinVariablePlaceHolder)
          filterBuilder.Append(outerMIIDJoinVariable);
        else
          filterBuilder.Append(statementPart.ToString());
      }
      filterStr = filterBuilder.ToString();
      values = _statementValues;
    }
  }
}
