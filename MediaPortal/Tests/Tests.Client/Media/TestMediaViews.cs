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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.FilterTrees;
using NUnit.Framework;

namespace Tests.Media
{
  [TestFixture]
  public class TestMediaViews
  {
    Guid baseRole = new Guid("00000000-aaaa-aaaa-aaaa-111111111111");
    Guid item1Id = new Guid("11111111-aaaa-aaaa-aaaa-111111111111");
    Guid item1Role = new Guid("22222222-aaaa-aaaa-aaaa-111111111111");
    Guid item2Id = new Guid("33333333-aaaa-aaaa-aaaa-111111111111");
    Guid item2Role = new Guid("44444444-aaaa-aaaa-aaaa-111111111111");
    Guid item3Id = new Guid("55555555-aaaa-aaaa-aaaa-111111111111");
    Guid item3Role = new Guid("66666666-aaaa-aaaa-aaaa-111111111111");

    [Test]
    public void TestFilterTree()
    {
      IFilterTree tree = new RelationshipFilterTree(baseRole);
      tree.AddFilter(new RelationalFilter(MediaAspect.ATTR_TITLE, RelationalOperator.EQ, "Item2"),
        new FilterTreePath(item1Role, item2Role));
      tree.AddFilter(new RelationalFilter(MediaAspect.ATTR_TITLE, RelationalOperator.EQ, "Item3"),
        new FilterTreePath(item1Role, item3Role));
      tree.AddFilter(new RelationalFilter(MediaAspect.ATTR_TITLE, RelationalOperator.EQ, "Item1"),
       new FilterTreePath(item1Role));
      tree.AddFilter(new RelationalFilter(MediaAspect.ATTR_TITLE, RelationalOperator.EQ, "Item0"));

      //No linked ids, should build full filter in both directions
      TestFilterTreeNoLinkedIds(tree);
      //Test copy
      IFilterTree copy = tree.DeepCopy();
      TestFilterTreeNoLinkedIds(copy);

      //Add a linked id in the middle, should optimise by ignoring any filters on the linked media item
      tree.AddLinkedId(item1Id, new FilterTreePath(item1Role));
      TestFilterTreeWithLinkedIds(tree);
      //Test copy
      copy = tree.DeepCopy();
      TestFilterTreeWithLinkedIds(copy);
    }

    void TestFilterTreeNoLinkedIds(IFilterTree tree)
    {
      IFilter item0Filter = tree.BuildFilter();
      Assert.AreEqual(item0Filter.ToString(),
        "MediaItem.Title EQ Item0 And (ROLE = '00000000-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '22222222-aaaa-aaaa-aaaa-111111111111' AND LINKED_ID IN (MediaItem.Title EQ Item1 And (ROLE = '22222222-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '44444444-aaaa-aaaa-aaaa-111111111111' AND LINKED_ID IN (MediaItem.Title EQ Item2)) And (ROLE = '22222222-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '66666666-aaaa-aaaa-aaaa-111111111111' AND LINKED_ID IN (MediaItem.Title EQ Item3))))");

      IFilter item1Filter = tree.BuildFilter(new FilterTreePath(item1Role));
      Assert.AreEqual(item1Filter.ToString(),
        "MediaItem.Title EQ Item1 And (ROLE = '22222222-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '44444444-aaaa-aaaa-aaaa-111111111111' AND LINKED_ID IN (MediaItem.Title EQ Item2)) And (ROLE = '22222222-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '66666666-aaaa-aaaa-aaaa-111111111111' AND LINKED_ID IN (MediaItem.Title EQ Item3)) And (ROLE = '22222222-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '00000000-aaaa-aaaa-aaaa-111111111111' AND LINKED_ID IN (MediaItem.Title EQ Item0))");

      IFilter item2Filter = tree.BuildFilter(new FilterTreePath(item1Role, item2Role));
      Assert.AreEqual(item2Filter.ToString(),
        "MediaItem.Title EQ Item2 And (ROLE = '44444444-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '22222222-aaaa-aaaa-aaaa-111111111111' AND LINKED_ID IN (MediaItem.Title EQ Item1 And (ROLE = '22222222-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '66666666-aaaa-aaaa-aaaa-111111111111' AND LINKED_ID IN (MediaItem.Title EQ Item3)) And (ROLE = '22222222-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '00000000-aaaa-aaaa-aaaa-111111111111' AND LINKED_ID IN (MediaItem.Title EQ Item0))))");
    }

    void TestFilterTreeWithLinkedIds(IFilterTree tree)
    {
      IFilter item0Filter = tree.BuildFilter();
      Assert.AreEqual(item0Filter.ToString(),
        "MediaItem.Title EQ Item0 And (LINKED_ID = '11111111-aaaa-aaaa-aaaa-111111111111' AND ROLE = '00000000-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '22222222-aaaa-aaaa-aaaa-111111111111')");

      IFilter item1Filter = tree.BuildFilter(new FilterTreePath(item1Role));
      Assert.AreEqual(item1Filter.ToString(),
        "MEDIA_ITEM_ID = '11111111-aaaa-aaaa-aaaa-111111111111'");

      IFilter item2Filter = tree.BuildFilter(new FilterTreePath(item1Role, item2Role));
      Assert.AreEqual(item2Filter.ToString(),
        "MediaItem.Title EQ Item2 And (LINKED_ID = '11111111-aaaa-aaaa-aaaa-111111111111' AND ROLE = '44444444-aaaa-aaaa-aaaa-111111111111' AND LINKED_ROLE = '22222222-aaaa-aaaa-aaaa-111111111111')");
    }
  }
}
