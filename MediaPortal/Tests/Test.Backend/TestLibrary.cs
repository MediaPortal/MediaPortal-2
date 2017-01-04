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
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Mock;
using NUnit.Framework;
using Test.Common;

namespace Test.Backend
{
  [TestFixture]
  public class TestLibrary
  {
    private string[] CreateAttributeIdList(int firstId, int maxId)
    {
      IList<string> ids = new List<string>();

      for (int id = firstId; id <= maxId; id++)
        ids.Add("A" + id);

      for (int id = 0; id < firstId; id++)
        ids.Add("A" + id);

      return ids.ToArray();
    }

    [SetUp]
    public void SetUp()
    {
      MockDBUtils.Reset();
      MockCore.Reset();
    }

    [Test]
    public void TestMediaItemAspectStorage()
    {
      TestBackendUtils.CreateSingleMIA("SINGLE", Cardinality.Inline, true, true);
      MockCommand singleCommand = MockDBUtils.FindCommand("CREATE TABLE M_SINGLE");
      Assert.IsNotNull(singleCommand, "Single create table command");
      // Columns and objects will be what we asked for
      Assert.AreEqual("CREATE TABLE M_SINGLE (MEDIA_ITEM_ID Guid, ATTR_STRING TEXT, ATTR_INTEGER Int32, CONSTRAINT M_SINGLE_PK PRIMARY KEY (MEDIA_ITEM_ID), CONSTRAINT FK FOREIGN KEY (MEDIA_ITEM_ID) REFERENCES MEDIA_ITEMS (MEDIA_ITEM_ID) ON DELETE CASCADE)", singleCommand.CommandText, "Single1 create table command");

      MockDBUtils.Reset();
      TestBackendUtils.CreateMultipleMIA("MULTIPLE", Cardinality.Inline, true, false);
      MockCommand multipleCommand = MockDBUtils.FindCommand("CREATE TABLE M_MULTIPLE");
      Assert.IsNotNull(multipleCommand, "Multiple create table command");
      // Columns and objects will be suffixed with _0 because the alises we asked for have already been given to Multiple1
      Assert.AreEqual("CREATE TABLE M_MULTIPLE (MEDIA_ITEM_ID Guid, ATTR_ID TEXT, ATTR_STRING TEXT, CONSTRAINT M_MULTIPLE_PK PRIMARY KEY (MEDIA_ITEM_ID,ATTR_ID), CONSTRAINT FK FOREIGN KEY (MEDIA_ITEM_ID) REFERENCES MEDIA_ITEMS (MEDIA_ITEM_ID) ON DELETE CASCADE)", multipleCommand.CommandText, "Multiple1 create table command");

      MockDBUtils.Reset();
      TestBackendUtils.CreateMultipleMIA("META3", Cardinality.OneToMany, true, true);
      MockCommand meta3Command = MockDBUtils.FindCommand("CREATE TABLE M_META3");
      Assert.IsNotNull(meta3Command, "Meta3 create table command");
      Assert.AreEqual("CREATE TABLE M_META3 (MEDIA_ITEM_ID Guid, ATTR_ID TEXT, CONSTRAINT M_META3_PK PRIMARY KEY (MEDIA_ITEM_ID,ATTR_ID), CONSTRAINT FK FOREIGN KEY (MEDIA_ITEM_ID) REFERENCES MEDIA_ITEMS (MEDIA_ITEM_ID) ON DELETE CASCADE)", meta3Command.CommandText, "Meta3 create table command");
    }

    [Test]
    public void TestMediaItemLoader_SingleMIAs_IdFilter()
    {
      MockDBUtils.Reset();
      SingleTestMIA single1 = TestBackendUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, false);
      SingleTestMIA single2 = TestBackendUtils.CreateSingleMIA("SINGLE2", Cardinality.Inline, false, true);

      Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      IList<Guid> ids = new List<Guid>();
      ids.Add(itemId);
      IFilter filter = new MediaItemIdFilter(ids);

      MockReader reader = MockDBUtils.AddReader(
        "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T1.MEDIA_ITEM_ID A4, T0.ATTR_STRING A0, T1.ATTR_INTEGER A1 FROM M_SINGLE1 T0 INNER JOIN M_SINGLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID "+
        " WHERE T0.MEDIA_ITEM_ID = @V0", "A2", "A3", "A4", "A0", "A1");
      reader.AddResult(itemId, itemId, itemId, "zero", 0);

