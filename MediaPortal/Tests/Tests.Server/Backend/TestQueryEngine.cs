#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Linq;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Mock;
using NUnit.Framework;

namespace Tests.Server.Backend
{
  [TestFixture]
  public class TestQueryEngine
  {
    [SetUp]
    public void SetUp()
    {
      MockDBUtils.Reset();
      MockCore.Reset();
    }

    private IDictionary<MediaItemAspectMetadata, string> CreateMIAMAliases(params object[] args)
    {
      IDictionary<MediaItemAspectMetadata, string> aliases = new Dictionary<MediaItemAspectMetadata, string>();
      for (int index = 0; index < args.Length; index += 2)
      {
        aliases.Add((MediaItemAspectMetadata)args[index], (string)args[index + 1]);
      }
      return aliases;
    }

    [Test]
    public void TestMediaItemIdFilter()
    {
      Guid itemId1 = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid itemId2 = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");

      IList<Guid> ids = new List<Guid>();
      ids.Add(itemId1);
      ids.Add(itemId2);
      IFilter filter = new MediaItemIdFilter(ids);

      IList<object> parts = new List<object>();
      IList<BindVar> bindVars = new List<BindVar>();

      MockCompiledFilter compiledFilter = new MockCompiledFilter();
      compiledFilter.test(MockCore.Management, filter, null, null, "test", null, parts, bindVars);

      //Console.WriteLine("Parts [{0}]", string.Join(",", parts));
      //Console.WriteLine("Bind vars [{0}]", string.Join(",", bindVars));

      Assert.AreEqual(new List<object> { "test", " IN (@V0, @V1)", "" }, parts, "Parts");
      Assert.AreEqual(new List<BindVar>
      {
        new BindVar("V0", itemId1, typeof(Guid)), 
        new BindVar("V1", itemId2, typeof(Guid))
      }, bindVars, "Bind vars");
    }

    [Test]
    public void TestSingleMIALikeFilter()
    {
      SingleTestMIA mia1 = TestCommonUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, true);

      IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      IList<object> parts = new List<object>();
      IList<BindVar> bindVars = new List<BindVar>();

      MockCompiledFilter compiledFilter = new MockCompiledFilter();
      compiledFilter.test(MockCore.Management, filter, null, requiredMIATypes, "test", null, parts, bindVars);

      //Console.WriteLine("Parts [{0}]", string.Join(",", parts));
      //Console.WriteLine("Bind vars [{0}]", string.Join(",", bindVars));

      Assert.AreEqual(new List<object> { new QueryAttribute(mia1.ATTR_STRING), " LIKE ", "@V0" }, parts, "Parts");
      Assert.AreEqual(new List<BindVar> { new BindVar("V0", "%", typeof(string)) }, bindVars, "Bind vars");
    }

    [Test]
    public void TestMultipleMIALikeFilter()
    {
      MultipleTestMIA mia1 = TestBackendUtils.CreateMultipleMIA("Meta1", Cardinality.Inline, true, true);

      IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      IList<object> parts = new List<object>();
      IList<BindVar> bindVars = new List<BindVar>();

      MockCompiledFilter compiledFilter = new MockCompiledFilter();
      compiledFilter.test(MockCore.Management, filter, null, requiredMIATypes, "test", null, parts, bindVars);

      //Console.WriteLine("Parts [{0}]", string.Join(",", parts));
      //Console.WriteLine("Part 7 " + parts[7].GetType());
      //Console.WriteLine("Bind vars [{0}]", string.Join(",", bindVars));

      Assert.AreEqual(new List<object> {"test"," IN(",
        "SELECT ","MEDIA_ITEM_ID"," FROM ","M_META1"," WHERE ","ATTR_STRING"," LIKE ","@V0",")"
        }, parts, "Parts");
      Assert.AreEqual(new List<BindVar> { new BindVar("V0", "%", typeof(string)) }, bindVars, "Bind vars");
    }

    [Test]
    public void TestRelationshipFilter()
    {
      // Use the real RelationshipFilter because CompiledFilter is hard coded to look for it
      MockCore.AddMediaItemAspectStorage(RelationshipAspect.Metadata);

      Guid movieId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid movieType = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid actorType = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      IFilter filter = new RelationshipFilter(actorType, movieType, movieId);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();

      IList<object> parts = new List<object>();
      IList<BindVar> bindVars = new List<BindVar>();
      ICollection<TableJoin> tableJoins = new List<TableJoin>();

      MockCompiledFilter compiledFilter = new MockCompiledFilter();
      compiledFilter.test(MockCore.Management, filter, null, requiredMIATypes, "test", tableJoins, parts, bindVars);

      //Console.WriteLine("Parts [{0}]", string.Join(",", parts));
      //Console.WriteLine("Bind vars [{0}]", string.Join(",", bindVars));
      //Console.WriteLine("Table joins [{0}]", string.Join(",", tableJoins));

      Assert.AreEqual(new List<object> {
        "test"," IN(",
        "SELECT R1.","MEDIA_ITEM_ID"," FROM ","M_RELATIONSHIP"," R1"," WHERE"," R1.","LINKEDID","=@V0"," AND"," R1.","ROLE","=@V1"," AND"," R1.","LINKEDROLE","=@V2",
        " UNION ",
        "SELECT R1.","LINKEDID"," FROM ","M_RELATIONSHIP"," R1"," WHERE"," R1.","MEDIA_ITEM_ID","=@V0"," AND"," R1.","LINKEDROLE","=@V1"," AND"," R1.","ROLE","=@V2",")"
        }, parts, "Parts");

      Assert.AreEqual(new List<BindVar>
      {
        new BindVar("V0", movieId, typeof(Guid)),
        new BindVar("V1", actorType, typeof(Guid)),
        new BindVar("V2", movieType, typeof(Guid))
      }, bindVars, "Bind vars");
      Assert.AreEqual(new List<TableJoin> { }, tableJoins, "Tables joins");
    }

    [Test]
    public void TestSingleMIAOnlyLikeQueryBuilder()
    {
      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, false);
      SingleTestMIA mia2 = TestBackendUtils.CreateSingleMIA("Meta2", Cardinality.Inline, true, false);

      IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null, null);

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

      Assert.AreEqual("A0", mediaItemIdAlias, "Media item ID alias");
      Assert.AreEqual(CreateMIAMAliases(mia1.Metadata, "A1", mia2.Metadata, "A2"), miamAliases, "MIAM aliases");
      Assert.AreEqual(new Dictionary<QueryAttribute, string>(), attributeAliases, "Attribute aliases");
      Assert.AreEqual("SELECT T0.MEDIA_ITEM_ID A0, T0.MEDIA_ITEM_ID A1, T1.MEDIA_ITEM_ID A2 FROM M_META1 T0 INNER JOIN M_META2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID "+
        " WHERE T0.ATTR_STRING LIKE @V0", statementStr, "Statement");
      Assert.AreEqual(new List<BindVar> { new BindVar("V0", "%", typeof(string)) }, bindVars, "Bind vars");
    }

    [Test]
    public void TestMultipleMIAOnlyLikeQueryBuilder()
    {
      MultipleTestMIA mia1 = TestBackendUtils.CreateMultipleMIA("Meta1", Cardinality.Inline, true, false);
      MultipleTestMIA mia2 = TestBackendUtils.CreateMultipleMIA("Meta2", Cardinality.Inline, true, false);

      IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null, null);

      string mediaItemIdAlias = null;
      IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
      IDictionary<QueryAttribute, string> attributeAliases = null;
      string statementStr = null;
      IList<BindVar> bindVars = null;

      builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
      //Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
      //Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
      //Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
      //Console.WriteLine("statementStr: {0}", statementStr);
      //Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));

      Assert.AreEqual("A0", mediaItemIdAlias, "Media item ID alias");
      Assert.AreEqual(CreateMIAMAliases(mia1.Metadata, "A1", mia2.Metadata, "A2"), miamAliases, "MIAM aliases");
      Assert.AreEqual(new string[] { }, attributeAliases, "Attribute aliases");
      Assert.AreEqual("SELECT T0.MEDIA_ITEM_ID A0, T0.MEDIA_ITEM_ID A1, T1.MEDIA_ITEM_ID A2 FROM M_META1 T0 INNER JOIN M_META2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID "+
        " WHERE T0.MEDIA_ITEM_ID IN(SELECT MEDIA_ITEM_ID FROM M_META1 WHERE ATTR_STRING LIKE @V0)", statementStr, "Statement");
      Assert.AreEqual(new List<BindVar> { new BindVar("V0", "%", typeof(string)) }, bindVars, "Bind vars");
    }

    [Test]
    public void TestSingleAndMultipleMIAQueryBuilder()
    {
      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("single1", Cardinality.Inline, true, false);
      MultipleTestMIA mia2 = TestBackendUtils.CreateMultipleMIA("multi1", Cardinality.Inline, true, false);

      IFilter filter1 = new LikeFilter(mia1.ATTR_STRING, "%", null);
      IFilter filter2 = new LikeFilter(mia2.ATTR_STRING, "%", null);
      IFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new[] { filter1, filter2 });

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null, null);

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

      Assert.AreEqual("A0", mediaItemIdAlias, "Media item ID alias");
      Assert.AreEqual(CreateMIAMAliases(mia1.Metadata, "A1", mia2.Metadata, "A2"), miamAliases, "MIAM aliases");
      Assert.AreEqual(new Dictionary<QueryAttribute, string>(), attributeAliases, "Attribute aliases");
      Assert.AreEqual("SELECT T0.MEDIA_ITEM_ID A0, T0.MEDIA_ITEM_ID A1, T1.MEDIA_ITEM_ID A2 FROM M_SINGLE1 T0 INNER JOIN M_MULTI1 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID  " +
        "WHERE T0.MEDIA_ITEM_ID IN(SELECT MEDIA_ITEM_ID FROM M_MULTI1 WHERE ATTR_STRING LIKE @V0) AND T0.ATTR_STRING LIKE @V1", statementStr, "Statement");
      Assert.AreEqual(new List<BindVar>
            {
                new BindVar("V0", "%", typeof(string)),
                new BindVar("V1", "%", typeof(string))
            }, bindVars, "Bind vars");
    }

    [Test]
    public void TestRelationshipQueryBuilder()
    {
      // Use the real RelationshipFilter because CompiledFilter is hard coded to look for it
      MockCore.AddMediaItemAspectStorage(RelationshipAspect.Metadata);

      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, true);
      SingleTestMIA mia2 = TestBackendUtils.CreateSingleMIA("Meta2", Cardinality.Inline, true, true);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      Guid movieId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid movieType = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid actorType = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      IFilter filter = new RelationshipFilter(actorType, movieType, movieId);

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null, null);

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

      Assert.AreEqual("A0", mediaItemIdAlias, "Media item ID alias");
      Assert.AreEqual(CreateMIAMAliases(mia1.Metadata, "A1", mia2.Metadata, "A2"), miamAliases, "MIAM aliases");
      Assert.AreEqual(new Dictionary<QueryAttribute, string>(), attributeAliases, "Attribute aliases");
      Assert.AreEqual("SELECT T0.MEDIA_ITEM_ID A0, T0.MEDIA_ITEM_ID A1, T1.MEDIA_ITEM_ID A2 FROM M_META1 T0 INNER JOIN M_META2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID "+
        " WHERE T0.MEDIA_ITEM_ID IN(SELECT R1.MEDIA_ITEM_ID FROM M_RELATIONSHIP R1 WHERE R1.LINKEDID=@V0 AND R1.ROLE=@V1 AND R1.LINKEDROLE=@V2 " +
        "UNION SELECT R1.LINKEDID FROM M_RELATIONSHIP R1 WHERE R1.MEDIA_ITEM_ID=@V0 AND R1.LINKEDROLE=@V1 AND R1.ROLE=@V2)", statementStr, "Statement");
      Assert.AreEqual(new List<BindVar>
            {
                new BindVar("V0", movieId, typeof(Guid)),
                new BindVar("V1", actorType, typeof(Guid)),
                new BindVar("V2", movieType, typeof(Guid))
            }, bindVars, "Bind vars");
    }

    [Test]
    public void TestFilteredRelationshipQueryBuilder()
    {
      // Use the real RelationshipFilter because CompiledFilter is hard coded to look for it
      MockCore.AddMediaItemAspectStorage(RelationshipAspect.Metadata);

      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, true);
      SingleTestMIA mia2 = TestBackendUtils.CreateSingleMIA("Meta2", Cardinality.Inline, true, true);
      SingleTestMIA mia3 = TestBackendUtils.CreateSingleMIA("Meta3", Cardinality.Inline, true, true);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      IFilter linkedMovieFilter = BooleanCombinationFilter.CombineFilters(
        BooleanOperator.And,
        new RelationalFilter(mia3.ATTR_INTEGER, RelationalOperator.EQ, 1),
        new RelationalFilter(mia3.ATTR_STRING, RelationalOperator.EQ, "test"));

      Guid movieType = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid actorType = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      IFilter filter = new FilteredRelationshipFilter(actorType, movieType, linkedMovieFilter);

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null, null);

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

      Assert.AreEqual("A0", mediaItemIdAlias, "Media item ID alias");
      Assert.AreEqual(CreateMIAMAliases(mia1.Metadata, "A1", mia2.Metadata, "A2"), miamAliases, "MIAM aliases");
      Assert.AreEqual(new Dictionary<QueryAttribute, string>(), attributeAliases, "Attribute aliases");
      Assert.AreEqual("SELECT T0.MEDIA_ITEM_ID A0, T0.MEDIA_ITEM_ID A1, T1.MEDIA_ITEM_ID A2 FROM M_META1 T0 INNER JOIN M_META2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID " +
        " WHERE T0.MEDIA_ITEM_ID IN(SELECT R1.MEDIA_ITEM_ID FROM M_RELATIONSHIP R1 WHERE R1.ROLE=@V0 AND R1.LINKEDROLE=@V1 AND R1.LINKEDID IN( " +
        "SELECT TS.A0 FROM (SELECT T0.MEDIA_ITEM_ID A0, T1.MEDIA_ITEM_ID A1 FROM MEDIA_ITEMS T0 LEFT OUTER JOIN M_META3 T1 ON T0.MEDIA_ITEM_ID = T1.MEDIA_ITEM_ID " +
        " WHERE (T1.ATTR_INTEGER = @V2 AND T1.ATTR_STRING = @V3)) TS) " +
        "UNION SELECT R1.LINKEDID FROM M_RELATIONSHIP R1 WHERE R1.LINKEDROLE=@V0 AND R1.ROLE=@V1 AND R1.MEDIA_ITEM_ID IN( SELECT TS.A0 FROM (" +
        "SELECT T0.MEDIA_ITEM_ID A0, T1.MEDIA_ITEM_ID A1 FROM MEDIA_ITEMS T0 LEFT OUTER JOIN M_META3 T1 ON T0.MEDIA_ITEM_ID = T1.MEDIA_ITEM_ID " +
        " WHERE (T1.ATTR_INTEGER = @V2 AND T1.ATTR_STRING = @V3)) TS))", statementStr, "Statement");
      Assert.AreEqual(new List<BindVar>
            {
                new BindVar("V0", actorType, typeof(Guid)),
                new BindVar("V1", movieType, typeof(Guid)),
                new BindVar("V2", 1, typeof(int)),
                new BindVar("V3", "test", typeof(string))
            }, bindVars, "Bind vars");
    }

    [Test]
    public void TestAndQueryBuilder()
    {
      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, true);
      SingleTestMIA mia2 = TestBackendUtils.CreateSingleMIA("Meta2", Cardinality.Inline, true, true);

      IList<IFilter> filters = new List<IFilter>();
      filters.Add(new LikeFilter(mia1.ATTR_STRING, "%", null));
      filters.Add(new LikeFilter(mia2.ATTR_STRING, "%", null));
      IFilter filter = new BooleanCombinationFilter(BooleanOperator.And, filters);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null, null);

      string mediaItemIdAlias = null;
      IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
      IDictionary<QueryAttribute, string> attributeAliases = null;
      string statementStr = null;
      IList<BindVar> bindVars = null;

      builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
      //Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
      //Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
      //Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
      //Console.WriteLine("statementStr: {0}", statementStr);
      //Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));

      Assert.AreEqual("A0", mediaItemIdAlias, "Media item ID alias");
      Assert.AreEqual(CreateMIAMAliases(mia1.Metadata, "A1", mia2.Metadata, "A2"), miamAliases, "MIAM aliases");
      Assert.AreEqual(new string[] { }, attributeAliases, "Attribute aliases");
      Assert.AreEqual("SELECT T0.MEDIA_ITEM_ID A0, T0.MEDIA_ITEM_ID A1, T1.MEDIA_ITEM_ID A2 FROM M_META1 T0 INNER JOIN M_META2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID  " +
        "WHERE (T0.ATTR_STRING LIKE @V0 AND T1.ATTR_STRING LIKE @V1)", statementStr, "Statement");
      Assert.AreEqual(new List<BindVar>
      {
          new BindVar("V0", "%", typeof(string)),
          new BindVar("V1", "%", typeof(string))
      }, bindVars, "Bind vars");
    }

    [Test]
    public void TestExternalQueryBuilder()
    {
      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, true);
      SingleTestMIA mia2 = TestBackendUtils.CreateSingleMIA("Meta2", Cardinality.Inline, true, true);
      MockCore.AddMediaItemAspectStorage(ExternalIdentifierAspect.Metadata);

      string source = "test";
      string type = "series";
      string id = "123";

      // Search using external identifiers
      BooleanCombinationFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new[]
      {
        new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
        new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
        new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
      });

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);
      requiredMIATypes.Add(mia2.Metadata);

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null, null);

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

    [Test]
    public void TestInverseRelationshipQueryBuilder()
    {
      MockCore.AddMediaItemAspectStorage(RelationshipAspect.Metadata);

      IList<Guid> guids = new List<Guid>();
      guids.Add(new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa"));
      guids.Add(new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb"));
      guids.Add(new Guid("cccccccc-3333-3333-3333-cccccccccccc"));

      IList<QueryAttribute> attributes = new List<QueryAttribute>();
      foreach (MediaItemAspectMetadata.AttributeSpecification attr in RelationshipAspect.Metadata.AttributeSpecifications.Values)
      {
        if (attr.Cardinality == Cardinality.Inline || attr.Cardinality == Cardinality.ManyToOne)
          attributes.Add(new QueryAttribute(attr));
      }

      string mediaItemIdAlias;
      IDictionary<QueryAttribute, string> attributeAliases;
      string statementStr;
      IList<BindVar> bindVars;

      InverseRelationshipQueryBuilder builder = new InverseRelationshipQueryBuilder(MockCore.Management, attributes, guids.ToArray());
      builder.GenerateSqlStatement(out mediaItemIdAlias, out attributeAliases, out statementStr, out bindVars);

      Console.WriteLine("attributes: [{0}]", string.Join(",", attributes));
      Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
      Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
      Console.WriteLine("statementStr: {0}", statementStr);
      Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));
    }

    [Test]
    public void TestSubqueryQueryBuilder()
    {
      // Use the real RelationshipFilter because CompiledFilter is hard coded to look for it
      MockCore.AddMediaItemAspectStorage(RelationshipAspect.Metadata);

      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("Meta1", Cardinality.Inline, true, true);
      SingleTestMIA mia2 = TestBackendUtils.CreateSingleMIA("Meta2", Cardinality.Inline, true, true);
      SingleTestMIA mia3 = TestBackendUtils.CreateSingleMIA("Meta3", Cardinality.Inline, true, true);
      SingleTestMIA mia4 = TestBackendUtils.CreateSingleMIA("Meta4", Cardinality.Inline, true, true);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(mia1.Metadata);

      IFilter linkedMovieFilter = new RelationalFilter(mia2.ATTR_INTEGER, RelationalOperator.EQ, 1);

      Guid movieType = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid collectionType = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      Guid actorType = new Guid("dddddddd-4444-4444-4444-dddddddddddd");
      IFilter movieToCollectionFilter = new FilteredRelationshipFilter(collectionType, movieType, linkedMovieFilter);

      IFilter linkedActorFilter = new RelationalFilter(mia3.ATTR_INTEGER, RelationalOperator.EQ, 1);
      IFilter movieToCollectionToActorFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
        linkedActorFilter, new FilteredRelationshipFilter(actorType, collectionType, movieToCollectionFilter));

      IFilter subQueryFilter = new RelationalFilter(mia4.ATTR_INTEGER, RelationalOperator.EQ, 1);

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), movieToCollectionToActorFilter, subQueryFilter, null);

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

      Assert.AreEqual("A0", mediaItemIdAlias, "Media item ID alias");
      Assert.AreEqual(CreateMIAMAliases(mia1.Metadata, "A1", mia3.Metadata, "A2"), miamAliases, "MIAM aliases");
      Assert.AreEqual(new Dictionary<QueryAttribute, string>(), attributeAliases, "Attribute aliases");
      Assert.AreEqual("SELECT T0.MEDIA_ITEM_ID A0, T0.MEDIA_ITEM_ID A1, T1.MEDIA_ITEM_ID A2 FROM M_META1 T0 LEFT OUTER JOIN M_META3 T1 ON T0.MEDIA_ITEM_ID = T1.MEDIA_ITEM_ID " +
        " WHERE (T1.ATTR_INTEGER = @V0 AND T0.MEDIA_ITEM_ID" +
          " IN(SELECT R1.MEDIA_ITEM_ID FROM M_RELATIONSHIP R1 WHERE R1.ROLE=@V1 AND R1.LINKEDROLE=@V2 AND R1.LINKEDID" +
            " IN( SELECT TS.A0 FROM (SELECT T0.MEDIA_ITEM_ID A0, T1.MEDIA_ITEM_ID A1 FROM MEDIA_ITEMS T0 LEFT OUTER JOIN M_META4 T1 ON T0.MEDIA_ITEM_ID = T1.MEDIA_ITEM_ID " +
              " WHERE (T0.MEDIA_ITEM_ID IN(SELECT R1.MEDIA_ITEM_ID FROM M_RELATIONSHIP R1 WHERE R1.ROLE=@V3 AND R1.LINKEDROLE=@V4 AND R1.LINKEDID" +
                " IN( SELECT TS.A0 FROM (SELECT T0.MEDIA_ITEM_ID A0, T1.MEDIA_ITEM_ID A1, T2.MEDIA_ITEM_ID A2 FROM MEDIA_ITEMS T0 LEFT OUTER JOIN M_META2 T1 ON T0.MEDIA_ITEM_ID = T1.MEDIA_ITEM_ID LEFT OUTER JOIN M_META4 T2 ON T0.MEDIA_ITEM_ID = T2.MEDIA_ITEM_ID" +
                 "  WHERE (T1.ATTR_INTEGER = @V5 AND T2.ATTR_INTEGER = @V6)) TS)" +
                " UNION SELECT R1.LINKEDID FROM M_RELATIONSHIP R1 WHERE R1.LINKEDROLE=@V3 AND R1.ROLE=@V4 AND R1.MEDIA_ITEM_ID" +
                " IN( SELECT TS.A0 FROM (SELECT T0.MEDIA_ITEM_ID A0, T1.MEDIA_ITEM_ID A1, T2.MEDIA_ITEM_ID A2 FROM MEDIA_ITEMS T0 LEFT OUTER JOIN M_META2 T1 ON T0.MEDIA_ITEM_ID = T1.MEDIA_ITEM_ID LEFT OUTER JOIN M_META4 T2 ON T0.MEDIA_ITEM_ID = T2.MEDIA_ITEM_ID" +
                  "  WHERE (T1.ATTR_INTEGER = @V5 AND T2.ATTR_INTEGER = @V6)) TS)) AND T1.ATTR_INTEGER = @V7)) TS)" +
            " UNION SELECT R1.LINKEDID FROM M_RELATIONSHIP R1 WHERE R1.LINKEDROLE=@V1 AND R1.ROLE=@V2 AND R1.MEDIA_ITEM_ID" +
            " IN( SELECT TS.A0 FROM (SELECT T0.MEDIA_ITEM_ID A0, T1.MEDIA_ITEM_ID A1 FROM MEDIA_ITEMS T0 LEFT OUTER JOIN M_META4 T1 ON T0.MEDIA_ITEM_ID = T1.MEDIA_ITEM_ID" +
              "  WHERE (T0.MEDIA_ITEM_ID IN(SELECT R1.MEDIA_ITEM_ID FROM M_RELATIONSHIP R1 WHERE R1.ROLE=@V3 AND R1.LINKEDROLE=@V4 AND R1.LINKEDID" +
                " IN( SELECT TS.A0 FROM (SELECT T0.MEDIA_ITEM_ID A0, T1.MEDIA_ITEM_ID A1, T2.MEDIA_ITEM_ID A2 FROM MEDIA_ITEMS T0 LEFT OUTER JOIN M_META2 T1 ON T0.MEDIA_ITEM_ID = T1.MEDIA_ITEM_ID LEFT OUTER JOIN M_META4 T2 ON T0.MEDIA_ITEM_ID = T2.MEDIA_ITEM_ID" +
                "  WHERE (T1.ATTR_INTEGER = @V5 AND T2.ATTR_INTEGER = @V6)) TS)" +
                " UNION SELECT R1.LINKEDID FROM M_RELATIONSHIP R1 WHERE R1.LINKEDROLE=@V3 AND R1.ROLE=@V4 AND R1.MEDIA_ITEM_ID" +
                " IN( SELECT TS.A0 FROM (SELECT T0.MEDIA_ITEM_ID A0, T1.MEDIA_ITEM_ID A1, T2.MEDIA_ITEM_ID A2 FROM MEDIA_ITEMS T0 LEFT OUTER JOIN M_META2 T1 ON T0.MEDIA_ITEM_ID = T1.MEDIA_ITEM_ID LEFT OUTER JOIN M_META4 T2 ON T0.MEDIA_ITEM_ID = T2.MEDIA_ITEM_ID" +
                  "  WHERE (T1.ATTR_INTEGER = @V5 AND T2.ATTR_INTEGER = @V6)) TS)) AND T1.ATTR_INTEGER = @V7)) TS)))", statementStr, "Statement");
      Assert.AreEqual(new List<BindVar>
            {
                new BindVar("V0", 1, typeof(int)),
                new BindVar("V1", actorType, typeof(Guid)),
                new BindVar("V2", collectionType, typeof(Guid)),
                new BindVar("V3", collectionType, typeof(Guid)),
                new BindVar("V4", movieType, typeof(Guid)),
                new BindVar("V5", 1, typeof(int)),
                new BindVar("V6", 1, typeof(int)),
                new BindVar("V7", 1, typeof(int))
            }, bindVars, "Bind vars");
    }

    [Test]
    public void TestQueryLimit()
    {
      MockDBUtils.Reset();
      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, true);
      MultipleTestMIA mia2 = TestBackendUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, true, false);
      MultipleTestMIA mia3 = TestBackendUtils.CreateMultipleMIA("MULTIPLE3", Cardinality.Inline, false, true);

      Guid itemId0 = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid itemId1 = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid itemId2 = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      Guid itemId3 = new Guid("dddddddd-4444-4444-4444-dddddddddddd");
      Guid itemId4 = new Guid("eeeeeeee-5555-5555-5555-eeeeeeeeeeee");
      Guid itemId5 = new Guid("ffffffff-6666-6666-6666-ffffffffffff");
      Guid itemId6 = new Guid("aaaaaaaa-7777-7777-7777-aaaaaaaaaaaa");

      MockReader reader = MockDBUtils.AddReader(1, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_STRING A0, T0.ATTR_INTEGER A1 FROM M_SINGLE1 T0  WHERE T0.MEDIA_ITEM_ID IN(SELECT T0.MEDIA_ITEM_ID FROM M_SINGLE1 T0" +
        " INNER JOIN M_MULTIPLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID )  AND EXISTS(SELECT 1 FROM M_MULTIPLE2 WHERE MEDIA_ITEM_ID=T0.MEDIA_ITEM_ID)", "A2", "A3", "A4", "A0", "A1");
      reader.AddResult(itemId0, itemId0, itemId0, "zero", 0, "0_0");
      reader.AddResult(itemId1, itemId1, itemId1, "one", 1, "1_1");
      reader.AddResult(itemId2, itemId2, itemId2, "two", 2, "2_2");
      reader.AddResult(itemId3, itemId3, itemId3, "tree", 3, "3_3");
      reader.AddResult(itemId4, itemId4, itemId4, "four", 4, "4_4");
      reader.AddResult(itemId5, itemId5, itemId5, "five", 5, "5_5");
      reader.AddResult(itemId6, itemId6, itemId6, "six", 6, "6_6");

      MockReader multipleReader2 = MockDBUtils.AddReader(2, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_STRING A1 FROM M_MULTIPLE2 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1, @V2, @V3, @V4)", "A2", "A3", "A0", "A1");
      multipleReader2.AddResult(itemId0, itemId0, "0_0", "zerozero");
      multipleReader2.AddResult(itemId0, itemId0, "0_1", "zeroone");
      multipleReader2.AddResult(itemId1, itemId1, "1_0", "onezero");
      multipleReader2.AddResult(itemId1, itemId1, "1_1", "oneone");
      multipleReader2.AddResult(itemId2, itemId2, "2_0", "twozero");
      multipleReader2.AddResult(itemId2, itemId2, "2_1", "twoone");
      multipleReader2.AddResult(itemId3, itemId3, "3_0", "twoone");
      multipleReader2.AddResult(itemId3, itemId3, "3_1", "threeone");
      multipleReader2.AddResult(itemId3, itemId3, "3_2", "threetwo");
      multipleReader2.AddResult(itemId4, itemId4, "4_0", "fourzero");
      multipleReader2.AddResult(itemId4, itemId4, "4_1", "fourone");

      MockReader multipleReader3 = MockDBUtils.AddReader(3, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_INTEGER A1 FROM M_MULTIPLE3 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1, @V2, @V3, @V4)", "A2", "A3", "A0", "A1");
      multipleReader3.AddResult(itemId1, itemId1, "1_0", 10);
      multipleReader3.AddResult(itemId1, itemId1, "1_1", 11);
      multipleReader3.AddResult(itemId1, itemId1, "1_2", 12);
      multipleReader3.AddResult(itemId1, itemId1, "1_3", 13);
      multipleReader3.AddResult(itemId1, itemId1, "1_4", 14);
      multipleReader3.AddResult(itemId2, itemId2, "1_0", 20);
      multipleReader3.AddResult(itemId3, itemId3, "1_0", 30);
      multipleReader3.AddResult(itemId3, itemId3, "1_1", 31);
      multipleReader3.AddResult(itemId3, itemId3, "1_2", 32);
                                                     
      MockReader reader2 = MockDBUtils.AddReader(4, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T1.MEDIA_ITEM_ID A4, T0.ATTR_STRING A0, T0.ATTR_INTEGER A1 FROM M_SINGLE1 T0 INNER JOIN M_MULTIPLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID ", "A2", "A3", "A4", "A0", "A1");
      reader2.AddResult(itemId0, itemId0, itemId0, "zero", 0, "0_0");
      reader2.AddResult(itemId0, itemId0, itemId0, "zero", 0, "0_0");
      reader2.AddResult(itemId1, itemId1, itemId1, "one", 1, "1_1");
      reader2.AddResult(itemId1, itemId1, itemId1, "one", 1, "1_1");
      reader2.AddResult(itemId2, itemId2, itemId2, "two", 2, "2_2");
      reader2.AddResult(itemId2, itemId2, itemId2, "two", 2, "2_2");
      reader2.AddResult(itemId3, itemId3, itemId3, "tree", 3, "3_3");
      reader2.AddResult(itemId3, itemId3, itemId3, "tree", 3, "3_3");
      reader2.AddResult(itemId3, itemId3, itemId3, "tree", 3, "3_3");
      reader2.AddResult(itemId4, itemId4, itemId4, "four", 4, "4_4");
      reader2.AddResult(itemId4, itemId4, itemId4, "four", 4, "4_4");
      reader2.AddResult(itemId5, itemId5, itemId5, "five", 5, "5_5");
      reader2.AddResult(itemId5, itemId5, itemId5, "five", 5, "5_5");
      reader2.AddResult(itemId6, itemId6, itemId6, "six", 6, "6_6");
      reader2.AddResult(itemId6, itemId6, itemId6, "six", 6, "6_6");
      reader2.AddResult(itemId6, itemId6, itemId6, "six", 6, "6_6");

      MockReader multipleReader4 = MockDBUtils.AddReader(5, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_STRING A1 FROM M_MULTIPLE2 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1, @V2)", "A2", "A3", "A0", "A1");
      multipleReader4.AddResult(itemId0, itemId0, "0_0", "zerozero");
      multipleReader4.AddResult(itemId0, itemId0, "0_1", "zeroone");
      multipleReader4.AddResult(itemId1, itemId1, "1_0", "onezero");
      multipleReader4.AddResult(itemId1, itemId1, "1_1", "oneone");
      multipleReader4.AddResult(itemId2, itemId2, "2_0", "twozero");
      multipleReader4.AddResult(itemId2, itemId2, "2_1", "twoone");

      MockReader multipleReader5 = MockDBUtils.AddReader(6, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_INTEGER A1 FROM M_MULTIPLE3 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1, @V2)", "A2", "A3", "A0", "A1");
      multipleReader5.AddResult(itemId1, itemId1, "1_0", 10);
      multipleReader5.AddResult(itemId1, itemId1, "1_1", 11);
      multipleReader5.AddResult(itemId1, itemId1, "1_2", 12);
      multipleReader5.AddResult(itemId1, itemId1, "1_3", 13);
      multipleReader5.AddResult(itemId1, itemId1, "1_4", 14);
      multipleReader5.AddResult(itemId2, itemId2, "1_0", 20);

      Guid[] requiredAspects = { mia1.ASPECT_ID, mia2.ASPECT_ID };
      Guid[] optionalAspects = { mia3.ASPECT_ID };
      MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, null)
      {
        Limit = 5,
      };
      CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MockCore.Management, query);
      IList<MediaItem> results = compiledQuery.QueryList();
      /*
      foreach (MediaItem result in results)
          //Console.WriteLine("Query result " + result.MediaItemId + ": " + string.Join(",", result.Aspects.Values) + ": " + result);
      */

      Assert.AreEqual(5, results.Count, "Results count");

      Assert.AreEqual(itemId0, results[0].MediaItemId, "MediaItem ID #0");
      Assert.AreEqual(2, results[0].Aspects.Count, "MediaItem Aspects #0");
      Assert.AreEqual(itemId1, results[1].MediaItemId, "MediaItem ID #1");
      Assert.AreEqual(3, results[1].Aspects.Count, "MediaItem Aspects #1");
      Assert.AreEqual(itemId2, results[2].MediaItemId, "MediaItem ID #2");
      Assert.AreEqual(3, results[2].Aspects.Count, "MediaItem Aspects #2");
      Assert.AreEqual(itemId3, results[3].MediaItemId, "MediaItem ID #3");
      Assert.AreEqual(3, results[3].Aspects.Count, "MediaItem Aspects #3");
      Assert.AreEqual(itemId4, results[4].MediaItemId, "MediaItem ID #4");
      Assert.AreEqual(2, results[4].Aspects.Count, "MediaItem Aspects #4");
    }

    [Test]
    public void TestQueryOffset()
    {
      MockDBUtils.Reset();
      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, true);
      MultipleTestMIA mia2 = TestBackendUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, true, false);
      MultipleTestMIA mia3 = TestBackendUtils.CreateMultipleMIA("MULTIPLE3", Cardinality.Inline, false, true);

      Guid itemId0 = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid itemId1 = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid itemId2 = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      Guid itemId3 = new Guid("dddddddd-4444-4444-4444-dddddddddddd");
      Guid itemId4 = new Guid("eeeeeeee-5555-5555-5555-eeeeeeeeeeee");
      Guid itemId5 = new Guid("ffffffff-6666-6666-6666-ffffffffffff");
      Guid itemId6 = new Guid("aaaaaaaa-7777-7777-7777-aaaaaaaaaaaa");

      MockReader reader = MockDBUtils.AddReader(1, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_STRING A0, T0.ATTR_INTEGER A1 FROM M_SINGLE1 T0  WHERE T0.MEDIA_ITEM_ID IN(SELECT T0.MEDIA_ITEM_ID FROM M_SINGLE1 T0" +
        " INNER JOIN M_MULTIPLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID )  AND EXISTS(SELECT 1 FROM M_MULTIPLE2 WHERE MEDIA_ITEM_ID=T0.MEDIA_ITEM_ID)", "A2", "A3", "A4", "A0", "A1");
      reader.AddResult(itemId0, itemId0, itemId0, "zero", 0, "0_0");
      reader.AddResult(itemId1, itemId1, itemId1, "one", 1, "1_1");
      reader.AddResult(itemId2, itemId2, itemId2, "two", 2, "2_2");
      reader.AddResult(itemId3, itemId3, itemId3, "tree", 3, "3_3");
      reader.AddResult(itemId4, itemId4, itemId4, "four", 4, "4_4");
      reader.AddResult(itemId5, itemId5, itemId5, "five", 5, "5_5");
      reader.AddResult(itemId6, itemId6, itemId6, "six", 6, "6_6");

      MockReader multipleReader2 = MockDBUtils.AddReader(2, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_STRING A1 FROM M_MULTIPLE2 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1, @V2, @V3)", "A2", "A3", "A0", "A1");
      multipleReader2.AddResult(itemId3, itemId3, "3_0", "twoone");
      multipleReader2.AddResult(itemId3, itemId3, "3_1", "threeone");
      multipleReader2.AddResult(itemId3, itemId3, "3_2", "threetwo");
      multipleReader2.AddResult(itemId4, itemId4, "4_0", "fourzero");
      multipleReader2.AddResult(itemId4, itemId4, "4_1", "fourone");
      multipleReader2.AddResult(itemId5, itemId5, "5_0", "fivezero");
      multipleReader2.AddResult(itemId5, itemId5, "5_1", "fiveone");
      multipleReader2.AddResult(itemId6, itemId6, "6_0", "sixzero");
      multipleReader2.AddResult(itemId6, itemId6, "6_1", "sixone");
      multipleReader2.AddResult(itemId6, itemId6, "6_2", "sixtwo");

      MockReader multipleReader3 = MockDBUtils.AddReader(3, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_INTEGER A1 FROM M_MULTIPLE3 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1, @V2, @V3)", "A2", "A3", "A0", "A1");
      multipleReader3.AddResult(itemId3, itemId3, "1_0", 30);
      multipleReader3.AddResult(itemId3, itemId3, "1_1", 31);
      multipleReader3.AddResult(itemId3, itemId3, "1_2", 32);
      multipleReader3.AddResult(itemId6, itemId6, "1_0", 60);
      multipleReader3.AddResult(itemId6, itemId6, "1_1", 61);
      multipleReader3.AddResult(itemId6, itemId6, "1_2", 62);
      multipleReader3.AddResult(itemId6, itemId6, "1_3", 63);
      multipleReader3.AddResult(itemId6, itemId6, "1_4", 64);

      MockReader reader2 = MockDBUtils.AddReader(4, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T1.MEDIA_ITEM_ID A4, T0.ATTR_STRING A0, T0.ATTR_INTEGER A1 FROM M_SINGLE1 T0 INNER JOIN M_MULTIPLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID ", "A2", "A3", "A4", "A0", "A1");
      reader2.AddResult(itemId0, itemId0, itemId0, "zero", 0, "0_0");
      reader2.AddResult(itemId0, itemId0, itemId0, "zero", 0, "0_0");
      reader2.AddResult(itemId1, itemId1, itemId1, "one", 1, "1_1");
      reader2.AddResult(itemId1, itemId1, itemId1, "one", 1, "1_1");
      reader2.AddResult(itemId2, itemId2, itemId2, "two", 2, "2_2");
      reader2.AddResult(itemId2, itemId2, itemId2, "two", 2, "2_2");
      reader2.AddResult(itemId3, itemId3, itemId3, "tree", 3, "3_3");
      reader2.AddResult(itemId3, itemId3, itemId3, "tree", 3, "3_3");
      reader2.AddResult(itemId3, itemId3, itemId3, "tree", 3, "3_3");
      reader2.AddResult(itemId4, itemId4, itemId4, "four", 4, "4_4");
      reader2.AddResult(itemId4, itemId4, itemId4, "four", 4, "4_4");
      reader2.AddResult(itemId5, itemId5, itemId5, "five", 5, "5_5");
      reader2.AddResult(itemId5, itemId5, itemId5, "five", 5, "5_5");
      reader2.AddResult(itemId6, itemId6, itemId6, "six", 6, "6_6");
      reader2.AddResult(itemId6, itemId6, itemId6, "six", 6, "6_6");
      reader2.AddResult(itemId6, itemId6, itemId6, "six", 6, "6_6");

      MockReader multipleReader4 = MockDBUtils.AddReader(5, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_STRING A1 FROM M_MULTIPLE2 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1, @V2, @V3, @V4, @V5)", "A2", "A3", "A0", "A1");
      multipleReader4.AddResult(itemId0, itemId0, "0_0", "zerozero");
      multipleReader4.AddResult(itemId0, itemId0, "0_1", "zeroone");
      multipleReader4.AddResult(itemId1, itemId1, "1_0", "onezero");
      multipleReader4.AddResult(itemId1, itemId1, "1_1", "oneone");
      multipleReader4.AddResult(itemId2, itemId2, "2_0", "twozero");
      multipleReader4.AddResult(itemId2, itemId2, "2_1", "twoone");
      multipleReader4.AddResult(itemId3, itemId3, "3_0", "twoone");
      multipleReader4.AddResult(itemId3, itemId3, "3_1", "threeone");
      multipleReader4.AddResult(itemId3, itemId3, "3_2", "threetwo");
      multipleReader4.AddResult(itemId4, itemId4, "4_0", "fourzero");
      multipleReader4.AddResult(itemId4, itemId4, "4_1", "fourone");
      multipleReader4.AddResult(itemId5, itemId5, "5_0", "fivezero");
      multipleReader4.AddResult(itemId5, itemId5, "5_1", "fiveone");
      multipleReader4.AddResult(itemId6, itemId6, "6_0", "sixzero");
      multipleReader4.AddResult(itemId6, itemId6, "6_1", "sixone");
      multipleReader4.AddResult(itemId6, itemId6, "6_2", "sixtwo");

      MockReader multipleReader5 = MockDBUtils.AddReader(6, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_INTEGER A1 FROM M_MULTIPLE3 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1, @V2, @V3, @V4, @V5)", "A2", "A3", "A0", "A1");
      multipleReader5.AddResult(itemId1, itemId1, "1_0", 10);
      multipleReader5.AddResult(itemId1, itemId1, "1_1", 11);
      multipleReader5.AddResult(itemId1, itemId1, "1_2", 12);
      multipleReader5.AddResult(itemId1, itemId1, "1_3", 13);
      multipleReader5.AddResult(itemId1, itemId1, "1_4", 14);
      multipleReader5.AddResult(itemId2, itemId2, "1_0", 20);
      multipleReader5.AddResult(itemId3, itemId3, "1_0", 30);
      multipleReader5.AddResult(itemId3, itemId3, "1_1", 31);
      multipleReader5.AddResult(itemId3, itemId3, "1_2", 32);
      multipleReader5.AddResult(itemId6, itemId6, "1_0", 60);
      multipleReader5.AddResult(itemId6, itemId6, "1_1", 61);
      multipleReader5.AddResult(itemId6, itemId6, "1_2", 62);
      multipleReader5.AddResult(itemId6, itemId6, "1_3", 63);
      multipleReader5.AddResult(itemId6, itemId6, "1_4", 64);

      Guid[] requiredAspects = { mia1.ASPECT_ID, mia2.ASPECT_ID };
      Guid[] optionalAspects = { mia3.ASPECT_ID };
      MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, null)
      {
        Offset = 3,
      };
      CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MockCore.Management, query);
      IList<MediaItem> results = compiledQuery.QueryList();
      /*
      foreach (MediaItem result in results)
          //Console.WriteLine("Query result " + result.MediaItemId + ": " + string.Join(",", result.Aspects.Values) + ": " + result);
      */

      Assert.AreEqual(4, results.Count, "Results count");

      Assert.AreEqual(itemId3, results[0].MediaItemId, "MediaItem ID #0");
      Assert.AreEqual(3, results[0].Aspects.Count, "MediaItem Aspects #0");
      Assert.AreEqual(itemId4, results[1].MediaItemId, "MediaItem ID #1");
      Assert.AreEqual(2, results[1].Aspects.Count, "MediaItem Aspects #1");
      Assert.AreEqual(itemId5, results[2].MediaItemId, "MediaItem ID #2");
      Assert.AreEqual(2, results[2].Aspects.Count, "MediaItem Aspects #2");
      Assert.AreEqual(itemId6, results[3].MediaItemId, "MediaItem ID #3");
      Assert.AreEqual(3, results[3].Aspects.Count, "MediaItem Aspects #3");
    }

    [Test]
    public void Should_BuildSQLStatementForComplexMIAAttributes_When_QueryAttributeCardinalityIsManyToMany()
    {
      // Arrange
      MultipleTestMIA mia = new MultipleTestMIA { ASPECT_ID = Guid.NewGuid() };

      IList<MediaItemAspectMetadata.MultipleAttributeSpecification> attributes = new List<MediaItemAspectMetadata.MultipleAttributeSpecification>();
      attributes.Add(mia.ATTR_ID = MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("EPISODE", 10, Cardinality.ManyToMany, true));
      attributes.Add(mia.ATTR_STRING = MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("EPISODE", 10, Cardinality.ManyToMany, false));
      mia.Metadata = new MultipleMediaItemAspectMetadata(mia.ASPECT_ID, "EpisodeItem", attributes.ToArray(), new[] { mia.ATTR_ID });
      MockCore.AddMediaItemAspectStorage(mia.Metadata);

      IList<Guid> ids = new List<Guid>
      {
        Guid.NewGuid(),
        Guid.NewGuid()
      };

      IFilter filter = new MediaItemIdFilter(ids);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>
      {
        mia.Metadata
      };

      ComplexAttributeQueryBuilder builder = new ComplexAttributeQueryBuilder(MockCore.Management, mia.Metadata.AttributeSpecifications.Values.First(), null, requiredMIATypes, filter, null);

      string mediaItemIdAlias = null;
      string valueAlias = null;
      string statementStr = null;
      IList<BindVar> bindVars = null;

      // Act
      builder.GenerateSqlStatement(out mediaItemIdAlias, out valueAlias, out statementStr, out bindVars);

      // Assert<
      Assert.AreEqual("SELECT T0.MEDIA_ITEM_ID A0, T1.VALUE A1 FROM NM_EPISODEITEM_EPISODE T0 INNER JOIN " +
                      "V_EPISODEITEM_EPISODE T1 ON T0.VALUE_ID = T1.VALUE_ID WHERE T0.MEDIA_ITEM_ID IN " +
                      "(@V0, @V1) ORDER BY T0.VALUE_ORDER", statementStr);
    }
  }
}
