#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using NMock2;
using NUnit.Framework;
using SkinEngine.Controls;

namespace SkinEngine
{
  [TestFixture]
  public class TestWindow
  {
    [Test]
    public void TestCtor()
    {
      Window window = new Window("testwindow");
      Assert.AreEqual(window.Name, "testwindow");
      Assert.IsTrue(window.IsOpened);
      Assert.IsFalse(window.WaitCursorVisible);
      Assert.IsFalse(window.WaitCursor.Visible);
      Assert.AreEqual(window.WindowState, Window.State.Running);
    }

    [Test]
    [ExpectedException(typeof (ArgumentNullException))]
    public void TestNullCtor()
    {
      Window window = new Window(null);
    }

    [Test]
    [ExpectedException(typeof (ArgumentOutOfRangeException))]
    public void TestInvalidCtor()
    {
      Window window = new Window("");
    }

    [Test]
    public void TestDefaultFocus()
    {
      Window window = new Window("testwindow");
      window.DefaultFocus = "mycontrol";
      Assert.AreEqual(window.DefaultFocus, "mycontrol");
    }

    [Test]
    [ExpectedException(typeof (ArgumentNullException))]
    public void TestNullDefaultFocus()
    {
      Window window = new Window("testwindow");
      window.DefaultFocus = null;
    }

    [Test]
    [ExpectedException(typeof (ArgumentOutOfRangeException))]
    public void TestInvalidDefaultFocus()
    {
      Window window = new Window("testwindow");
      window.DefaultFocus = "";
    }

    [Test]
    public void TestIsOpen()
    {
      Window window = new Window("testwindow");
      window.IsOpenedProperty = new Property(true);
      Assert.IsTrue(window.IsOpened);
      window.IsOpenedProperty = new Property(false);
      Assert.IsFalse(window.IsOpened);
    }


    [Test]
    public void TestAddModel()
    {
      Window window = new Window("testwindow");
      Model m = window.GetModelByName("somename");
      Assert.IsNull(m);
      Model model = new Model("assembly", "class", GetType(), this);
      window.AddModel("somename", model);

      m = window.GetModelByName("somename");
      Assert.AreEqual(m, model);
    }

    [Test]
    [ExpectedException(typeof (ArgumentNullException))]
    public void TestAddNullModel2()
    {
      Window window = new Window("testwindow");
      window.AddModel("somename", null);
    }

    [Test]
    [ExpectedException(typeof (ArgumentNullException))]
    public void TestAddNullModel3()
    {
      Window window = new Window("testwindow");
      Model model = new Model("assembly", "class", GetType(), this);
      window.AddModel(null, model);
    }

    [Test]
    [ExpectedException(typeof (ArgumentOutOfRangeException))]
    public void TestAddNullModel4()
    {
      Window window = new Window("testwindow");
      Model model = new Model("assembly", "class", GetType(), this);
      window.AddModel("", model);
    }


    [Test]
    public void TestAddControl()
    {
      Window window = new Window("testwindow");
      Control m = window.GetControlByName("somename");
      Assert.IsNull(m);
      Control control = new Control(null);
      window.AddControl(control);
      m = window.GetControlByName("somename");
      Assert.IsNull(m);

      control = new Control(null);
      control.Name = "somename";
      window.AddControl(control);
      m = window.GetControlByName("somename");
      Assert.AreEqual(m, control);
    }

    [Test]
    [ExpectedException(typeof (ArgumentNullException))]
    public void TestAddNullControl2()
    {
      Window window = new Window("testwindow");
      window.AddControl(null);
    }


    [Test]
    public void TestAddNamedControl()
    {
      Window window = new Window("testwindow");
      Control control = new Control(null);
      window.AddControl(control);
      Control m = window.GetControlByName("somename");
      Assert.IsNull(m);

      control.Name = "somename";
      window.AddNamedControl(control);
      m = window.GetControlByName("somename");
      Assert.AreEqual(m, control);
    }

    [Test]
    [ExpectedException(typeof (ArgumentNullException))]
    public void TestAddNamedControlNull()
    {
      Window window = new Window("testwindow");
      window.AddNamedControl(null);
    }

    [Test]
    [ExpectedException(typeof (ArgumentNullException))]
    public void TestAddNamedControlNull2()
    {
      Window window = new Window("testwindow");
      Control control = new Control(null);
      control.Name = null;
      window.AddNamedControl(control);
    }

