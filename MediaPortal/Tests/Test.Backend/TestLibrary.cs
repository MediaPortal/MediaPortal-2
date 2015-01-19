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
using System.Collections.Generic;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using NUnit.Framework;

namespace Test.Backend
{
  [TestFixture]
  public class TestLibrary
  {
      [TestFixtureSetUp]
      public void OneTimeSetUp()
      {
          TestDbUtils.Setup();
          MIAUtils.Setup();
      }

      [SetUp]
      public void SetUp()
      {
          TestDbUtils.Reset();
          MIAUtils.Reset();
      }

    [Test]
    public void TestMediaItemAspectStorage()
    {
      MIAUtils.CreateSingleMIA("SINGLE", Cardinality.Inline, true, true);
      TestCommand singleCommand = TestDbUtils.FindCommand("CREATE TABLE M_SINGLE");
      Assert.IsNotNull(singleCommand, "Single create table command");
      // Columns and objects will be what we asked for
      Assert.AreEqual("CREATE TABLE M_SINGLE (MEDIA_ITEM_ID Guid, ATTR_STRING TEXT, ATTR_INTEGER Int32, CONSTRAINT PK PRIMARY KEY (MEDIA_ITEM_ID), CONSTRAINT FK FOREIGN KEY (MEDIA_ITEM_ID) REFERENCES MEDIA_ITEMS (MEDIA_ITEM_ID) ON DELETE CASCADE)", singleCommand.CommandText, "Single1 create table command");

      TestDbUtils.Reset();
      MIAUtils.CreateMultipleMIA("MULTIPLE", Cardinality.Inline, true, false);
      TestCommand multipleCommand = TestDbUtils.FindCommand("CREATE TABLE M_MULTIPLE");
      Assert.IsNotNull(multipleCommand, "Multiple create table command");
      // Columns and objects will be suffixed with _0 because the alises we asked for have already been given to Multiple1
      Assert.AreEqual("CREATE TABLE M_MULTIPLE (MEDIA_ITEM_ID Guid, INDEX_ID Int32, ATTR_STRING_0 TEXT, CONSTRAINT PK_0 PRIMARY KEY (MEDIA_ITEM_ID,INDEX_ID), CONSTRAINT FK_0 FOREIGN KEY (MEDIA_ITEM_ID) REFERENCES MEDIA_ITEMS (MEDIA_ITEM_ID) ON DELETE CASCADE)", multipleCommand.CommandText, "Multiple1 create table command");

      // TODO: Put this back when Many cardinalities are supported on multiple MIAMs
        /*
        TestDBUtils.Database.Reset();
        TestMIA.CreateMultipleMIAM("META3", Cardinality.OneToMany, true, true);
        TestCommand meta3Command = TestDBUtils.Database.FindCommand("CREATE TABLE M_META3");
        Assert.IsNotNull(meta3Command, "Meta3 create table command");
        Assert.AreEqual("CREATE TABLE M_META3 (MEDIA_ITEM_ID Guid, INDEX_ID Int32, ATTR2A TEXT, ATTR2B TEXT, CONSTRAINT PK_0 PRIMARY KEY (MEDIA_ITEM_ID,INDEX_ID), CONSTRAINT FK_0 FOREIGN KEY (MEDIA_ITEM_ID) REFERENCES MEDIA_ITEMS (MEDIA_ITEM_ID) ON DELETE CASCADE)", meta3Command.CommandText, "Meta3 create table command");
        */
    }

    [Test]
    public void TestMediaItemLoader_SingleMIAs_IdFilter()
    {
      SingleTestMIA single1 = MIAUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, false);
      SingleTestMIA single2 = MIAUtils.CreateSingleMIA("SINGLE2", Cardinality.Inline, false, true);

      Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      IList<Guid> ids = new List<Guid>();
      ids.Add(itemId);
      IFilter filter = new MediaItemIdFilter(ids);

      TestReader reader = TestDbUtils.AddReader("SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T1.MEDIA_ITEM_ID A4, T0.ATTR_STRING A0, T1.ATTR_INTEGER A1 FROM M_SINGLE1 T0 INNER JOIN M_SINGLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID  WHERE T0.MEDIA_ITEM_ID = @V0", "A2", "A3", "A4", "A0", "A1");
      reader.AddResult(itemId.ToString(), itemId.ToString(), itemId.ToString(), "zero", "0");

