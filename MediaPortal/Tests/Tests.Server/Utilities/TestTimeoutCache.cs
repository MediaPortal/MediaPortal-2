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
using System.Threading.Tasks;
using MediaPortal.Utilities.Cache;
using NUnit.Framework;

namespace Tests.Server.Utilities
{
  [TestFixture]
  public class TestTimeoutCache
  {
    [Test]
    public void TestTimeoutCacheGetOrAddNew()
    {
      //Arrange
      AsyncStaticTimeoutCache<int, object> cache = new AsyncStaticTimeoutCache<int, object>(TimeSpan.FromMinutes(1));
      object value = new object();
      
      //Act
      object cacheValue = cache.GetValue(1, _ => Task.FromResult(value)).Result;

      //Assert
      Assert.IsTrue(ReferenceEquals(value, cacheValue));
    }

    [Test]
    public void TestTimeoutCacheGetOrAddExisting()
    {
      //Arrange
      AsyncStaticTimeoutCache<int, object> cache = new AsyncStaticTimeoutCache<int, object>(TimeSpan.FromMinutes(1));
      object existingValue = new object();
      object newValue = new object();
      cache.GetValue(1, _ => Task.FromResult(existingValue));

      //Act
      object cacheValue = cache.GetValue(1, _ => Task.FromResult(newValue)).Result;

      //Assert
      Assert.IsTrue(ReferenceEquals(existingValue, cacheValue));
    }

    [Test]
    public void TestTimeoutCacheUpdateExisting()
    {
      //Arrange
      AsyncStaticTimeoutCache<int, object> cache = new AsyncStaticTimeoutCache<int, object>(TimeSpan.FromMinutes(1));
      object existingValue = new object();
      object newValue = new object();
      cache.GetValue(1, _ => Task.FromResult(existingValue));

      //Act
      object cacheValue = cache.UpdateValue(1, _ => Task.FromResult(newValue)).Result;

      //Assert
      Assert.IsTrue(ReferenceEquals(newValue, cacheValue));
    }

    [Test]
    public void TestTimeoutCacheUpdateNew()
    {
      //Arrange
      AsyncStaticTimeoutCache<int, object> cache = new AsyncStaticTimeoutCache<int, object>(TimeSpan.FromMinutes(1));
      object newValue = new object();

      //Act
      object cacheValue = cache.UpdateValue(1, _ => Task.FromResult(newValue)).Result;

      //Assert
      Assert.IsTrue(ReferenceEquals(newValue, cacheValue));
    }
  }
}