    [Test]
    [ExpectedException(typeof (ArgumentOutOfRangeException))]
    public void TestAddNamedControlNull3()
    {
      Window window = new Window("testwindow");
      Control control = new Control(null);
      control.Name = "";
      window.AddNamedControl(control);
    }

    [Test]
    public void TestState()
    {
      Window window = new Window("testwindow");
      Assert.AreEqual(window.WindowState, Window.State.Running);
      window.WindowState = Window.State.Closing;
      Assert.AreEqual(window.WindowState, Window.State.Closing);
    }

    [Test]
    public void TestHasFocus()
    {
      Window window = new Window("testwindow");
      Assert.IsFalse(window.HasFocus);
      window.HasFocus = true;
      Assert.IsTrue(window.HasFocus);
    }

    [Test]
    public void TestControlsProperty()
    {
      Window window = new Window("testwindow");
      Assert.AreEqual(window.Controls.Count, 0);
      Control c1 = new Control(null);
      Control c2 = new Control(null);
      window.AddControl(c1);
      window.AddControl(c2);
      Assert.AreEqual(window.Controls.Count, 2);
      Assert.AreEqual(window.Controls[0], c1);
      Assert.AreEqual(window.Controls[1], c2);
    }

    [Test]
    public void TestWaitCursor()
    {
      Window window = new Window("testwindow");
      window.WaitCursorVisible = true;
      Assert.IsTrue(window.WaitCursorVisible);
      Assert.IsTrue(window.WaitCursor.Visible);
      window.WaitCursor.Visible = false;
      Assert.IsFalse(window.WaitCursorVisible);
      Assert.IsFalse(window.WaitCursor.Visible);
    }

    [Test]
    public void TestShow()
    {
      Window window = new Window("testwindow");
      Button c1 = new Button(null);
      c1.Name = "default";
      Button c2 = new Button(null);
      c2.Name = "second";
      window.AddControl(c1);
      window.AddControl(c2);
      window.DefaultFocus = c1.Name;
      using (Mockery mock = new Mockery())
      {
        IInputManager manager = mock.NewMock<IInputManager>();
        ServiceScope.Add<IInputManager>(manager);

        MockKeyPressedEvent keyEvent = new MockKeyPressedEvent();
        MockMouseMoveEvent mouseEvent = new MockMouseMoveEvent();
        Expect.Once.On(manager).EventAdd("OnKeyPressed", Is.Anything).Will(MockKeyPressedEvent.Hookup(keyEvent));
        Expect.Once.On(manager).EventAdd("OnMouseMove", Is.Anything).Will(MockMouseMoveEvent.Hookup(mouseEvent));
        window.Show(true);
        window.Show(true);
        mock.VerifyAllExpectationsHaveBeenMet();
      }
      Assert.IsTrue(c1.HasFocus);
      Assert.IsFalse(c2.HasFocus);
    }

    [Test]
    public void TestHide()
    {
      Window window = new Window("testwindow");
      using (Mockery mock = new Mockery())
      {
        IInputManager manager = mock.NewMock<IInputManager>();
        ServiceScope.Add<IInputManager>(manager);

        MockKeyPressedEvent keyEvent = new MockKeyPressedEvent();
        MockMouseMoveEvent mouseEvent = new MockMouseMoveEvent();
        Expect.Once.On(manager).EventAdd("OnKeyPressed", Is.Anything).Will(MockKeyPressedEvent.Hookup(keyEvent));
        Expect.Once.On(manager).EventAdd("OnMouseMove", Is.Anything).Will(MockMouseMoveEvent.Hookup(mouseEvent));
        Expect.Once.On(manager).EventRemove("OnKeyPressed", Is.Anything).Will(MockKeyPressedEvent.Hookup(keyEvent));
        Expect.Once.On(manager).EventRemove("OnMouseMove", Is.Anything).Will(MockMouseMoveEvent.Hookup(mouseEvent));
        window.Show(true);
        window.Hide();
        window.Hide();
        mock.VerifyAllExpectationsHaveBeenMet();
        ServiceScope.Remove<IInputManager>();
      }
    }

