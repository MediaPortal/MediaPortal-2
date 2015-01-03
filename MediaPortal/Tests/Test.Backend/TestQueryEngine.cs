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

using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Utilities.DB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;

namespace Test.Backend
{
  [TestClass]
  public class TestQueryEngine
  {
    [TestMethod]
    public void TestMediaItemIdFilter()
    {
        TestDbUtils.Setup();
        MIAUtils.Setup();

        Guid itemId1 = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid itemId2 = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");

      IList<Guid> ids = new List<Guid>();
      ids.Add(itemId1);
      ids.Add(itemId2);
      IFilter filter = new MediaItemIdFilter(ids);

      IList<object> resultParts = new List<object>();
      IList<BindVar> resultBindVars = new List<BindVar>();

      TestCompiledFilter compiledFilter = new TestCompiledFilter();
      compiledFilter.test(MIAUtils.Management, filter, null, "test", null, resultParts, resultBindVars);

      Console.WriteLine("Result parts [{0}]", string.Join(",", resultParts));
      Console.WriteLine("Result bind vars [{0}]", string.Join(",", resultBindVars));
    }

    [TestMethod]
    public void TestSingleMIALikeFilter()
    {
        TestDbUtils.Setup();
        MIAUtils.Setup();

        SingleTestMIA mia1 = MIAUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, true);

      IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      IList<object> resultParts = new List<object>();
      IList<BindVar> resultBindVars = new List<BindVar>();

      TestCompiledFilter compiledFilter = new TestCompiledFilter();
      compiledFilter.test(MIAUtils.Management, filter, requiredMIATypes, "test", null, resultParts, resultBindVars);

      Console.WriteLine("Result parts [{0}]", string.Join(",", resultParts));
      Console.WriteLine("Result bind vars [{0}]", string.Join(",", resultBindVars));
    }

    [TestMethod]
    public void TestMultipleMIALikeFilter()
    {
        TestDbUtils.Setup();
        MIAUtils.Setup();

        MultipleTestMIA mia1 = MIAUtils.CreateMultipleMIA("Meta1", Cardinality.Inline, true, true);

        IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

        ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
        requiredMIATypes.Add(mia1.Metadata);
        IList<object> resultParts = new List<object>();
        IList<BindVar> resultBindVars = new List<BindVar>();

        TestCompiledFilter compiledFilter = new TestCompiledFilter();
        compiledFilter.test(MIAUtils.Management, filter, requiredMIATypes, "test", null, resultParts, resultBindVars);

        Console.WriteLine("Result parts [{0}]", string.Join(",", resultParts));
        Console.WriteLine("Result bind vars [{0}]", string.Join(",", resultBindVars));
    }

    [TestMethod]
    public void TestRelationshipFilter()
    {
        TestDbUtils.Setup();
        MIAUtils.Setup();

        // Use the real RelationshipFilter because CompiledFilter is hard coded to look for it
        MIAUtils.AddMediaItemAspectStorage(RelationshipAspect.Metadata);

      Guid movieId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid movieType = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid actorType = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      IFilter filter = new RelationshipFilter(movieId, movieType, actorType);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();

      IList<object> resultParts = new List<object>();
      IList<BindVar> resultBindVars = new List<BindVar>();
      ICollection<TableJoin> tableJoins = new List<TableJoin>();

      TestCompiledFilter compiledFilter = new TestCompiledFilter();
      compiledFilter.test(MIAUtils.Management, filter, requiredMIATypes, "test", tableJoins, resultParts, resultBindVars);
    }

    [TestMethod]
    public void TestSingleMIAOnlyLikeQueryBuilder()
    {
        TestDbUtils.Setup();
        MIAUtils.Setup();

        SingleTestMIA mia1 = MIAUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, false);
        SingleTestMIA mia2 = MIAUtils.CreateSingleMIA("Meta2", Cardinality.Inline, true, false);

        IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      SingleMIAQueryBuilder builder = new SingleMIAQueryBuilder(MIAUtils.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

      string mediaItemIdAlias = null;
      IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
      IDictionary<QueryAttribute, string> attributeAliases = null;
      string statementStr = null;
      IList<BindVar> bindVars = null;

      builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
      Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
      Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
      Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
      Console.WriteLine("statementStr: {0}", statementStr);
      Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));
    }

    [TestMethod]
    public void TestMultipleMIAOnlyLikeQueryBuilder()
    {
        TestDbUtils.Setup();
        MIAUtils.Setup();

        MultipleTestMIA mia1 = MIAUtils.CreateMultipleMIA("Meta1", Cardinality.Inline, true, false);
        MultipleTestMIA mia2 = MIAUtils.CreateMultipleMIA("Meta2", Cardinality.Inline, true, false);

        IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

        ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
        requiredMIATypes.Add(mia1.Metadata);
        requiredMIATypes.Add(mia2.Metadata);

        SingleMIAQueryBuilder builder = new SingleMIAQueryBuilder(MIAUtils.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

        string mediaItemIdAlias = null;
        IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
        IDictionary<QueryAttribute, string> attributeAliases = null;
        string statementStr = null;
        IList<BindVar> bindVars = null;

        builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
        Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
        Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
        Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
        Console.WriteLine("statementStr: {0}", statementStr);
        Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));
    }

    [TestMethod]
    public void TestSingleAndMultipleMIAQueryBuilder()
    {
        TestDbUtils.Setup();
        MIAUtils.Setup();

        SingleTestMIA mia1 = MIAUtils.CreateSingleMIA("single1", Cardinality.Inline, true, false);
        MultipleTestMIA mia2 = MIAUtils.CreateMultipleMIA("multi1", Cardinality.Inline, true, false);

        IFilter filter1 = new LikeFilter(mia1.ATTR_STRING, "%", null);
        IFilter filter2 = new LikeFilter(mia2.ATTR_STRING, "%", null);
        IFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new IFilter[] { filter1, filter2 });

        ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
        requiredMIATypes.Add(mia1.Metadata);
        requiredMIATypes.Add(mia2.Metadata);

        SingleMIAQueryBuilder builder = new SingleMIAQueryBuilder(MIAUtils.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

        string mediaItemIdAlias = null;
        IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
        IDictionary<QueryAttribute, string> attributeAliases = null;
        string statementStr = null;
        IList<BindVar> bindVars = null;

        builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
        Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
        Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
        Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
        Console.WriteLine("statementStr: {0}", statementStr);
        Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));
    }

    [TestMethod]
    public void TestRelationshipQueryBuilder()
    {
        TestDbUtils.Setup();
        MIAUtils.Setup();

        // Use the real RelationshipFilter because CompiledFilter is hard coded to look for it
        MIAUtils.AddMediaItemAspectStorage(RelationshipAspect.Metadata);

        SingleTestMIA mia1 = MIAUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, true);
        SingleTestMIA mia2 = MIAUtils.CreateSingleMIA("Meta2", Cardinality.Inline, true, true);

        ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      Guid movieId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid movieType = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid actorType = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      IFilter filter = new RelationshipFilter(movieId, movieType, actorType);

      SingleMIAQueryBuilder builder = new SingleMIAQueryBuilder(MIAUtils.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

      string mediaItemIdAlias = null;
      IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
      IDictionary<QueryAttribute, string> attributeAliases = null;
      string statementStr = null;
      IList<BindVar> bindVars = null;

      builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
      Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
      Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
      Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
      Console.WriteLine("statementStr: {0}", statementStr);
      Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));
    }

    [TestMethod]
    public void TestAndQueryBuilder()
    {
        TestDbUtils.Setup();
        MIAUtils.Setup();

        SingleTestMIA mia1 = MIAUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, true);
        SingleTestMIA mia2 = MIAUtils.CreateSingleMIA("Meta2", Cardinality.Inline, true, true);

      IList<IFilter> filters = new List<IFilter>();
      filters.Add(new LikeFilter(mia1.ATTR_STRING, "%", null));
      filters.Add(new LikeFilter(mia2.ATTR_STRING, "%", null));
      IFilter filter = new BooleanCombinationFilter(BooleanOperator.And, filters);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      SingleMIAQueryBuilder builder = new SingleMIAQueryBuilder(MIAUtils.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

      string mediaItemIdAlias = null;
      IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
      IDictionary<QueryAttribute, string> attributeAliases = null;
      string statementStr = null;
      IList<BindVar> bindVars = null;

      builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
      Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
      Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
      Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
      Console.WriteLine("statementStr: {0}", statementStr);
      Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));
    }
  }
}
