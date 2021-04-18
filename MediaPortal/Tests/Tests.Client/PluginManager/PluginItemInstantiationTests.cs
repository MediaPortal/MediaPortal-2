#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Client.PluginManager
{
  [TestFixture]
  public class PluginItemInstantiationTests
  {
    protected static ManualResetEventSlim _firstObjectCreatedEvent;
    protected static ManualResetEventSlim _secondObjectCreatedEvent;
    protected static object _disposedPluginItem;
    
    [OneTimeSetUp]
    public void Init()
    {
      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.Debug, true));
    }

    [SetUp]
    public void SetUp()
    {
      _disposedPluginItem = null;
      _firstObjectCreatedEvent = new ManualResetEventSlim();
      _secondObjectCreatedEvent = new ManualResetEventSlim();
    }

    [TearDown]
    public void TearDown()
    {
      _firstObjectCreatedEvent.Dispose();
      _secondObjectCreatedEvent.Dispose();
      _disposedPluginItem = null;
    }

    /// <summary>
    /// Tests whether the same instance of a plugin item is returned when the same item is requested at the same time from multiple threads and whether any duplicated items are disposed.
    /// </summary>
    [Test]
    public void DuplicatePluginItemShouldBeDisposedAndNotReturned()
    {
      var mockPluginMetadata = new Mock<IPluginMetadata>();
      mockPluginMetadata.SetupGet(m => m.AssemblyFilePaths).Returns(() => new[] { Assembly.GetExecutingAssembly().Location });

      // PluginRuntime constructor is internal so create it by reflection
      var ctor = typeof(PluginRuntime).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
      PluginRuntime pluginRuntime = (PluginRuntime)ctor.Invoke(new[] { mockPluginMetadata.Object, new object() });

      MockBlockingPluginItem pluginItem1 = null;
      // This will block in the constructor of the plugin item to simulate a race condition where the same item type is created in different threads at the same time
      Task blockedTask = Task.Run(() => pluginItem1 = (MockBlockingPluginItem)pluginRuntime.InstantiatePluginObject(typeof(MockBlockingPluginItem).FullName));
      // Wait for the above item's constructor to be called
      _firstObjectCreatedEvent.Wait();

      // Now request the same item again, this will not block
      MockBlockingPluginItem pluginItem2 = (MockBlockingPluginItem)pluginRuntime.InstantiatePluginObject(typeof(MockBlockingPluginItem).FullName);
      // Release the first item and wait for it to complete, it should detect that another item has been created in the meantime and dispose the instance it created
      _secondObjectCreatedEvent.Set();
      blockedTask.Wait();

      // Assert that we get the same reference for both items and that the duplicated instance has been disposed
      Assert.NotNull(pluginItem1, "Plugin item was not created");
      Assert.AreSame(pluginItem1, pluginItem2, "Plugin item references are not equal");
      Assert.IsNotNull(_disposedPluginItem, "Duplicate plugin item was not disposed");
      Assert.AreNotSame(pluginItem1, _disposedPluginItem, "Wrong plugin item was disposed");

      // Assert that the correct item has been cached with the correct ref count
      var fi = typeof(PluginRuntime).GetField("_instantiatedObjects", BindingFlags.Instance | BindingFlags.NonPublic);
      var instatiatedObjects = (IDictionary<string, PluginRuntime.ObjectReference>)fi.GetValue(pluginRuntime);
      Assert.AreEqual(1, instatiatedObjects.Count, "Multiple instantiated object references cached");
      Assert.AreSame(pluginItem1, instatiatedObjects[typeof(MockBlockingPluginItem).FullName].Object, "Wrong object reference cached");
      Assert.AreEqual(2, instatiatedObjects[typeof(MockBlockingPluginItem).FullName].RefCounter, "Wrong object ref count");
    }
    
    public class MockBlockingPluginItem : IDisposable
    {
      public MockBlockingPluginItem()
      {
        // If this is the first object, block until the second object has been created
        if (!_firstObjectCreatedEvent.IsSet)
        {
          _firstObjectCreatedEvent.Set();
          _secondObjectCreatedEvent.Wait();
        }
      }

      public void Dispose()
      {
        Assert.IsNull(_disposedPluginItem, "Plugin item disposed multiple times");
        _disposedPluginItem = this;
      }
    }

  }
}
