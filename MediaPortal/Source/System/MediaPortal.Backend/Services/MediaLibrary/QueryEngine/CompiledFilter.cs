#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  public class CompiledFilter
  {
    #region Constants

    public const int MAX_IN_VALUES_SIZE = 800;

    #endregion

    protected IList<object> _statementParts;
    protected IList<BindVar> _statementBindVars;

    protected int _bindVarCounter = 0;

    /// <summary>
    /// Placeholder object which will be replaced by method <see cref="CreateSqlFilterCondition"/> with the
    /// final outer join variable (or alias).
    /// </summary>
    protected object _outerMIIDJoinVariablePlaceHolder;

    protected readonly ICollection<QueryAttribute> _filterAttributes;

    public CompiledFilter(MIA_Management miaManagement, IFilter filter, object outerMIIDJoinVariablePlaceHolder)
    {
      _statementParts = new List<object>();
      _statementBindVars = new List<BindVar>();
      CompileStatementParts(miaManagement, filter, outerMIIDJoinVariablePlaceHolder, _statementParts, _statementBindVars);
      ICollection<QueryAttribute> filterAttributes = new List<QueryAttribute>();
      foreach (object statementPart in _statementParts)
      {
        QueryAttribute qa = statementPart as QueryAttribute;
        if (qa != null)
          filterAttributes.Add(qa);
      }

      _filterAttributes = filterAttributes;
      _outerMIIDJoinVariablePlaceHolder = outerMIIDJoinVariablePlaceHolder;
    }

    public static CompiledFilter Compile(MIA_Management miaManagement, IFilter filter)
    {
      object outerMIIDJoinVariablePlaceHolder = new object();
      return new CompiledFilter(miaManagement, filter, outerMIIDJoinVariablePlaceHolder);
    }

    protected void CompileStatementParts(MIA_Management miaManagement, IFilter filter,
        object outerMIIDJoinVariablePlaceHolder, IList<object> resultParts, IList<BindVar> resultBindVars)
    {
      if (filter == null)
        return;

      MediaItemIdFilter mediaItemIdFilter = filter as MediaItemIdFilter;
      if (mediaItemIdFilter != null)
      {
        ICollection<Guid> mediaItemIds = mediaItemIdFilter.MediaItemIds;
        if (mediaItemIds.Count == 0)
          resultParts.Add("1 = 2");
        else
        {
          resultParts.Add(outerMIIDJoinVariablePlaceHolder);
          if (mediaItemIds.Count == 1)
          {
            BindVar bindVar = new BindVar("V" + _bindVarCounter++, mediaItemIds.First(), typeof(Guid));
            resultParts.Add(" = @" + bindVar.Name);
            resultBindVars.Add(bindVar);
          }
          else
          {
            ICollection<string> clusterExpressions = new List<string>();
            foreach (IList<Guid> mediaItemIdsCluster in CollectionUtils.Cluster(mediaItemIds, MAX_IN_VALUES_SIZE))
            {
              IList<string> bindVarRefs = new List<string>(MAX_IN_VALUES_SIZE);
              foreach (Guid mediaItemId in mediaItemIdsCluster)
              {
                BindVar bindVar = new BindVar("V" + _bindVarCounter++, mediaItemId, typeof(Guid));
                bindVarRefs.Add("@" + bindVar.Name);
                resultBindVars.Add(bindVar);
              }
              clusterExpressions.Add(" IN (" + StringUtils.Join(",", bindVarRefs) + ")");
            }
            resultParts.Add(StringUtils.Join(" OR ", clusterExpressions));
          }
        }
        return;
      }

      BooleanCombinationFilter boolFilter = filter as BooleanCombinationFilter;
      if (boolFilter != null)
      {
        int numOperands = boolFilter.Operands.Length;
        IEnumerator enumOperands = boolFilter.Operands.GetEnumerator();
        if (!enumOperands.MoveNext())
          return;
        if (numOperands > 1)
          resultParts.Add("(");
        CompileStatementParts(miaManagement, (IFilter) enumOperands.Current, outerMIIDJoinVariablePlaceHolder, resultParts, resultBindVars);
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
          CompileStatementParts(miaManagement, (IFilter) enumOperands.Current, outerMIIDJoinVariablePlaceHolder, resultParts, resultBindVars);
        }
        if (numOperands > 1)
          resultParts.Add(")");
        return;
      }

      NotFilter notFilter = filter as NotFilter;
      if (notFilter != null)
      {
        resultParts.Add("NOT (");
        CompileStatementParts(miaManagement, notFilter.InnerFilter, outerMIIDJoinVariablePlaceHolder, resultParts, resultBindVars);
        resultParts.Add(")");
        return;
      }

      FalseFilter falseFilter = filter as FalseFilter;
      if (falseFilter != null)
      {
        resultParts.Add("1 = 2");
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
          resultParts.Add("SELECT V.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add(" FROM ");
          resultParts.Add(miaManagement.GetMIACollectionAttributeTableName(attributeType));
          resultParts.Add(" V WHERE V.");
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
          resultParts.Add(" V ON NM.");
          resultParts.Add(MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME);
          resultParts.Add(" = V.");
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
        //  SELECT V.MEDIA_ITEM_ID
        //  FROM [OTM-Value-Table] V
        //  WHERE V.MI_ID=[Outer-Join-Variable-Placeholder] AND V.VALUE [Operator] [Comparison-Value])
        //
        // for MTM attributes, we create
        //
        // EXISTS(
        //  SELECT NM.MEDIA_ITEM_ID
        //  FROM [MTM-NM-Table] NM
        //  INNER JOIN [MTM-Value-Table] V ON NM.ID = V.ID
        //  WHERE NM.MI_ID=[Outer-Join-Variable-Placeholder] AND V.VALUE [Operator] [Comparison-Value])

        MediaItemAspectMetadata.AttributeSpecification attributeType = attributeFilter.AttributeType;
        Cardinality cardinality = attributeType.Cardinality;
        if (cardinality == Cardinality.Inline || cardinality == Cardinality.ManyToOne)
          BuildAttributeFilterExpression(attributeFilter, new QueryAttribute(attributeType), resultParts, resultBindVars);
        else if (cardinality == Cardinality.OneToMany)
        {
          resultParts.Add("EXISTS(");
          resultParts.Add("SELECT V.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add(" FROM ");
          resultParts.Add(miaManagement.GetMIACollectionAttributeTableName(attributeType));
          resultParts.Add(" V WHERE V.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add("=");
          resultParts.Add(outerMIIDJoinVariablePlaceHolder);
          resultParts.Add(" AND ");
          BuildAttributeFilterExpression(attributeFilter, "V." + MIA_Management.COLL_ATTR_VALUE_COL_NAME, resultParts, resultBindVars);
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
          resultParts.Add(" V ON NM.");
          resultParts.Add(MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME);
          resultParts.Add(" = V.");
          resultParts.Add(MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME);
          resultParts.Add(" WHERE NM.");
          resultParts.Add(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          resultParts.Add("=");
          resultParts.Add(outerMIIDJoinVariablePlaceHolder);
          resultParts.Add(" AND ");
          BuildAttributeFilterExpression(attributeFilter, "V." + MIA_Management.COLL_ATTR_VALUE_COL_NAME, resultParts, resultBindVars);
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
    /// <param name="resultBindVars">Bind variables for the attribute filter.</param>
    protected void BuildAttributeFilterExpression(IAttributeFilter filter, object attributeOperand,
        IList<object> resultParts, IList<BindVar> resultBindVars)
    {
      Type attributeType = filter.AttributeType.AttributeType;
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
        BindVar bindVar = new BindVar("V" + _bindVarCounter++, relationalFilter.FilterValue, attributeType);
        resultParts.Add("@" + bindVar.Name);
        resultBindVars.Add(bindVar);
        return;
      }

      LikeFilter likeFilter = filter as LikeFilter;
      if (likeFilter != null)
      {
        if (!likeFilter.CaseSensitive)
          resultParts.Add("UPPER(");

        resultParts.Add(attributeOperand);

        if (!likeFilter.CaseSensitive)
          resultParts.Add(")");

        resultParts.Add(" LIKE ");

        BindVar bindVar = new BindVar("V" + _bindVarCounter++, likeFilter.Expression, attributeType);
        if (likeFilter.CaseSensitive)
          resultParts.Add("@" + bindVar.Name);
        else
          resultParts.Add("UPPER(@" + bindVar.Name + ")");
        resultBindVars.Add(bindVar);
        if (likeFilter.EscapeChar.HasValue)
        {
          bindVar = new BindVar("E" + _bindVarCounter++, likeFilter.EscapeChar.ToString(), typeof(Char));
          resultParts.Add(" ESCAPE @" + bindVar.Name);
          resultBindVars.Add(bindVar);
        }
        return;
      }

      SimilarToFilter similarToFilter = filter as SimilarToFilter;
      if (similarToFilter != null)
      {
        resultParts.Add(attributeOperand);
        resultParts.Add(" SIMILAR TO ");
        BindVar bindVar = new BindVar("V" + _bindVarCounter++, similarToFilter.Expression, attributeType);
        resultParts.Add("@" + bindVar.Name);
        resultBindVars.Add(bindVar);
        if (similarToFilter.EscapeChar.HasValue)
        {
          bindVar = new BindVar("E" + _bindVarCounter++, similarToFilter.EscapeChar.ToString(), typeof(Char));
          resultParts.Add(" ESCAPE @" + bindVar.Name);
          resultBindVars.Add(bindVar);
        }
        return;
      }

      BetweenFilter betweenFilter = filter as BetweenFilter;
      if (betweenFilter != null)
      {
        resultParts.Add(attributeOperand);
        resultParts.Add(" BETWEEN ");
        BindVar bindVar = new BindVar("V" + _bindVarCounter++, betweenFilter.Value1, attributeType);
        resultParts.Add("@" + bindVar.Name);
        resultBindVars.Add(bindVar);
        resultParts.Add(" AND ");
        bindVar = new BindVar("V" + _bindVarCounter++, betweenFilter.Value2, attributeType);
        resultParts.Add("@" + bindVar.Name);
        resultBindVars.Add(bindVar);
        return;
      }

      InFilter inFilter = filter as InFilter;
      if (inFilter != null)
      {
        if (inFilter.Values.Length == 0)
        {
          resultParts.Add("1 = 2"); // No comparison values means filter is always false
          return;
        }
        ICollection<string> clusterExpressions = new List<string>();
        foreach (IList<object> valuesCluster in CollectionUtils.Cluster(inFilter.Values, MAX_IN_VALUES_SIZE))
        {
          IList<string> bindVarRefs = new List<string>(MAX_IN_VALUES_SIZE);
          foreach (object value in valuesCluster)
          {
            BindVar bindVar = new BindVar("V" + _bindVarCounter++, value, attributeType);
            bindVarRefs.Add("@" + bindVar.Name);
            resultBindVars.Add(bindVar);
          }
          clusterExpressions.Add(" IN (" + StringUtils.Join(",", bindVarRefs) + ")");
        }
        resultParts.Add(StringUtils.Join(" OR ", clusterExpressions));
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
        string outerMIIDJoinVariable, out string filterStr, out IList<BindVar> bindVars)
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
      bindVars = _statementBindVars;
    }
  }
}