      Guid[] requiredAspects = new Guid[] { single1.ASPECT_ID, single2.ASPECT_ID };
      Guid[] optionalAspects = null;
      MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
      CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MockCore.Management, query);
      MediaItem result = compiledQuery.QueryMediaItem();
      Assert.AreEqual(itemId, result.MediaItemId, "MediaItem ID");
      // TODO: More asserts
    }

    [Test]
    public void TestMediaItemLoader_SingleMIAs_LikeFilter()
    {
      MockDBUtils.Reset();
      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, false);
      SingleTestMIA mia2 = TestBackendUtils.CreateSingleMIA("SINGLE2", Cardinality.Inline, false, true);
      SingleTestMIA mia3 = TestBackendUtils.CreateSingleMIA("SINGLE3", Cardinality.Inline, true, true);

      Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");

      IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

      MockReader reader = MockDBUtils.AddReader(
          "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, T1.MEDIA_ITEM_ID A6, T2.MEDIA_ITEM_ID A7, T0.ATTR_STRING A0, T1.ATTR_INTEGER A1, T2.ATTR_STRING A2, T2.ATTR_INTEGER A3 "+
          "FROM M_SINGLE1 T0 INNER JOIN M_SINGLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID LEFT OUTER JOIN M_SINGLE3 T2 ON T2.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID "+
          " WHERE T0.ATTR_STRING LIKE @V0", "A4", "A5", "A6", "A7", "A0", "A1", "A2", "A3");
      reader.AddResult(itemId, itemId, itemId, itemId, "zerozero", 11, "twotwo", 33);

      Guid[] requiredAspects = new Guid[] { mia1.ASPECT_ID, mia2.ASPECT_ID };
      Guid[] optionalAspects = new Guid[] { mia3.ASPECT_ID };
      MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
      CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MockCore.Management, query);
      MediaItem result = compiledQuery.QueryMediaItem();
      //Console.WriteLine("Query result " + result.MediaItemId + ": " + string.Join(",", result.Aspects) + ": " + result);

      Assert.AreEqual(itemId, result.MediaItemId, "MediaItem ID");
      SingleMediaItemAspect value = null;
      Assert.IsTrue(MediaItemAspect.TryGetAspect(result.Aspects, mia1.Metadata, out value), "MIA1");
      Assert.AreEqual("zerozero", value.GetAttributeValue(mia1.ATTR_STRING), "MIA1 string attibute");
      Assert.IsTrue(MediaItemAspect.TryGetAspect(result.Aspects, mia2.Metadata, out value), "MIA2");
      Assert.AreEqual(11, value.GetAttributeValue(mia2.ATTR_INTEGER), "MIA2 integer attibute");
      Assert.IsTrue(MediaItemAspect.TryGetAspect(result.Aspects, mia3.Metadata, out value), "MIA3");
      Assert.AreEqual("twotwo", value.GetAttributeValue(mia3.ATTR_STRING), "MIA3 string attibute");
      Assert.AreEqual(33, value.GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute");
    }

    [Test]
    public void TestMediaItemLoader_MultipleMIAs_IdFilter()
    {
      MockDBUtils.Reset();
      MultipleTestMIA mia1 = TestBackendUtils.CreateMultipleMIA("MULTIPLE1", Cardinality.Inline, true, false);
      MultipleTestMIA mia2 = TestBackendUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, false, true);

      Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      IList<Guid> ids = new List<Guid>();
      ids.Add(itemId);
      IFilter filter = new MediaItemIdFilter(ids);

      MockReader multipleReader1 = MockDBUtils.AddReader(1, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_STRING A1 FROM M_MULTIPLE1 T0  WHERE T0.MEDIA_ITEM_ID = @V0", "A2", "A3", "A0", "A1");
      multipleReader1.AddResult(itemId, itemId, "1_1", "oneone");

      MockReader multipleReader2 = MockDBUtils.AddReader(2, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_INTEGER A1 FROM M_MULTIPLE2 T0  WHERE T0.MEDIA_ITEM_ID = @V0", "A2", "A3", "A0", "A1");
      multipleReader2.AddResult(itemId, itemId, "2_1", 21);
      multipleReader2.AddResult(itemId, itemId, "2_2", 22);

      MockReader multipleReader3 = MockDBUtils.AddReader(3, "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, T1.MEDIA_ITEM_ID A6, T0.ATTR_ID A0, T0.ATTR_STRING A1, T1.ATTR_ID A2, T1.ATTR_INTEGER A3 FROM M_MULTIPLE1 T0 "+
        "INNER JOIN M_MULTIPLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID  WHERE T0.MEDIA_ITEM_ID = @V0", "A4");
      multipleReader3.AddResult(itemId, itemId, itemId, "1_1", "oneone", "1_1", 11);
      multipleReader3.AddResult(itemId, itemId, itemId, "2_2", "twotwo", "2_2", 22);

      Guid[] requiredAspects = new Guid[] { mia1.ASPECT_ID, mia2.ASPECT_ID };
      Guid[] optionalAspects = null;
      MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
      CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MockCore.Management, query);
      MediaItem result = compiledQuery.QueryMediaItem();
      //Console.WriteLine("Query result " + result.MediaItemId + ": " + string.Join(",", result.Aspects) + ": " + result);

      IList<MultipleMediaItemAspect> values;

      Assert.AreEqual(itemId, result.MediaItemId, "MediaItem ID");
      Assert.IsTrue(MediaItemAspect.TryGetAspects(result.Aspects, mia1.Metadata, out values), "MIA1");
      Assert.AreEqual("oneone", values[0].GetAttributeValue(mia1.ATTR_STRING), "MIA1 string attibute");
      Assert.IsTrue(MediaItemAspect.TryGetAspects(result.Aspects, mia2.Metadata, out values), "MIA2");
      Assert.AreEqual(21, values[0].GetAttributeValue(mia2.ATTR_INTEGER), "MIA2 integer attibute #0");
      Assert.AreEqual(22, values[1].GetAttributeValue(mia2.ATTR_INTEGER), "MIA2 integer attibute #1");
    }

    [Test]
    public void TestMediaItemsLoader_SingleAndMultipleMIAs_BooleanLikeFilter()
    {
      MockDBUtils.Reset();
      SingleTestMIA mia1 = TestBackendUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, true);
      MultipleTestMIA mia2 = TestBackendUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, true, false);
      MultipleTestMIA mia3 = TestBackendUtils.CreateMultipleMIA("MULTIPLE3", Cardinality.Inline, false, true);

      Guid itemId0 = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid itemId1 = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");

      IFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new List<IFilter> { new LikeFilter(mia1.ATTR_STRING, "%", null), new LikeFilter(mia2.ATTR_STRING, "%", null) });

      MockReader reader = MockDBUtils.AddReader(1, "SELECT T0.MEDIA_ITEM_ID A6, T0.MEDIA_ITEM_ID A7, T1.MEDIA_ITEM_ID A8, T2.MEDIA_ITEM_ID A9, T0.ATTR_STRING A0, T0.ATTR_INTEGER A1, T1.ATTR_ID A2, T1.ATTR_STRING A3, T2.ATTR_ID A4, "+
        "T2.ATTR_INTEGER A5 FROM M_SINGLE1 T0 INNER JOIN M_MULTIPLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID LEFT OUTER JOIN M_MULTIPLE3 T2 ON T2.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID "+
        " WHERE T0.MEDIA_ITEM_ID IN(SELECT MEDIA_ITEM_ID FROM M_MULTIPLE2 WHERE ATTR_STRING LIKE @V0) AND T0.ATTR_STRING LIKE @V1", "A6", "A7", "A8", "A9", "A0", "A1", "A2");
      reader.AddResult(itemId0, itemId0, itemId0, itemId0, "zero", 0, "0_0");
      reader.AddResult(itemId1, itemId1, itemId1, itemId1, "one", 1, "1_1");

      MockReader multipleReader2 = MockDBUtils.AddReader(2, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_STRING A1 FROM M_MULTIPLE2 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1)", "A2", "A3", "A0", "A1");
      multipleReader2.AddResult(itemId0, itemId0, "0_0", "zerozero");
      multipleReader2.AddResult(itemId0, itemId0, "0_1", "zeroone");
      multipleReader2.AddResult(itemId1, itemId1, "1_0", "onezero");

      MockReader multipleReader3 = MockDBUtils.AddReader(3, "SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_ID A0, T0.ATTR_INTEGER A1 FROM M_MULTIPLE3 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1)", "A2", "A3", "A0", "A1");
      multipleReader3.AddResult(itemId0, itemId0, "1_0", 10);
      multipleReader3.AddResult(itemId0, itemId0, "1_1", 11);
      multipleReader3.AddResult(itemId0, itemId0, "1_2", 12);
      multipleReader3.AddResult(itemId0, itemId0, "1_3", 13);
      multipleReader3.AddResult(itemId0, itemId0, "1_4", 14);
      multipleReader3.AddResult(itemId1, itemId1, "1_0", 20);

      Guid[] requiredAspects = { mia1.ASPECT_ID, mia2.ASPECT_ID };
      Guid[] optionalAspects = { mia3.ASPECT_ID };
      MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
      CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MockCore.Management, query);
      IList<MediaItem> results = compiledQuery.QueryList();
      /*
      foreach (MediaItem result in results)
          //Console.WriteLine("Query result " + result.MediaItemId + ": " + string.Join(",", result.Aspects.Values) + ": " + result);
      */

      SingleMediaItemAspect value;
      IList<MultipleMediaItemAspect> values;

      Assert.AreEqual(2, results.Count, "Results count");

      Assert.AreEqual(itemId0, results[0].MediaItemId, "MediaItem ID #0");
      Assert.IsTrue(MediaItemAspect.TryGetAspect(results[0].Aspects, mia1.Metadata, out value), "MIA1 #0");
      Assert.AreEqual("zero", value.GetAttributeValue(mia1.ATTR_STRING), "MIA1 string attibute #0");
      Assert.AreEqual(0, value.GetAttributeValue(mia1.ATTR_INTEGER), "MIA1 integer attibute #0");
      Assert.IsTrue(MediaItemAspect.TryGetAspects(results[0].Aspects, mia2.Metadata, out values), "MIA2 #0");
      Assert.AreEqual(2, values.Count, "MIA2 count #0");
      Assert.AreEqual("zerozero", values[0].GetAttributeValue(mia2.ATTR_STRING), "MIA2 string attibute 0 #0");
      Assert.AreEqual("zeroone", values[1].GetAttributeValue(mia2.ATTR_STRING), "MIA2 string attibute 1 #0");
      Assert.IsTrue(MediaItemAspect.TryGetAspects(results[0].Aspects, mia3.Metadata, out values), "MIA3 #0");
      Assert.AreEqual(5, values.Count, "MIA3 count #0");
      Assert.AreEqual(10, values[0].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 0 #0");
      Assert.AreEqual(11, values[1].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 1 #0");
      Assert.AreEqual(12, values[2].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 2 #0");
      Assert.AreEqual(13, values[3].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 3 #0");
      Assert.AreEqual(14, values[4].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 4 #0");

      Assert.AreEqual(itemId1, results[1].MediaItemId, "MediaItem ID #1");
      Assert.IsTrue(MediaItemAspect.TryGetAspect(results[1].Aspects, mia1.Metadata, out value), "MIA1 #0");
      Assert.AreEqual("one", value.GetAttributeValue(mia1.ATTR_STRING), "MIA1 string attibute #1");
      Assert.AreEqual(1, value.GetAttributeValue(mia1.ATTR_INTEGER), "MIA1 integer attibute #1");
      Assert.IsTrue(MediaItemAspect.TryGetAspects(results[1].Aspects, mia2.Metadata, out values), "MIA2 #1");
      Assert.AreEqual(1, values.Count, "MIA2 count #1");
      Assert.AreEqual("onezero", values[0].GetAttributeValue(mia2.ATTR_STRING), "MIA2 string attibute 0 #1");
      Assert.IsTrue(MediaItemAspect.TryGetAspects(results[1].Aspects, mia3.Metadata, out values), "MIA3 #0");
      Assert.AreEqual(1, values.Count, "MIA3 count #1");
      Assert.AreEqual(20, values[0].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 0 #1");
    }

    [Test]
    public void TestMediaItemLoader_SingleMIAsUnusedOptional_IdFilter()
    {
      MockDBUtils.Reset();
      SingleTestMIA single1 = TestBackendUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, false);
      SingleTestMIA single2 = TestBackendUtils.CreateSingleMIA("SINGLE2", Cardinality.Inline, false, true);
      SingleTestMIA single3 = TestBackendUtils.CreateSingleMIA("SINGLE3", Cardinality.Inline, false, true);
      SingleTestMIA single4 = TestBackendUtils.CreateSingleMIA("SINGLE4", Cardinality.Inline, false, true);

      Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      IList<Guid> ids = new List<Guid>();
      ids.Add(itemId);
      IFilter filter = new MediaItemIdFilter(ids);

      
      MockReader reader = MockDBUtils.AddReader(
        "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, T1.MEDIA_ITEM_ID A6, T2.MEDIA_ITEM_ID A7, T3.MEDIA_ITEM_ID A8, T0.ATTR_STRING A0, T1.ATTR_INTEGER A1, T2.ATTR_INTEGER A2, "+
        "T3.ATTR_INTEGER A3 FROM M_SINGLE1 T0 INNER JOIN M_SINGLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID LEFT OUTER JOIN M_SINGLE3 T2 ON T2.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID "+
        "LEFT OUTER JOIN M_SINGLE4 T3 ON T3.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID  WHERE T0.MEDIA_ITEM_ID = @V0", "A4", "A5", "A6", "A7", "A8", "A0", "A1", "A2", "A3");
      reader.AddResult(itemId, itemId, itemId, itemId, null, "zero", 0, 0, null);

      Guid[] requiredAspects = new Guid[] { single1.ASPECT_ID, single2.ASPECT_ID };
      Guid[] optionalAspects = new Guid[] { single3.ASPECT_ID, single4.ASPECT_ID };
      MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
      CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MockCore.Management, query);
      MediaItem result = compiledQuery.QueryMediaItem();
      Assert.AreEqual(itemId, result.MediaItemId, "MediaItem ID");
      SingleMediaItemAspect value = null;
      Assert.IsTrue(MediaItemAspect.TryGetAspect(result.Aspects, single1.Metadata, out value), "MIA1");
      Assert.IsTrue(MediaItemAspect.TryGetAspect(result.Aspects, single2.Metadata, out value), "MIA2");
      Assert.IsTrue(MediaItemAspect.TryGetAspect(result.Aspects, single3.Metadata, out value), "MIA3");
      Assert.IsFalse(MediaItemAspect.TryGetAspect(result.Aspects, single4.Metadata, out value), "MIA4");
    }

    [Test]
    public void TestAddMediaItem()
    {
      MockDBUtils.Reset();
      MockCore.SetupLibrary();

      SingleTestMIA mia1 = TestCommonUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, true);
      MultipleTestMIA mia2 = TestCommonUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, true, false);
      MultipleTestMIA mia3 = TestCommonUtils.CreateMultipleMIA("MULTIPLE3", Cardinality.Inline, false, true);

      MockCore.Management.AddMediaItemAspectStorage(mia1.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(mia2.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(mia3.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(ProviderResourceAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(ImporterAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(MediaAspect.Metadata);

      IList<MediaItemAspect> aspects = new List<MediaItemAspect>();

      SingleMediaItemAspect aspect1 = new SingleMediaItemAspect(mia1.Metadata);
      aspect1.SetAttribute(mia1.ATTR_INTEGER, 1);
      aspect1.SetAttribute(mia1.ATTR_STRING, "one");
      aspects.Add(aspect1);

      MultipleMediaItemAspect aspect2_1 = new MultipleMediaItemAspect(mia2.Metadata);
      aspect2_1.SetAttribute(mia2.ATTR_STRING, "two.one");
      aspects.Add(aspect2_1);
      MultipleMediaItemAspect aspect2_2 = new MultipleMediaItemAspect(mia2.Metadata);
      aspect2_2.SetAttribute(mia2.ATTR_STRING, "two.two");
      aspects.Add(aspect2_2);

      MultipleMediaItemAspect aspect3_1 = new MultipleMediaItemAspect(mia3.Metadata);
      aspect3_1.SetAttribute(mia3.ATTR_INTEGER, 31);
      aspects.Add(aspect3_1);
      MultipleMediaItemAspect aspect3_2 = new MultipleMediaItemAspect(mia3.Metadata);
      aspect3_2.SetAttribute(mia3.ATTR_INTEGER, 32);
      aspects.Add(aspect3_2);
      MultipleMediaItemAspect aspect3_3 = new MultipleMediaItemAspect(mia3.Metadata);
      aspect3_3.SetAttribute(mia3.ATTR_INTEGER, 33);
      aspects.Add(aspect3_3);

      MockDBUtils.AddReader(1, "SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE SYSTEM_ID = @SYSTEM_ID AND PATH = @PATH", "MEDIA_ITEM_ID");
      MockDBUtils.AddReader(2, "SELECT T6.MEDIA_ITEM_ID A24, T0.MEDIA_ITEM_ID A25, T1.MEDIA_ITEM_ID A26, T2.MEDIA_ITEM_ID A27, T3.MEDIA_ITEM_ID A28, " +
        "T4.MEDIA_ITEM_ID A29, T5.MEDIA_ITEM_ID A30, T0.ATTR_STRING A0, T0.ATTR_INTEGER A1, T1.ATTR_ID A2, T1.ATTR_STRING A3, T2.ATTR_ID A4, T2.ATTR_INTEGER A5, " +
        "T3.SYSTEM_ID A6, T3.RESOURCEINDEX A7, T3.ISPRIMARY A8, T3.MIMETYPE A9, T3.SIZE A10, T3.PATH A11, T3.PARENTDIRECTORY A12, T4.LASTIMPORTDATE A13, T4.DIRTY A14, " +
        "T4.DATEADDED A15, T5.TITLE A16, T5.SORTTITLE A17, T5.RECORDINGTIME A18, T5.RATING A19, T5.COMMENT A20, T5.PLAYCOUNT A21, T5.LASTPLAYED A22, T5.ISVIRTUAL A23 " +
        "FROM MEDIA_ITEMS T6 LEFT OUTER JOIN M_SINGLE1 T0 ON T0.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID LEFT OUTER JOIN M_MULTIPLE2 T1 ON T1.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_MULTIPLE3 T2 ON T2.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID LEFT OUTER JOIN M_PROVIDERRESOURCE T3 ON T3.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_IMPORTEDITEM T4 ON T4.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID LEFT OUTER JOIN M_MEDIAITEM T5 ON T5.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID  WHERE T6.MEDIA_ITEM_ID = @V0", "V0");

      string pathStr = "c:\\item.mp3";
      ResourcePath path = LocalFsResourceProviderBase.ToResourcePath(pathStr);
      MockCore.Library.AddOrUpdateMediaItem(Guid.Empty, null, path, aspects, false);

      MockCore.ShutdownLibrary();
    }

    [Test]
    public void TestEditSmallMediaItem()
    {
      MockDBUtils.Reset();
      MockCore.SetupLibrary();

      MultipleTestMIA mia1 = TestCommonUtils.CreateMultipleMIA("MULTIPLE1", Cardinality.Inline, true, false);
      MockCore.Management.AddMediaItemAspectStorage(mia1.Metadata);

      MockCore.Management.AddMediaItemAspectStorage(ProviderResourceAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(ImporterAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(MediaAspect.Metadata);

      IList<MediaItemAspect> aspects = new List<MediaItemAspect>();

      MultipleMediaItemAspect aspect1_1 = new MultipleMediaItemAspect(mia1.Metadata);
      aspect1_1.SetAttribute(mia1.ATTR_STRING, "1_1");
      aspect1_1.SetAttribute(mia1.ATTR_STRING, "one.three");
      aspects.Add(aspect1_1);
      MultipleMediaItemAspect aspect1_2 = new MultipleMediaItemAspect(mia1.Metadata);
      aspect1_2.SetAttribute(mia1.ATTR_STRING, "1_2");
      aspect1_2.SetAttribute(mia1.ATTR_STRING, "one.two");
      aspects.Add(aspect1_2);

      Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");

      MockReader resourceReader = MockDBUtils.AddReader(1, "SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE SYSTEM_ID = @SYSTEM_ID AND PATH = @PATH", "MEDIA_ITEM_ID");
      resourceReader.AddResult(itemId);

      DateTime importDate;
      DateTime.TryParse("2014-10-11 12:34:56", out importDate);
      MockReader importReader = MockDBUtils.AddReader(2, "SELECT LASTIMPORTDATE A0, DIRTY A1, DATEADDED A2 FROM M_IMPORTEDITEM WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID", "A0", "A1", "A2");
      importReader.AddResult(importDate, "false", importDate);

      string pathStr = @"c:\item.mp3";
      MockReader mraReader = MockDBUtils.AddReader(3, "SELECT SYSTEM_ID A0, RESOURCEINDEX A1, ISPRIMARY A2, MIMETYPE A3, SIZE A4, PATH A5, PARENTDIRECTORY A6 FROM M_PROVIDERRESOURCE WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID", "MEDIA_ITEM_ID");
      mraReader.AddResult(null, 0, true, "audio/mp3", 100, pathStr, Guid.Empty);

      MockReader mia1Reader1 = MockDBUtils.AddReader(4, "SELECT MEDIA_ITEM_ID FROM M_MULTIPLE1 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ATTR_ID = @ATTR_ID", "MEDIA_ITEM_ID", "ATTR_ID");
      mia1Reader1.AddResult(itemId, "1_1");

      MockReader mia1Reader2 = MockDBUtils.AddReader(5, "SELECT MEDIA_ITEM_ID FROM M_MULTIPLE1 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ATTR_ID = @ATTR_ID", "MEDIA_ITEM_ID", "ATTR_ID");
      mia1Reader2.AddResult(itemId, "1_1");

      MockDBUtils.AddReader(6, "SELECT T4.MEDIA_ITEM_ID A20, T0.MEDIA_ITEM_ID A21, T1.MEDIA_ITEM_ID A22, T2.MEDIA_ITEM_ID A23, T3.MEDIA_ITEM_ID A24, T0.ATTR_ID A0, T0.ATTR_STRING A1, T1.SYSTEM_ID A2, " +
        "T1.RESOURCEINDEX A3, T1.ISPRIMARY A4, T1.MIMETYPE A5, T1.SIZE A6, T1.PATH A7, T1.PARENTDIRECTORY A8, T2.LASTIMPORTDATE A9, T2.DIRTY A10, T2.DATEADDED A11, T3.TITLE A12, T3.SORTTITLE A13, T3.RECORDINGTIME A14, " +
        "T3.RATING A15, T3.COMMENT A16, T3.PLAYCOUNT A17, T3.LASTPLAYED A18, T3.ISVIRTUAL A19 FROM MEDIA_ITEMS T4 LEFT OUTER JOIN M_MULTIPLE1 T0 ON T0.MEDIA_ITEM_ID = T4.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_PROVIDERRESOURCE T1 ON T1.MEDIA_ITEM_ID = T4.MEDIA_ITEM_ID LEFT OUTER JOIN M_IMPORTEDITEM T2 ON T2.MEDIA_ITEM_ID = T4.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_MEDIAITEM T3 ON T3.MEDIA_ITEM_ID = T4.MEDIA_ITEM_ID  WHERE T4.MEDIA_ITEM_ID = @V0", "V0");

      ResourcePath path = LocalFsResourceProviderBase.ToResourcePath(pathStr);
      MockCore.Library.AddOrUpdateMediaItem(Guid.Empty, null, path, aspects, true);

      MockCore.ShutdownLibrary();
    }

    [Test]
    public void TestEditBigMediaItem()
    {
      MockDBUtils.Reset();
      MockCore.SetupLibrary();

      SingleTestMIA mia1 = TestCommonUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, true);
      MockCore.Management.AddMediaItemAspectStorage(mia1.Metadata);

      MultipleTestMIA mia2 = TestCommonUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, true, false);
      MockCore.Management.AddMediaItemAspectStorage(mia2.Metadata);

      MultipleTestMIA mia3 = TestCommonUtils.CreateMultipleMIA("MULTIPLE3", Cardinality.Inline, false, true);
      MockCore.Management.AddMediaItemAspectStorage(mia3.Metadata);

      SingleTestMIA mia4 = TestCommonUtils.CreateSingleMIA("SINGLE4", Cardinality.Inline, true, true);
      MockCore.Management.AddMediaItemAspectStorage(mia4.Metadata);

      MockCore.Management.AddMediaItemAspectStorage(ProviderResourceAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(ImporterAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(MediaAspect.Metadata);

      IList<MediaItemAspect> aspects = new List<MediaItemAspect>();

      SingleMediaItemAspect aspect1 = new SingleMediaItemAspect(mia1.Metadata);
      aspect1.SetAttribute(mia1.ATTR_INTEGER, 1);
      aspect1.SetAttribute(mia1.ATTR_STRING, "one");
      aspects.Add(aspect1);

      MultipleMediaItemAspect aspect2_1 = new MultipleMediaItemAspect(mia2.Metadata);
      aspect2_1.SetAttribute(mia2.ATTR_ID, "2_1");
      aspect2_1.SetAttribute(mia2.ATTR_STRING, "two.one");
      aspects.Add(aspect2_1);
      MultipleMediaItemAspect aspect2_2 = new MultipleMediaItemAspect(mia2.Metadata);
      aspect2_2.SetAttribute(mia2.ATTR_ID, "2_2");
      aspect2_2.SetAttribute(mia2.ATTR_STRING, "two.two");
      aspects.Add(aspect2_2);

      MultipleMediaItemAspect aspect3_1 = new MultipleMediaItemAspect(mia3.Metadata);
      aspect3_1.SetAttribute(mia3.ATTR_ID, "3_1");
      aspect3_1.SetAttribute(mia3.ATTR_INTEGER, 31);
      aspects.Add(aspect3_1);
      MultipleMediaItemAspect aspect3_2 = new MultipleMediaItemAspect(mia3.Metadata);
      aspect3_2.SetAttribute(mia3.ATTR_ID, "3_2");
      aspect3_2.SetAttribute(mia3.ATTR_INTEGER, 32);
      aspects.Add(aspect3_2);
      MultipleMediaItemAspect aspect3_3 = new MultipleMediaItemAspect(mia3.Metadata);
      aspect3_3.Deleted = true;
      aspect3_3.SetAttribute(mia3.ATTR_INTEGER, 33);
      aspects.Add(aspect3_3);

      Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");

      MockReader resourceReader = MockDBUtils.AddReader(1, "SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE SYSTEM_ID = @SYSTEM_ID AND PATH = @PATH", "MEDIA_ITEM_ID");
      resourceReader.AddResult(itemId);

      DateTime importDate;
      DateTime.TryParse("2014-10-11 12:34:56", out importDate);
      MockReader importReader = MockDBUtils.AddReader(2, "SELECT LASTIMPORTDATE A0, DIRTY A1, DATEADDED A2 FROM M_IMPORTEDITEM WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID", "A0", "A1", "A2");
      importReader.AddResult(importDate, "false", importDate);

      string pathStr = @"c:\item.mp3";
      MockReader mraReader = MockDBUtils.AddReader(3, "SELECT SYSTEM_ID A0, RESOURCEINDEX A1, ISPRIMARY A2, MIMETYPE A3, SIZE A4, PATH A5, PARENTDIRECTORY A6 FROM M_PROVIDERRESOURCE WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID", "MEDIA_ITEM_ID");
      mraReader.AddResult("00000000-0000-0000-0000-000000000000", 0, true, "audio/mp3", 100, pathStr, Guid.Empty);

      MockReader mia1Reader = MockDBUtils.AddReader(4, "SELECT MEDIA_ITEM_ID FROM M_SINGLE1 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID", "MEDIA_ITEM_ID");
      mia1Reader.AddResult(itemId);

      MockReader mia2Reader1 = MockDBUtils.AddReader(5, "SELECT MEDIA_ITEM_ID FROM M_MULTIPLE2 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ATTR_ID = @ATTR_ID", "MEDIA_ITEM_ID", "ATTR_ID");
      mia2Reader1.AddResult(itemId, "1_1");

      MockReader mia2Reader2 = MockDBUtils.AddReader(6, "SELECT MEDIA_ITEM_ID FROM M_MULTIPLE2 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ATTR_ID = @ATTR_ID", "MEDIA_ITEM_ID", "ATTR_ID");
      mia2Reader2.AddResult(itemId, "1_1");

      MockDBUtils.AddReader(7, "SELECT MEDIA_ITEM_ID FROM M_MULTIPLE2 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ATTR_ID = @ATTR_ID", "MEDIA_ITEM_ID", "ATTR_ID");
      MockDBUtils.AddReader(8, "SELECT MEDIA_ITEM_ID FROM M_MULTIPLE2 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ATTR_ID = @ATTR_ID", "MEDIA_ITEM_ID", "ATTR_ID");
      MockDBUtils.AddReader(9, "SELECT MEDIA_ITEM_ID FROM M_MULTIPLE3 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ATTR_ID = @ATTR_ID", "MEDIA_ITEM_ID", "ATTR_ID");
      MockDBUtils.AddReader(10, "SELECT MEDIA_ITEM_ID FROM M_MULTIPLE3 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ATTR_ID = @ATTR_ID", "MEDIA_ITEM_ID", "ATTR_ID");

      MockDBUtils.AddReader(11, "SELECT T7.MEDIA_ITEM_ID A26, T0.MEDIA_ITEM_ID A27, T1.MEDIA_ITEM_ID A28, T2.MEDIA_ITEM_ID A29, T3.MEDIA_ITEM_ID A30, T4.MEDIA_ITEM_ID A31, " +
        "T5.MEDIA_ITEM_ID A32, T6.MEDIA_ITEM_ID A33, T0.ATTR_STRING A0, T0.ATTR_INTEGER A1, T1.ATTR_ID A2, T1.ATTR_STRING A3, T2.ATTR_ID A4, T2.ATTR_INTEGER A5, T3.ATTR_STRING A6, " +
        "T3.ATTR_INTEGER A7, T4.SYSTEM_ID A8, T4.RESOURCEINDEX A9, T4.ISPRIMARY A10, T4.MIMETYPE A11, T4.SIZE A12, T4.PATH A13, T4.PARENTDIRECTORY A14, T5.LASTIMPORTDATE A15, T5.DIRTY A16, " +
        "T5.DATEADDED A17, T6.TITLE A18, T6.SORTTITLE A19, T6.RECORDINGTIME A20, T6.RATING A21, T6.COMMENT A22, T6.PLAYCOUNT A23, T6.LASTPLAYED A24, T6.ISVIRTUAL A25 FROM MEDIA_ITEMS T7 " +
        "LEFT OUTER JOIN M_SINGLE1 T0 ON T0.MEDIA_ITEM_ID = T7.MEDIA_ITEM_ID LEFT OUTER JOIN M_MULTIPLE2 T1 ON T1.MEDIA_ITEM_ID = T7.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_MULTIPLE3 T2 ON T2.MEDIA_ITEM_ID = T7.MEDIA_ITEM_ID LEFT OUTER JOIN M_SINGLE4 T3 ON T3.MEDIA_ITEM_ID = T7.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_PROVIDERRESOURCE T4 ON T4.MEDIA_ITEM_ID = T7.MEDIA_ITEM_ID LEFT OUTER JOIN M_IMPORTEDITEM T5 ON T5.MEDIA_ITEM_ID = T7.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_MEDIAITEM T6 ON T6.MEDIA_ITEM_ID = T7.MEDIA_ITEM_ID  WHERE T7.MEDIA_ITEM_ID = @V0", "V0");

      ResourcePath path = LocalFsResourceProviderBase.ToResourcePath(pathStr);
      MockCore.Library.AddOrUpdateMediaItem(Guid.Empty, null, path, aspects, true);

      MockCore.ShutdownLibrary();
    }

    [Test]
    public void TestExternalMediaItem()
    {
      MockDBUtils.Reset();
      MockCore.SetupLibrary();

      SingleTestMIA mia1 = TestCommonUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, true);
      MockCore.Management.AddMediaItemAspectStorage(mia1.Metadata);

      MockCore.Management.AddMediaItemAspectStorage(ProviderResourceAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(ImporterAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(ExternalIdentifierAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(MediaAspect.Metadata);

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      SingleMediaItemAspect aspect1 = new SingleMediaItemAspect(mia1.Metadata);
      aspect1.SetAttribute(mia1.ATTR_INTEGER, 1);
      aspect1.SetAttribute(mia1.ATTR_STRING, "one");
      MediaItemAspect.SetAspect(aspects, aspect1);

      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, "test", ExternalIdentifierAspect.TYPE_EPISODE, "123");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, "test", ExternalIdentifierAspect.TYPE_SERIES, "456");

      MockDBUtils.AddReader(1, "SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE SYSTEM_ID = @SYSTEM_ID AND PATH = @PATH", "MEDIA_ITEM_ID");
      MockDBUtils.AddReader(2, "SELECT T5.MEDIA_ITEM_ID A23, T0.MEDIA_ITEM_ID A24, T1.MEDIA_ITEM_ID A25, T2.MEDIA_ITEM_ID A26, T3.MEDIA_ITEM_ID A27, T4.MEDIA_ITEM_ID A28, T0.ATTR_STRING A0, " +
        "T0.ATTR_INTEGER A1, T1.SYSTEM_ID A2, T1.RESOURCEINDEX A3, T1.ISPRIMARY A4, T1.MIMETYPE A5, T1.SIZE A6, T1.PATH A7, T1.PARENTDIRECTORY A8, T2.LASTIMPORTDATE A9, T2.DIRTY A10, T2.DATEADDED A11, " +
        "T3.SOURCE A12, T3.TYPE A13, T3.ID A14, T4.TITLE A15, T4.SORTTITLE A16, T4.RECORDINGTIME A17, T4.RATING A18, T4.COMMENT A19, T4.PLAYCOUNT A20, T4.LASTPLAYED A21, T4.ISVIRTUAL A22 FROM MEDIA_ITEMS T5 " +
        "LEFT OUTER JOIN M_SINGLE1 T0 ON T0.MEDIA_ITEM_ID = T5.MEDIA_ITEM_ID LEFT OUTER JOIN M_PROVIDERRESOURCE T1 ON T1.MEDIA_ITEM_ID = T5.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_IMPORTEDITEM T2 ON T2.MEDIA_ITEM_ID = T5.MEDIA_ITEM_ID LEFT OUTER JOIN M_EXTERNALIDENTIFIER T3 ON T3.MEDIA_ITEM_ID = T5.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_MEDIAITEM T4 ON T4.MEDIA_ITEM_ID = T5.MEDIA_ITEM_ID  WHERE T5.MEDIA_ITEM_ID = @V0", "@V0");

      string pathStr = "c:\\item.mp3";
      ResourcePath path = LocalFsResourceProviderBase.ToResourcePath(pathStr);
      MockCore.Library.AddOrUpdateMediaItem(Guid.Empty, null, path, aspects.Values.SelectMany(x => x), false);

      MockCore.ShutdownLibrary();
    }
  }
}
