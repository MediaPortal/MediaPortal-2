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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Used within the template of an item control to specify the place in the controls visual tree 
  /// where the ItemsPanel defined by the ItemsControl is to be added.
  /// http://msdn2.microsoft.com/en-us/library/system.windows.controls.itemspresenter.aspx
  /// </summary>
  public class ItemsPresenter : Control, IScrollViewerFocusSupport, IScrollInfo
  {
    #region Protected fields

    protected char _startsWith = ' ';
    protected int _startsWithIndex = 0;
    protected Panel _itemsHostPanel = null;
    protected bool _doScroll = false;
    protected IList<string> _dataStrings = null;

    #endregion

    #region Ctor

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      DetachScrolling();
      base.DeepCopy(source, copyManager);
      ItemsPresenter ip = (ItemsPresenter) source;
      _itemsHostPanel = copyManager.GetCopy(ip._itemsHostPanel);
      _doScroll = ip._doScroll;
      _dataStrings = ip._dataStrings;
      AttachScrolling();
    }

    #endregion

    private void InvokeScrolled()
    {
      ScrolledDlgt dlgt = Scrolled;
      if (dlgt != null) dlgt(this);
    }

    protected void DetachScrolling()
    {
      IScrollInfo si = _itemsHostPanel as IScrollInfo;
      if (si != null)
        si.Scrolled -= OnItemsPanelScrolled; // Repeat the Scrolled event to our subscribers
    }

    protected void AttachScrolling()
    {
      IScrollInfo si = _itemsHostPanel as IScrollInfo;
      if (si != null)
      {
        si.DoScroll = _doScroll;
        si.Scrolled += OnItemsPanelScrolled; // Repeat the Scrolled event to our subscribers
      }
    }

    protected void UpdateCanScroll()
    {
      IScrollInfo si = _itemsHostPanel as IScrollInfo;
      if (si != null)
        si.DoScroll = _doScroll;
    }

    void OnItemsPanelScrolled(object sender)
    {
      InvokeScrolled();
    }

    public void ApplyTemplate(ItemsPanelTemplate template)
    {
      DetachScrolling();
      FrameworkElement content = template.LoadContent() as FrameworkElement;
      FrameworkElement oldTemplateControl = TemplateControl;
      if (oldTemplateControl != null)
        oldTemplateControl.CleanupAndDispose();
      TemplateControl = content;
      if (content == null)
        _itemsHostPanel = null;
      else
      {
        content.LogicalParent = this;
        _itemsHostPanel = content.FindElement(ItemsHostMatcher.Instance) as Panel;
      }
      AttachScrolling();
    }

    public Panel ItemsHostPanel
    {
      get { return _itemsHostPanel; }
    }

    internal override void OnMouseMove(float x, float y, ICollection<FocusCandidate> focusCandidates)
    {
      base.OnMouseMove(x, y, focusCandidates);
      _startsWithIndex = -1;
    }

    protected override void OnKeyPress(KeyEventArgs e)
    {
      // migration from OnKeyPressed:
      // - no need to call base class
      // - no need to check if focus is in scope, since bubbling event is only raised from inside

      int updatedStartsWithIndex = -1;
      try
      {
        if (e.Key.IsPrintableKey)
        {
          if (e.Key.RawCode.HasValue &&  (_startsWithIndex == -1 || _startsWith != e.Key.RawCode))
          {
            _startsWith = e.Key.RawCode.Value;
            updatedStartsWithIndex = 0;
          }
          else
            updatedStartsWithIndex = _startsWithIndex + 1;
          e.Handled = true;
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

    public void SetDataStrings(IList<string> dataStrings)
    {
      _dataStrings = dataStrings == null ? null : new List<string>(dataStrings.Select(s => s == null ? null : s.ToLowerInvariant()));
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
      IList<string> dataStrings = _dataStrings;
      if (dataStrings == null || _itemsHostPanel == null)
        return false;
      string startsWithStr = char.ToLower(startsWith).ToString();
      FrameworkElement element = null;
      int elementIndex = -1;
      for (int i = 0; i < dataStrings.Count; i++)
      {
        string dataString = dataStrings[i];
        if (dataString == null)
          return false;
        if (dataString.StartsWith(startsWithStr))
        {
          if (index == 0)
          {
            FrameworkElement ele = _itemsHostPanel.GetElement(i);
            if (ele == null || !ele.IsVisible)
              continue;
            element = ele;
            elementIndex = i;
            break;
          }
          index--;
        }
      }
      if (element == null)
        return false;
      _itemsHostPanel.BringIntoView(elementIndex);
      element.SetFocusPrio = SetFocusPriority.Default; // For VirtualizingStackPanel, the item might not be in the visual tree yet so defer the focus setting to the next layouting
      return true;
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

    public bool ScrollDown(int numLines)
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.ScrollDown(numLines);
    }

    public bool ScrollUp(int numLines)
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.ScrollUp(numLines);
    }

    public bool Scroll(float deltaX, float deltaY)
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.Scroll(deltaX, deltaY);
    }

    public bool BeginScroll()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.BeginScroll();
    }

    public bool EndScroll()
    {
      IScrollViewerFocusSupport svfs = _itemsHostPanel as IScrollViewerFocusSupport;
      return svfs != null && svfs.EndScroll();
    }

    #endregion

    #region IScrollInfo implementation

    public event ScrolledDlgt Scrolled;

    public bool DoScroll
    {
      get { return _doScroll; }
      set
      {
        _doScroll = value;
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
        return si == null || si.IsViewPortAtTop;
      }
    }

    public bool IsViewPortAtBottom
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null || si.IsViewPortAtBottom;
      }
    }

    public bool IsViewPortAtLeft
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null || si.IsViewPortAtLeft;
      }
    }

    public bool IsViewPortAtRight
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null || si.IsViewPortAtRight;
      }
    }

    public int NumberOfVisibleLines
    {
      get
      {
        IScrollInfo si = _itemsHostPanel as IScrollInfo;
        return si == null ? 0 : si.NumberOfVisibleLines;
      }
    }

    #endregion
  }
}