    [Test]
    public void TestKeyPress()
    {
      Window window = new Window("testwindow");
      using (Mockery mock = new Mockery())
      {
        ServiceScope.Add<IInputManager>(new InputManager());
        Key key = Key.Down;
        IControlExt c = mock.NewMock<IControlExt>();
        Expect.Once.On(c).Method("OnKeyPressed").With(key);
        Expect.AtLeastOnce.On(c).GetProperty("Name").Will(Return.Value("name"));
        window.AddControl(c);
        window.Show(false);
        ServiceScope.Get<IInputManager>().KeyPressed(key);
        window.HasFocus = true;
        ServiceScope.Get<IInputManager>().KeyPressed(key);
        ServiceScope.Remove<IInputManager>();
        mock.VerifyAllExpectationsHaveBeenMet();
      }
    }

    [Test]
    public void TestMouseMove()
    {
      Window window = new Window("testwindow");
      using (Mockery mock = new Mockery())
      {
        ServiceScope.Add<IInputManager>(new InputManager());
        IControlExt c = mock.NewMock<IControlExt>();
        float x = 20;
        float y = 20;
        Expect.Once.On(c).Method("OnMouseMove").With(x, y);
        Expect.AtLeastOnce.On(c).GetProperty("Name").Will(Return.Value("name"));
        window.AddControl(c);
        window.Show(false);
        ServiceScope.Get<IInputManager>().MouseMove(x, y);
        window.HasFocus = true;
        ServiceScope.Get<IInputManager>().MouseMove(x, y);
        ServiceScope.Remove<IInputManager>();
        mock.VerifyAllExpectationsHaveBeenMet();
      }
    }

    [Test]
    public void TestRender()
    {
      Window window = new Window("testwindow");
      using (Mockery mock = new Mockery())
      {
        IControlExt c = mock.NewMock<IControlExt>();
        IControlExt c2 = mock.NewMock<IControlExt>();
        Expect.AtLeastOnce.On(c).GetProperty("Name").Will(Return.Value(""));
        Expect.AtLeastOnce.On(c2).GetProperty("Name").Will(Return.Value(""));
        Expect.Once.On(c).Method("UpdateProperties").WithAnyArguments();
        Expect.Once.On(c).Method("DoRender").WithAnyArguments();
        Expect.Once.On(c2).Method("UpdateProperties").WithAnyArguments();
        Expect.Once.On(c2).Method("DoRender").WithAnyArguments();
        window.AddControl(c);
        window.AddControl(c2);
        window.Render();
        mock.VerifyAllExpectationsHaveBeenMet();
      }
    }

    [Test]
    public void TestIsAnimating()
    {
      Window window = new Window("testwindow");
      using (Mockery mock = new Mockery())
      {
        IControlExt c = mock.NewMock<IControlExt>();
        IControlExt c2 = mock.NewMock<IControlExt>();
        Expect.AtLeastOnce.On(c).GetProperty("Name").Will(Return.Value(""));
        Expect.AtLeastOnce.On(c2).GetProperty("Name").Will(Return.Value(""));
        Expect.Once.On(c).GetProperty("IsAnimating").Will(Return.Value(false));
        Expect.Once.On(c2).GetProperty("IsAnimating").Will(Return.Value(false));
        window.AddControl(c);
        window.AddControl(c2);
        bool result = window.IsAnimating;
        Assert.IsFalse(result);
        mock.VerifyAllExpectationsHaveBeenMet();
      }
    }

    [Test]
    public void TestIsAnimating2()
    {
      Window window = new Window("testwindow");
      using (Mockery mock = new Mockery())
      {
        IControlExt c = mock.NewMock<IControlExt>();
        IControlExt c2 = mock.NewMock<IControlExt>();
        Expect.AtLeastOnce.On(c).GetProperty("Name").Will(Return.Value(""));
        Expect.AtLeastOnce.On(c2).GetProperty("Name").Will(Return.Value(""));
        Expect.Once.On(c).GetProperty("IsAnimating").Will(Return.Value(false));
        Expect.Once.On(c2).GetProperty("IsAnimating").Will(Return.Value(true));
        window.AddControl(c);
        window.AddControl(c2);
        bool result = window.IsAnimating;
        Assert.IsTrue(result);
        mock.VerifyAllExpectationsHaveBeenMet();
      }
    }
  }
}

#endif
