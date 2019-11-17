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
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.UserProfileDataManagement;
using NUnit.Framework;

namespace Tests.Server.Backend
{
  [TestFixture]
  public class TestUserProfile
  {
    [Test]
    public void TestFeatureUsageDeSerialize()
    {
      for (var i = 0; i < 100; i++)
      {
        var usageStatistics = new UsageStatisticsList();
        usageStatistics.SetUsed("onlinevideos", "unit test");
        var serialized = UsageStatisticsList.Serialize(usageStatistics);
        Assert.NotNull(serialized);
        var deserialized = UsageStatisticsList.Deserialize(serialized);
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized[0].Scope);
        Assert.AreEqual(1, deserialized[0].TopUsed.Count);
        Assert.AreEqual(1, deserialized[0].LastUsed.Count);
      }
    }

    [Test]
    public void TestTopUsed()
    {
      var usageStatistics = new UsageStatistics();
      for (var i = 0; i < UsageStatistics.MAX_STORED_ENTRIES; i++)
        for (var j = 0; j <= i; j++)
        {
          usageStatistics.SetUsed("Unit Test-" + (i + 1));
        }

      var newStat = usageStatistics.LimitEntries(UsageStatistics.MAX_RETURNED_ENTRIES);
      Assert.AreEqual(UsageStatistics.MAX_RETURNED_ENTRIES, newStat.TopUsed.Count);
      Assert.AreEqual(UsageStatistics.MAX_RETURNED_ENTRIES, newStat.LastUsed.Count);

      Assert.AreEqual(20, newStat.TopUsed[0].CountUsed);
      Assert.AreEqual(15, newStat.TopUsed[5].CountUsed);
    }
  }
}
