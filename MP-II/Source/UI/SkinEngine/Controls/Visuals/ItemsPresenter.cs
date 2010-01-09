#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Used within the template of an item control to specify the place in the control’s visual tree 
  /// where the ItemsPanel defined by the ItemsControl is to be added.
  /// http://msdn2.microsoft.com/en-us/library/system.windows.controls.itemspresenter.aspx
  /// </summary>
  public class ItemsPresenter : Control, IScrollViewerFocusSupport, IScrollInfo
  {
    #region Protected fields

    protected char _startsWith = ' ';
    protected int _startsWithIndex = 0;
    protected Panel _itemsHostPanel = null;
    protected bool _canScroll = false;

    #endregion

    #region Ctor

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ItemsPresenter ip = (ItemsPresenter) source;
      _itemsHostPanel = copyManager.GetCopy(ip._itemsHostPanel);
      _canScroll = copyManager.GetCopy(ip._canScroll);
      UpdateCanScroll();
    }

    #endregion

    private void InvokeScrolled()
    {
      ScrolledDlgt dlgt = Scrolled;
      if (dlgt != null) dlgt(this);
    }

    protected void UpdateCanScroll()
    {
      IScrollInfo si = _itemsHostPanel as IScrollInfo;
      if (si != null)
      {
        si.CanScroll = _canScroll;
        si.Scrolled += OnItemsPanelScrolled; // Repeat the Scrolled event to our subscribers
      }
    }

    void OnItemsPanelScrolled(object sender)
    {
      InvokeScrolled();
    }

    public void ApplyTemplate(ItemsPanelTemplate template)
    {
      ControlTemplate ct = new ControlTemplate();
      ct.AddChild(template.LoadContent());
      Template = ct;
      _itemsHostPanel = TemplateControl.FindElement(ItemsHostFinder.Instance) as Panel;
      UpdateCanScroll();
    }

    public Panel ItemsHostPanel
    {
      get { return _itemsHostPanel; }
    }

    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      _startsWithIndex = -1;
    }

    public override void OnKeyPressed(ref Key key)
    {
      base.OnKeyPressed(ref key);

      if (key == Key.None)
        // Key event was handeled by child
        return;
      int updatedStartsWithIndex = -1;
      try
      {
        if (!CheckFocusInScope())
          return;

        if (key.IsPrintableKey)
        {
          if (_startsWithIndex == -1 || _startsWith != key.RawCode)
          {
            _startsWith = key.RawCode.Value;
            updatedStartsWithIndex = 0;
          }
          else
            updatedStartsWithIndex = _startsWithIndex + 1;
          key = Key.None;
          if (!FocusItemWhichStartsWith(_startsWith, updatedStartsWithIndex))
            updatedStartsWithIndex = -1;
        }
      }
      finally
      {
        // Will reset the startsWith function if no char was pressed
        _startsWithIndex = updatedStartsWithIndex;
      }
    }

    /// <summary>
    /// Checks if the currently focused control is contained in this scrollviewer and
    /// is not contained in a sub scrollviewer. This is necessary for this scrollviewer to
    /// handle the focus scrolling keys in this scope.
    /// </summary>
    private bool CheckFocusInScope()
    {
      Visual focusPath = Screen == null ? null : Screen.FocusedElement;
      while (focusPath != null)
      {
        if (focusPath == this)
          // Focused control is located in our focus scope
          return true;
        if (focusPath is ItemsPresenter)
          // Focused control is located in another itemspresenter's focus scope
          return false;
        focusPath = focusPath.VisualParent;
      }
      return false;
    }

    /// <summary>
    /// Moves the focus to the first child item whose content starts with the specified
    /// <paramref name="startsWith"/> character.
    /// </summary>
    /// <param name="startsWith">Character to search in the content.</param>
    /// <param name="index">Search the <paramref name="index"/>th occurence of
    /// <paramref name="startsWith"/>.</param>
    public bool FocusItemWhichStartsWith(char startsWith, int index)
    {
      startsWith = char.ToLower(startsWith);
      foreach (UIElement element in _itemsHostPanel.Children)
      {
        ISearchableItem searchItem = element as ISearchableItem;
        if (searchItem == null)
          continue;
        string dataString = searchItem.DataString.ToLower();
        if (!string.IsNullOrEmpty(dataString) && dataString.StartsWith(startsWith.ToString()))
        {
          if (index == 0)
          {
            FrameworkElement focusable = ScreenManagement.Screen.FindFirstFocusableElement(searchItem as FrameworkElement);
            if (focusable != null)
              focusable.HasFocus = true;
            return true;
          }
          index--;
        }
      }
      return false;
    }

    #region IScrollViewerFocusSupport implementation

    public bool FocusDown()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusDown();
    }

    public bool FocusLeft()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusLeft();
    }

    public bool FocusRight()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusRight();
    }

    public bool FocusUp()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusUp();
    }

    public bool FocusPageDown()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusPageDown();
    }

    public bool FocusPageUp()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusPageUp();
    }

    public bool FocusPageLeft()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusPageLeft();
    }

    public bool FocusPageRight()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusPageRight();
    }

    public bool FocusHome()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusHome();
    }

    public bool FocusEnd()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusEnd();
    }

    #endregion

    #region IScrollInfo implementation

    public event ScrolledDlgt Scrolled;

    public bool CanScroll
    {
      get { return _canScroll; }
      set
      {
        _canScroll = value;
        UpdateCanScroll();
      }
    }

    public float TotalWidth
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? (float) ActualWidth : si.TotalWidth;
      }
    }

    public float TotalHeight
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? (float) ActualHeight : si.TotalHeight;
      }
    }

    public float ViewPortWidth
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? 0 : si.ViewPortWidth;
      }
    }

    public float ViewPortStartX
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? 0 : si.ViewPortStartX;
      }
    }

    public float ViewPortHeight
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? 0 : si.ViewPortHeight;
      }
    }

    public float ViewPortStartY
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? 0 : si.ViewPortStartY;
      }
    }

    public bool IsViewPortAtTop
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? true : si.IsViewPortAtTop;
      }
    }

    public bool IsViewPortAtBottom
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? true : si.IsViewPortAtBottom;
      }
    }

    public bool IsViewPortAtLeft
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? true : si.IsViewPortAtLeft;
      }
    }

    public bool IsViewPortAtRight
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? true : si.IsViewPortAtRight;
      }
    }

    #endregion
  }
}