      Guid[] requiredAspects = new Guid[] { single1.ASPECT_ID, single2.ASPECT_ID};
      Guid[] optionalAspects = null;
      MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
      CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MIAUtils.Management, query);
      MediaItem result = compiledQuery.QueryMediaItem();
      Assert.AreEqual(itemId, result.MediaItemId, "MediaItem ID");
      // TODO: More asserts
    }

    [Test]
    public void TestMediaItemLoader_SingleMIAs_LikeFilter()
    {
        SingleTestMIA mia1 = MIAUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, false);
        SingleTestMIA mia2 = MIAUtils.CreateSingleMIA("SINGLE2", Cardinality.Inline, false, true);
        SingleTestMIA mia3 = MIAUtils.CreateSingleMIA("SINGLE3", Cardinality.Inline, true, true);

        Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");

        IFilter filter = new LikeFilter(mia1.ATTR_STRING, "%", null);

        TestReader reader = TestDbUtils.AddReader(
            "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, T1.MEDIA_ITEM_ID A6, T2.MEDIA_ITEM_ID A7, T0.ATTR_STRING A0, T1.ATTR_INTEGER A1, T2.ATTR_STRING_0 A2, T2.ATTR_INTEGER_0 A3 " +
            "FROM M_SINGLE1 T0 INNER JOIN M_SINGLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID LEFT OUTER JOIN M_SINGLE3 T2 ON T2.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID  WHERE T0.ATTR_STRING LIKE @V0",
            "A4", "A5", "A6", "A7", "A0", "A1", "A2", "A3");
        reader.AddResult(itemId.ToString(), itemId.ToString(), itemId.ToString(), itemId.ToString(), "zerozero", "11", "twotwo", "23");

        Guid[] requiredAspects = new Guid[] { mia1.ASPECT_ID, mia2.ASPECT_ID };
        Guid[] optionalAspects = new Guid[] { mia3.ASPECT_ID };
        MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
        CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MIAUtils.Management, query);
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
        Assert.AreEqual(23, value.GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute");
    }

    [Test]
    public void TestMediaItemLoader_MultipleMIAs_IdFilter()
    {
        MultipleTestMIA mia1 = MIAUtils.CreateMultipleMIA("MULTIPLE1", Cardinality.Inline, true, false);
        MultipleTestMIA mia2 = MIAUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, false, true);

        Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
        IList<Guid> ids = new List<Guid>();
        ids.Add(itemId);
        IFilter filter = new MediaItemIdFilter(ids);

        TestReader singleReader = TestDbUtils.AddReader("SELECT T0.MEDIA_ITEM_ID A0 FROM MEDIA_ITEMS T0  WHERE T0.MEDIA_ITEM_ID = @V0", "A0");
        singleReader.AddResult(itemId.ToString());

        TestReader multipleReader1 = TestDbUtils.AddReader("SELECT T0.MEDIA_ITEM_ID A1, T0.MEDIA_ITEM_ID A2, T0.INDEX_ID A3, T0.ATTR_STRING A0 FROM M_MULTIPLE1 T0  WHERE T0.MEDIA_ITEM_ID = @V0", "A1", "A2", "A3", "A0");
        multipleReader1.AddResult(itemId.ToString(), itemId.ToString(), "0", "oneone");

        TestReader multipleReader2 = TestDbUtils.AddReader("SELECT T0.MEDIA_ITEM_ID A1, T0.MEDIA_ITEM_ID A2, T0.INDEX_ID A3, T0.ATTR_INTEGER A0 FROM M_MULTIPLE2 T0  WHERE T0.MEDIA_ITEM_ID = @V0", "A1", "A2", "A3", "A0");
        multipleReader2.AddResult(itemId.ToString(), itemId.ToString(), "0", "21");
        multipleReader2.AddResult(itemId.ToString(), itemId.ToString(), "1", "22");

        Guid[] requiredAspects = new Guid[] { mia1.ASPECT_ID, mia2.ASPECT_ID };
        Guid[] optionalAspects = null;
        MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
        CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MIAUtils.Management, query);
        MediaItem result = compiledQuery.QueryMediaItem();
        //Console.WriteLine("Query result " + result.MediaItemId + ": " + string.Join(",", result.Aspects) + ": " + result);

        IList<MultipleMediaItemAspect> values;

        Assert.AreEqual(itemId, result.MediaItemId, "MediaItem ID");
        Assert.IsTrue(MediaItemAspect.TryGetAspects(result.Aspects, mia1.Metadata, out values), "MIA1");
        Assert.AreEqual(0, values[0].Index, "MIA1 index");
        Assert.AreEqual("oneone", values[0].GetAttributeValue(mia1.ATTR_STRING), "MIA1 string attibute");
        Assert.IsTrue(MediaItemAspect.TryGetAspects(result.Aspects, mia2.Metadata, out values), "MIA2");
        Assert.AreEqual(0, values[0].Index, "MIA1 index #0");
        Assert.AreEqual(21, values[0].GetAttributeValue(mia2.ATTR_INTEGER), "MIA2 integer attibute #0");
        Assert.AreEqual(1, values[1].Index, "MIA1 index #0");
        Assert.AreEqual(22, values[1].GetAttributeValue(mia2.ATTR_INTEGER), "MIA2 integer attibute #0");
    }

    [Test]
    public void TestMediaItemsLoader_SingleAndMultipleMIAs_BooleanLikeFilter()
    {
        SingleTestMIA mia1 = MIAUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, true);
        MultipleTestMIA mia2 = MIAUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, true, false);
        MultipleTestMIA mia3 = MIAUtils.CreateMultipleMIA("MULTIPLE3", Cardinality.Inline, false, true);

        Guid itemId0 = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
        Guid itemId1 = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
        
        IFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new List<IFilter> { new LikeFilter(mia1.ATTR_STRING, "%", null), new LikeFilter(mia2.ATTR_STRING, "%", null) });

        TestReader reader = TestDbUtils.AddReader("SELECT T0.MEDIA_ITEM_ID A2, T0.MEDIA_ITEM_ID A3, T0.ATTR_STRING A0, T0.ATTR_INTEGER A1 FROM M_SINGLE1 T0  WHERE (T0.ATTR_STRING LIKE @V0 AND T0.MEDIA_ITEM_ID IN(SELECT MEDIA_ITEM_ID FROM M_MULTIPLE2 WHERE MULTIPLE2.ATTR_STRING LIKE @V1))", "A2", "A3", "A0", "A1");
        reader.AddResult(itemId0.ToString(), itemId0.ToString(), "zero", "0");
        reader.AddResult(itemId1.ToString(), itemId1.ToString(), "one", "1");

        TestReader multipleReader2 = TestDbUtils.AddReader("SELECT T0.MEDIA_ITEM_ID A1, T0.MEDIA_ITEM_ID A2, T0.INDEX_ID A3, T0.ATTR_STRING_0 A0 FROM M_MULTIPLE2 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1)", "A1", "A2", "A3", "A0");
        multipleReader2.AddResult(itemId0.ToString(), itemId0.ToString(), "0", "zerozero");
        multipleReader2.AddResult(itemId0.ToString(), itemId0.ToString(), "1", "zeroone");
        multipleReader2.AddResult(itemId1.ToString(), itemId1.ToString(), "0", "onezero");

        TestReader multipleReader3 = TestDbUtils.AddReader("SELECT T0.MEDIA_ITEM_ID A1, T0.MEDIA_ITEM_ID A2, T0.INDEX_ID A3, T0.ATTR_INTEGER_0 A0 FROM M_MULTIPLE3 T0  WHERE T0.MEDIA_ITEM_ID IN (@V0, @V1)", "A1", "A2", "A3", "A0");
        multipleReader3.AddResult(itemId0.ToString(), itemId0.ToString(), "0", "10");
        multipleReader3.AddResult(itemId0.ToString(), itemId0.ToString(), "1", "11");
        multipleReader3.AddResult(itemId0.ToString(), itemId0.ToString(), "2", "12");
        multipleReader3.AddResult(itemId0.ToString(), itemId0.ToString(), "3", "13");
        multipleReader3.AddResult(itemId0.ToString(), itemId0.ToString(), "4", "14");
        multipleReader3.AddResult(itemId1.ToString(), itemId1.ToString(), "0", "20");

        Guid[] requiredAspects = new Guid[] { mia1.ASPECT_ID, mia2.ASPECT_ID };
        Guid[] optionalAspects = new Guid[] { mia3.ASPECT_ID };
        MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
        CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MIAUtils.Management, query);
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
        Assert.AreEqual(0, values[0].Index, "MIA2 index 0 #0");
        Assert.AreEqual("zerozero", values[0].GetAttributeValue(mia2.ATTR_STRING), "MIA2 string attibute 0 #0");
        Assert.AreEqual("zeroone", values[1].GetAttributeValue(mia2.ATTR_STRING), "MIA2 string attibute 1 #0");
        Assert.IsTrue(MediaItemAspect.TryGetAspects(results[0].Aspects, mia3.Metadata, out values), "MIA3 #0");
        Assert.AreEqual(5, values.Count, "MIA3 count #0");
        Assert.AreEqual(0, values[0].Index, "MIA3 index 0 #0");
        Assert.AreEqual(10, values[0].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 0 #0");
        Assert.AreEqual(1, values[1].Index, "MIA3 index 1 #0");
        Assert.AreEqual(11, values[1].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 1 #0");
        Assert.AreEqual(2, values[2].Index, "MIA3 index 2 #0");
        Assert.AreEqual(12, values[2].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 2 #0");
        Assert.AreEqual(3, values[3].Index, "MIA3 index 3 #0");
        Assert.AreEqual(13, values[3].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 3 #0");
        Assert.AreEqual(4, values[4].Index, "MIA3 index 4 #0");
        Assert.AreEqual(14, values[4].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 4 #0");

        Assert.AreEqual(itemId1, results[1].MediaItemId, "MediaItem ID #1");
        Assert.IsTrue(MediaItemAspect.TryGetAspect(results[1].Aspects, mia1.Metadata, out value), "MIA1 #0");
        Assert.AreEqual("one", value.GetAttributeValue(mia1.ATTR_STRING), "MIA1 string attibute #1");
        Assert.AreEqual(1, value.GetAttributeValue(mia1.ATTR_INTEGER), "MIA1 integer attibute #1");
        Assert.IsTrue(MediaItemAspect.TryGetAspects(results[1].Aspects, mia2.Metadata, out values), "MIA2 #1");
        Assert.AreEqual(1, values.Count, "MIA2 count #1");
        Assert.AreEqual(0, values[0].Index, "MIA2 index #1");
        Assert.AreEqual("onezero", values[0].GetAttributeValue(mia2.ATTR_STRING), "MIA2 string attibute 0 #1");
        Assert.IsTrue(MediaItemAspect.TryGetAspects(results[1].Aspects, mia3.Metadata, out values), "MIA3 #0");
        Assert.AreEqual(1, values.Count, "MIA3 count #1");
        Assert.AreEqual(0, values[0].Index, "MIA3 index 0 #1");
        Assert.AreEqual(20, values[0].GetAttributeValue(mia3.ATTR_INTEGER), "MIA3 integer attibute 0 #1");
    }

    [Test]
    public void TestMediaItemLoader_SingleMIAsUnusedOptional_IdFilter()
    {
        SingleTestMIA single1 = MIAUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, false);
        SingleTestMIA single2 = MIAUtils.CreateSingleMIA("SINGLE2", Cardinality.Inline, false, true);
        SingleTestMIA single3 = MIAUtils.CreateSingleMIA("SINGLE3", Cardinality.Inline, false, true);
        SingleTestMIA single4 = MIAUtils.CreateSingleMIA("SINGLE4", Cardinality.Inline, false, true);

        Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
        IList<Guid> ids = new List<Guid>();
        ids.Add(itemId);
        IFilter filter = new MediaItemIdFilter(ids);

        TestReader reader = TestDbUtils.AddReader("SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, T1.MEDIA_ITEM_ID A6, T2.MEDIA_ITEM_ID A7, T3.MEDIA_ITEM_ID A8, T0.ATTR_STRING A0, T1.ATTR_INTEGER A1, T2.ATTR_INTEGER_0 A2, T3.ATTR_INTEGER_1 A3 FROM M_SINGLE1 T0 INNER JOIN M_SINGLE2 T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID LEFT OUTER JOIN M_SINGLE3 T2 ON T2.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID LEFT OUTER JOIN M_SINGLE4 T3 ON T3.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID  WHERE T0.MEDIA_ITEM_ID = @V0", "A4", "A5", "A6", "A7", "A8", "A0", "A1", "A2", "A3");
        reader.AddResult(itemId.ToString(), itemId.ToString(), itemId.ToString(), itemId.ToString(), null, "zero", "0", "0", null);

        Guid[] requiredAspects = new Guid[] { single1.ASPECT_ID, single2.ASPECT_ID };
        Guid[] optionalAspects = new Guid[] { single3.ASPECT_ID, single4.ASPECT_ID };
        MediaItemQuery query = new MediaItemQuery(requiredAspects, optionalAspects, filter);
        CompiledMediaItemQuery compiledQuery = CompiledMediaItemQuery.Compile(MIAUtils.Management, query);
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
        MIAUtils.SetupLibrary();

        SingleTestMIA mia1 = MIAUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, true);
        MultipleTestMIA mia2 = MIAUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, true, false);
        MultipleTestMIA mia3 = MIAUtils.CreateMultipleMIA("MULTIPLE3", Cardinality.Inline, false, true);

        MIAUtils.Management.AddMediaItemAspectStorage(mia1.Metadata);
        MIAUtils.Management.AddMediaItemAspectStorage(mia2.Metadata);
        MIAUtils.Management.AddMediaItemAspectStorage(mia3.Metadata);
        MIAUtils.Management.AddMediaItemAspectStorage(ProviderResourceAspect.Metadata);
        MIAUtils.Management.AddMediaItemAspectStorage(ImporterAspect.Metadata);

        IList<MediaItemAspect> aspects = new List<MediaItemAspect>();

        SingleMediaItemAspect aspect1 = new SingleMediaItemAspect(mia1.Metadata);
        aspect1.SetAttribute(mia1.ATTR_INTEGER, 1);
        aspect1.SetAttribute(mia1.ATTR_STRING, "one");
        aspects.Add(aspect1);

        MultipleMediaItemAspect aspect2_1 = new MultipleMediaItemAspect(1, mia2.Metadata);
        aspect2_1.SetAttribute(mia2.ATTR_STRING, "two.one");
        aspects.Add(aspect2_1);
        MultipleMediaItemAspect aspect2_2 = new MultipleMediaItemAspect(2, mia2.Metadata);
        aspect2_2.SetAttribute(mia2.ATTR_STRING, "two.two");
        aspects.Add(aspect2_2);

        MultipleMediaItemAspect aspect3_1 = new MultipleMediaItemAspect(1, mia3.Metadata);
        aspect3_1.SetAttribute(mia3.ATTR_INTEGER, 31);
        aspects.Add(aspect3_1);
        MultipleMediaItemAspect aspect3_2 = new MultipleMediaItemAspect(2, mia3.Metadata);
        aspect3_2.SetAttribute(mia3.ATTR_INTEGER, 32);
        aspects.Add(aspect3_2);
        MultipleMediaItemAspect aspect3_3 = new MultipleMediaItemAspect(3, mia3.Metadata);
        aspect3_3.SetAttribute(mia3.ATTR_INTEGER, 33);
        aspects.Add(aspect3_3);

        TestDbUtils.AddReader("SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE SYSTEM_ID = @SYSTEM_ID AND PATH = @PATH", "MEDIA_ITEM_ID");

        string pathStr = "c:\\item.mp3";
        ResourcePath path = LocalFsResourceProviderBase.ToResourcePath(pathStr);
        MIAUtils.Library.AddOrUpdateMediaItem(Guid.Empty, null, path, aspects);

        MIAUtils.Shutdown();
    }

    [Test]
    public void TestEditMediaItem()
    {
        MIAUtils.SetupLibrary();

        SingleTestMIA mia1 = MIAUtils.CreateSingleMIA("SINGLE1", Cardinality.Inline, true, true);
        MIAUtils.Management.AddMediaItemAspectStorage(mia1.Metadata);

        MultipleTestMIA mia2 = MIAUtils.CreateMultipleMIA("MULTIPLE2", Cardinality.Inline, true, false);
        MIAUtils.Management.AddMediaItemAspectStorage(mia2.Metadata);

        MultipleTestMIA mia3 = MIAUtils.CreateMultipleMIA("MULTIPLE3", Cardinality.Inline, false, true);
        MIAUtils.Management.AddMediaItemAspectStorage(mia3.Metadata);

        SingleTestMIA mia4 = MIAUtils.CreateSingleMIA("SINGLE4", Cardinality.Inline, true, true);
        MIAUtils.Management.AddMediaItemAspectStorage(mia4.Metadata);

        MIAUtils.Management.AddMediaItemAspectStorage(ProviderResourceAspect.Metadata);
        MIAUtils.Management.AddMediaItemAspectStorage(ImporterAspect.Metadata);

        IList<MediaItemAspect> aspects = new List<MediaItemAspect>();

        SingleMediaItemAspect aspect1 = new SingleMediaItemAspect(mia1.Metadata);
        aspect1.SetAttribute(mia1.ATTR_INTEGER, 1);
        aspect1.SetAttribute(mia1.ATTR_STRING, "one");
        aspects.Add(aspect1);

        MultipleMediaItemAspect aspect2_1 = new MultipleMediaItemAspect(1, mia2.Metadata);
        aspect2_1.SetAttribute(mia2.ATTR_STRING, "two.one");
        aspects.Add(aspect2_1);
        MultipleMediaItemAspect aspect2_2 = new MultipleMediaItemAspect(2, mia2.Metadata);
        aspect2_2.SetAttribute(mia2.ATTR_STRING, "two.two");
        aspects.Add(aspect2_2);

        MultipleMediaItemAspect aspect3_1 = new MultipleMediaItemAspect(1, mia3.Metadata);
        aspect3_1.SetAttribute(mia3.ATTR_INTEGER, 31);
        aspects.Add(aspect3_1);
        MultipleMediaItemAspect aspect3_2 = new MultipleMediaItemAspect(2, mia3.Metadata);
        aspect3_2.SetAttribute(mia3.ATTR_INTEGER, 32);
        aspects.Add(aspect3_2);
        MultipleMediaItemAspect aspect3_3 = new MultipleMediaItemAspect(3, mia3.Metadata);
        aspect3_3.Deleted = true;
        aspect3_3.SetAttribute(mia3.ATTR_INTEGER, 33);
        aspects.Add(aspect3_3);

        Guid itemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");

        TestReader resourceReader = TestDbUtils.AddReader("SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE SYSTEM_ID = @SYSTEM_ID AND PATH = @PATH", "MEDIA_ITEM_ID");
        resourceReader.AddResult(itemId.ToString());

        DateTime importDate;
        DateTime.TryParse("2014-10-11 12:34:56", out importDate);
        TestReader importReader = TestDbUtils.AddReader("SELECT LASTIMPORTDATE A0, DIRTY A1, DATEADDED A2 FROM M_IMPORTEDITEM WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID", "A0", "A1", "A2");
        resourceReader.AddResult(importDate.ToString(), "false", importDate.ToString());

        TestReader mia1Reader = TestDbUtils.AddReader("SELECT MEDIA_ITEM_ID FROM M_SINGLE1 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID", "MEDIA_ITEM_ID");
        mia1Reader.AddResult(itemId.ToString());

        TestReader mia2Reader = TestDbUtils.AddReader("SELECT MEDIA_ITEM_ID FROM M_MULTIPLE2 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND INDEX_ID = @INDEX_ID", "MEDIA_ITEM_ID");
        mia2Reader.AddResult(itemId.ToString());

        TestReader mia3Reader = TestDbUtils.AddReader("SELECT MEDIA_ITEM_ID FROM M_MULTIPLE3 WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND INDEX_ID = @INDEX_ID", "MEDIA_ITEM_ID");
        //mia3Reader.AddResult(itemId.ToString());
        
        string pathStr = "c:\\item.mp3";
        ResourcePath path = LocalFsResourceProviderBase.ToResourcePath(pathStr);
        MIAUtils.Library.AddOrUpdateMediaItem(Guid.Empty, null, path, aspects);

        MIAUtils.Shutdown();
    }
  }
}
