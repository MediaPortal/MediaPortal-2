#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using Test.Common;

namespace Test.Backend
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
      compiledFilter.test(MockCore.Management, filter, null, "test", null, parts, bindVars);

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
      compiledFilter.test(MockCore.Management, filter, requiredMIATypes, "test", null, parts, bindVars);

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
      compiledFilter.test(MockCore.Management, filter, requiredMIATypes, "test", null, parts, bindVars);

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
      compiledFilter.test(MockCore.Management, filter, requiredMIATypes, "test", tableJoins, parts, bindVars);

      //Console.WriteLine("Parts [{0}]", string.Join(",", parts));
      //Console.WriteLine("Bind vars [{0}]", string.Join(",", bindVars));
      //Console.WriteLine("Table joins [{0}]", string.Join(",", tableJoins));

      Assert.AreEqual(new List<object> {
        "test"," IN(",
        "SELECT R1.","MEDIA_ITEM_ID"," FROM ","M_RELATIONSHIP"," R1"," WHERE R1.LINKEDID","=@V0"," AND R1.","ROLE","=@V1"," AND R1.","LINKEDROLE","=@V2",
        " UNION ",
        "SELECT R2.","LINKEDID"," FROM ","M_RELATIONSHIP"," R2"," WHERE R2.MEDIA_ITEM_ID","=@V0"," AND R2.","LINKEDROLE","=@V1"," AND R2.","ROLE","=@V2",")"
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

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

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

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

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

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

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

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

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
        " WHERE T0.MEDIA_ITEM_ID IN(SELECT R1.MEDIA_ITEM_ID FROM M_RELATIONSHIP R1 WHERE R1.LINKEDID=@V0 AND R1.ROLE=@V1 AND R1.LINKEDROLE=@V2 UNION SELECT R2.LINKEDID "+
        "FROM M_RELATIONSHIP R2 WHERE R2.MEDIA_ITEM_ID=@V0 AND R2.LINKEDROLE=@V1 AND R2.ROLE=@V2)", statementStr, "Statement");
      Assert.AreEqual(new List<BindVar>
            {
                new BindVar("V0", movieId, typeof(Guid)),
                new BindVar("V1", actorType, typeof(Guid)),
                new BindVar("V2", movieType, typeof(Guid))
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

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

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

      MIAQueryBuilder builder = new MIAQueryBuilder(MockCore.Management, new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

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
  }
}
