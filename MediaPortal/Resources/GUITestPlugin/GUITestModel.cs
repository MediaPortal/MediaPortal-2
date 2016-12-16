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
using System.Diagnostics;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using MediaPortal.UI.SkinEngine.ScreenManagement;

namespace MediaPortal.Test.GUITest
{
  /// <summary>
  /// Model which holds the GUI state for the GUI test workflow state.
  /// </summary>
  public class GUITestModel : IWorkflowModel
  {
    public const string MODEL_ID_STR = "F4FC1599-F412-40d0-82BF-46FC352E93BE";

    #region Protected fields

    protected AbstractProperty _triggerTestMouseStateProperty = new WProperty(typeof(string), "Mouse not triggered yet");
    protected AbstractProperty _mouseCaptureTestMousePosProperty = new WProperty(typeof(string), "?/?");
    protected AbstractProperty _mouseCaptureTestCaptureSubTreeProperty = new WProperty(typeof(bool), false);
    protected AbstractProperty _mouseCaptureTestCaptureOriginalSourceProperty = new WProperty(typeof(bool), false);

    protected AbstractProperty _itemsProperty = new WProperty(typeof(ItemsList), new ItemsList());
    protected AbstractProperty _layoutTypeProperty = new WProperty(typeof(int), 0);

    #endregion

    #region public properties

    public AbstractProperty TriggerTestMouseStateProperty
    {
      get { return _triggerTestMouseStateProperty; }
    }

    public string TriggerTestMouseState
    {
      get { return (string) _triggerTestMouseStateProperty.GetValue(); }
      set { _triggerTestMouseStateProperty.SetValue(value); }
    }


    public AbstractProperty MouseCaptureTestMousePosProperty
    {
      get { return _mouseCaptureTestMousePosProperty; }
    }

    public string MouseCaptureTestMousePos
    {
      get { return (string) _mouseCaptureTestMousePosProperty.GetValue(); }
      set { _mouseCaptureTestMousePosProperty.SetValue(value); }
    }


    public AbstractProperty MouseCaptureTestCaptureSubTreeProperty
    {
      get { return _mouseCaptureTestCaptureSubTreeProperty; }
    }

    public bool MouseCaptureTestCaptureSubTree
    {
      get { return (bool) _mouseCaptureTestCaptureSubTreeProperty.GetValue(); }
      set { _mouseCaptureTestCaptureSubTreeProperty.SetValue(value); }
    }


    public AbstractProperty MouseCaptureTestCaptureOriginalSourceProperty
    {
      get { return _mouseCaptureTestCaptureOriginalSourceProperty; }
    }

    public bool MouseCaptureTestCaptureOriginalSource
    {
      get { return (bool) _mouseCaptureTestCaptureOriginalSourceProperty.GetValue(); }
      set { _mouseCaptureTestCaptureOriginalSourceProperty.SetValue(value); }
    }

    public AbstractProperty ItemsProperty
    {
      get { return _itemsProperty; }
    }

    public ItemsList Items
    {
      get { return (ItemsList)_itemsProperty.GetValue(); }
      set { _itemsProperty.SetValue(value); }
    }

    public AbstractProperty LayoutTypeProperty
    {
      get { return _layoutTypeProperty; }
    }

    public int LayoutType
    {
      get { return (int)_layoutTypeProperty.GetValue(); }
      set { _layoutTypeProperty.SetValue(value); }
    }

    #endregion

    #region Public members

    public void ShowScreenInTransientState(string screen)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePushTransient(new WorkflowState(Guid.NewGuid(), screen, screen, true, screen, false, true, ModelId, WorkflowType.Workflow), null);
    }

    public void RoutedEventHandler(object sender, MouseButtonEventArgs e)
    {
      ServiceRegistration.Get<ILogger>().Debug("RoutedEvent:");
      ServiceRegistration.Get<ILogger>().Debug("  Event=          {0}", e.RoutedEvent.Name);
      ServiceRegistration.Get<ILogger>().Debug("  sender=         {0}", sender);
      ServiceRegistration.Get<ILogger>().Debug("  Source=         {0}", e.Source);
      ServiceRegistration.Get<ILogger>().Debug("  OriginalSource= {0}", e.OriginalSource);
      ServiceRegistration.Get<ILogger>().Debug("  Hit at=         {0}", e.GetPosition(sender as UIElement));
    }

    public void TriggerTestMouseDown()
    {
      TriggerTestMouseState = "Mouse is down";
    }

    public void TriggerTestMouseUp()
    {
      TriggerTestMouseState = "Mouse is up";
    }

    public void MouseCaptureTestMouseMove(object sender, MouseEventArgs e)
    {
      UIElement element;
      if (MouseCaptureTestCaptureOriginalSource)
      {
        element = e.OriginalSource as UIElement;
      }
      else
      {
        element = e.Source as UIElement;
      }
      if (element != null)
      {
        var pt = e.GetPosition(element);
        MouseCaptureTestMousePos = String.Format("{0:F1}/{1:F1}", pt.X, pt.Y);
      }
    }

    public void MouseCaptureTestMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      UIElement element;
      if (MouseCaptureTestCaptureOriginalSource)
      {
        element = e.OriginalSource as UIElement;
      }
      else
      {
        element = e.Source as UIElement;
      }
      if (element != null)
      {
        if (MouseCaptureTestCaptureSubTree)
        {
          element.Screen.CaptureMouse(element, CaptureMode.SubTree);
        }
        else
        {
          element.CaptureMouse();
        }
      }
    }

    public void MouseCaptureTestMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      UIElement element;
      if (MouseCaptureTestCaptureOriginalSource)
      {
        element = e.OriginalSource as UIElement;
      }
      else
      {
        element = e.Source as UIElement;
      }
      if (element != null)
        element.ReleaseMouseCapture();
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // We could initialize some data here when entering the media navigation state
      InitGroupingTestItems();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // We could dispose some data here when exiting media navigation context
      CleanupGroupingTestItems();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion

    private void InitGroupingTestItems()
    {
      Items.Clear();
      int year = 1977;
      var random = new Random(1);
      int next = random.Next(0, 100);
      for (int n = 0; n < 1000; ++n)
      {
        var item = new ListItem("Name", String.Format("Item {0}", n + 1));
        item.AdditionalProperties.Add("Year", year);

        if (n == next)
        {
          next = random.Next(n + 1, n + 100);
          ++year;
        }

        Items.Add(item);
      }
      Items.FireChange();
    }

    private void CleanupGroupingTestItems()
    {
      Items.Clear();
      Items.FireChange();
    }

    public void SetLayoutType(int layoutType)
    {
      LayoutType = layoutType;
    }
  }
}
