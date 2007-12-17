#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#if DEBUG
using System;
using NMock2;
using NUnit.Framework;
using SkinEngine.Skin;

namespace SkinEngine
{
  public class TestWindowManager
  {
    [Test]
    [ExpectedException(typeof (ArgumentNullException))]
    public void TestNullCtor()
    {
      WindowManager manager = new WindowManager(null);
    }


    [Test]
    public void TestLoadSkin()
    {
      using (Mockery mock = new Mockery())
      {
        ISkinLoader loader = mock.NewMock<ISkinLoader>();
        Expect.Once.On(loader).Method("Load").WithAnyArguments();
        WindowManager manager = new WindowManager(loader);
        manager.LoadSkin();
        Assert.IsNotNull(manager.CurrentWindow);


        mock.VerifyAllExpectationsHaveBeenMet();
      }
    }
  }
}

#endif