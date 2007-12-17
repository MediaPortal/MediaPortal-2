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

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Collections;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using MediaPortal.Core.Commands;
using SkinEngine.Commands;
using SkinEngine.Scripts;
using SkinEngine.Skin;

namespace SkinEngine.Controls
{
  public class ListContainer : Control
  {
    public class ScrollContext
    {
      public ListContainer List;
      public Control FocusedControl;
      public Control NextControl;

      public ScrollContext(ListContainer list, Control focus, Control next)
      {
        List = list;
        FocusedControl = focus;
        NextControl = next;
      }

      public bool Process()
      {
        if (List.Animations.IsAnimating || List._styles.SelectedStyle.IsAnimating)
        {
          return false;
        }

        return true;
      }

      public void End()
      {
        if (List.ScrollingDown)
        {
          List.ScrollingDown = false;
          for (int i = 0; i < (int)List.Styles.SelectedStyle.Move.W; ++i)
          {
            List.MoveDown();
          }
        }
        else if (List.ScrollingUp)
        {
          List.ScrollingUp = false;
          for (int i = 0; i < (int)List.Styles.SelectedStyle.Move.Y; ++i)
          {
            List.MoveUp();
          }
        }
        else if (List.ScrollingLeft)
        {
          List.ScrollingLeft = false;
          if (List.SelectedItem != null && List.SelectedItem.SubItems.Count <= 1)
          {
            for (int i = 0; i < (int)List.Styles.SelectedStyle.Move.X; ++i)
            {
              List.MoveUp();
            }
          }
          else
          {
            for (int i = 0; i < (int)List.Styles.SelectedStyle.Move.X; ++i)
            {
              List.MoveLeft();
            }
          }
        }
        else if (List.ScrollingRight)
        {
          List.ScrollingRight = false;
          if (List.SelectedItem != null && List.SelectedItem.SubItems.Count <= 1)
          {
            for (int i = 0; i < (int)List.Styles.SelectedStyle.Move.Z; ++i)
            {
              List.MoveDown();
            }
          }
          else
          {
            for (int i = 0; i < (int)List.Styles.SelectedStyle.Move.Z; ++i)
            {
              List.MoveRight();
            }
          }
        }
        if (NextControl != null)
        {
          FocusedControl.HasFocus = true;
          NextControl.HasFocus = false;
          FocusedControl.SetState();
          NextControl.SetState();
        }
        List.SelectedItemProperty.SetValue(List.SelectedItem);
      }
    } ;

    #region variables

    private StylesCollection _styles;
    private Property _pageOffset;
    private Property _pageSize;
    private Property _subPageSize;
    private Property _scrollingLeft;
    private Property _scrollingRight;
    private Property _scrollingUp;
    private Property _scrollingDown;
    private Property _contentChange;
    private Property _contentAboutToChange;
    private readonly string _itemsProperty;
    private bool _hasRendered = false;
    private ScrollContext _scrollContext;
    private readonly Property _selectedItemProperty;
    private readonly Property _listItems;
    private readonly ItemsCollection.ItemsChangedHandler _handler;
    private bool _reactOnChildFocusChanges = true;
    private ICommand _onSelectedItemChangeCommand;
    private Property _isAnimatingProperty;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ListContainer"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    /// <param name="itemsProperty">The items property.</param>
    public ListContainer(Control parent, string itemsProperty)
      : base(parent)
    {
      _itemsProperty = itemsProperty;
      _styles = new StylesCollection(parent);
      IsFocusable = true;
      _selectedItemProperty = new Property("SelectedItemProperty");
      _scrollingLeft = new Property("ScrollingLeftProperty", false);
      _scrollingRight = new Property("ScrollingRightProperty", false);
      _scrollingUp = new Property("ScrollingUpProperty", false);
      _scrollingDown = new Property("ScrollingDownProperty", false);
      _contentChange = new Property("ContentChangeProperty", false);
      _contentAboutToChange = new Property("ContentAboutToChange", false);
      _pageSize = new Property("PageSizeProperty", -1);
      _pageOffset = new Property("PageOffsetProperty", 0);
      _listItems = new Property("ListItemsProperty", null);
      _isAnimatingProperty = new Property("", false);
      _subPageSize = new Property(0);
      _handler = Items_OnChanged;
    }

    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    /// <value>The items.</value>
    public ItemsCollection Items
    {
      get { return (ItemsCollection)_listItems.GetValue(); }
      set
      {
        if (Items != null)
        {
          Items.Changed -= _handler;
        }
        _listItems.SetValue(value);
        if (Items != null)
        {
          Items.Changed += _handler;
        }
      }
    }
    public Property IsAnimatingProperty
    {
      get { return _isAnimatingProperty; }
      set { _isAnimatingProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the items property.
    /// </summary>
    /// <value>The items property.</value>
    public Property ItemsProperty
    {
      get { return _listItems; }
      set { _listItems.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the scrolling left property.
    /// </summary>
    /// <value>The scrolling left property.</value>
    public Property ScrollingLeftProperty
    {
      get { return _scrollingLeft; }
      set { _scrollingLeft = value; }
    }

    /// <summary>
    /// Gets or sets the scrolling right property.
    /// </summary>
    /// <value>The scrolling right property.</value>
    public Property ScrollingRightProperty
    {
      get { return _scrollingRight; }
      set { _scrollingRight = value; }
    }

    /// <summary>
    /// Gets or sets the scrolling up property.
    /// </summary>
    /// <value>The scrolling up property.</value>
    public Property ScrollingUpProperty
    {
      get { return _scrollingUp; }
      set { _scrollingUp = value; }
    }

    /// <summary>
    /// Gets or sets the scrolling down property.
    /// </summary>
    /// <value>The scrolling down property.</value>
    public Property ScrollingDownProperty
    {
      get { return _scrollingDown; }
      set { _scrollingDown = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether we're current scrolling to the left of the listcontainer
    /// </summary>
    /// <value><c>true</c> if scrolling left; otherwise, <c>false</c>.</value>
    public bool ScrollingLeft
    {
      get { return (bool)_scrollingLeft.GetValue(); }
      set { _scrollingLeft.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether we're current scrolling to up of the listcontainer
    /// </summary>
    /// <value><c>true</c> if scrolling up; otherwise, <c>false</c>.</value>
    public bool ScrollingUp
    {
      get { return (bool)_scrollingUp.GetValue(); }
      set { _scrollingUp.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether we're current scrolling to down of the listcontainer
    /// </summary>
    /// <value><c>true</c> if scrolling down; otherwise, <c>false</c>.</value>
    public bool ScrollingDown
    {
      get { return (bool)_scrollingDown.GetValue(); }
      set { _scrollingDown.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether [content change].
    /// </summary>
    /// <value><c>true</c> if [content change]; otherwise, <c>false</c>.</value>
    public bool ContentChange
    {
      get { return (bool)_contentChange.GetValue(); }
      set { _contentChange.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the content change property.
    /// </summary>
    /// <value>The content change property.</value>
    public Property ContentChangeProperty
    {
      get { return _contentChange; }
      set { _contentChange = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether [content about to change].
    /// </summary>
    /// <value>
    /// 	<c>true</c> if [content about to change]; otherwise, <c>false</c>.
    /// </value>
    public bool ContentAboutToChange
    {
      get { return (bool)_contentAboutToChange.GetValue(); }
      set { _contentAboutToChange.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the content about to change property.
    /// </summary>
    /// <value>The content about to change property.</value>
    public Property ContentAboutToChangeProperty
    {
      get { return _contentAboutToChange; }
      set { _contentAboutToChange = value; }
    }


    /// <summary>
    /// Gets or sets a value indicating whether we're scrolling to the right
    /// </summary>
    /// <value><c>true</c> if scrolling right; otherwise, <c>false</c>.</value>
    public bool ScrollingRight
    {
      get { return (bool)_scrollingRight.GetValue(); }
      set { _scrollingRight.SetValue(value); }
    }


    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public override float Width
    {
      get { return _styles.SelectedStyle.Width; }
      set { _width = value; }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public override float Height
    {
      get { return _styles.SelectedStyle.Height; }
      set { _height = value; }
    }

    /// <summary>
    /// handles any keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      if (!HasFocus)
      {
        return;
      }
      if (key == Key.PageUp)
      {
        PageUp();
      }
      if (key == Key.PageDown)
      {
        PageDown();
      }
      if (key == Key.Home)
      {
        PageOffset = 0;
        SelectedSubItemIndex = 0;
      }
      if (key == Key.End)
      {
        PageOffset = (Items.Count - PageSize);
        SelectedSubItemIndex = 0;
      }


      _styles.SelectedStyle.OnKeyPressed(ref key);
      UpdateSelectedItem();
    }

    /// <summary>
    /// Gets or sets a value indicating whether this control has focus.
    /// </summary>
    /// <value><c>true</c> if this control has focus; otherwise, <c>false</c>.</value>
    public override bool HasFocus
    {
      get { return base.HasFocus; }
      set
      {
        if (base.HasFocus != value)
        {
          //          Trace.WriteLine("List "+this.Name+" Focus:" + value);
          _reactOnChildFocusChanges = false;
          base.HasFocus = value;
          if (value)
          {
            if (FocusedControl == null)
            {
              if (Items != null)
              {
                for (int i = 0; i < Items.Count; ++i)
                {
                  if (Items[i].Selected)
                  {
                    //                  Trace.WriteLine("  List "+this.Name+" set focus on selected item");
                    _styles.SelectedStyle.SetFocus(i - PageOffset);
                    _reactOnChildFocusChanges = true;
                    return;
                  }
                }
                //              Trace.WriteLine("  List " + this.Name + " set focus on item#0");
                _styles.SelectedStyle.HasFocus = true;
                _styles.SelectedStyle.SetFocus(0);
              }
            }
          }

          UpdateSelectedItem();
        }
        _reactOnChildFocusChanges = true;
      }
    }


    /// <summary>
    /// Gets the the number of focused controls on 1 page
    /// </summary>
    /// <value>The number of focused controls on 1 page.</value>
    public int PageSize
    {
      get
      {
        if (Items.Count == 0)
        {
          return 0;
        }
        if (((int)_pageSize.GetValue()) < 0)
        {
          _pageSize.SetValue(_styles.SelectedStyle.FocusableControlCount);
        }
        return (int)_pageSize.GetValue();
      }
      set { _pageSize.SetValue(value); }
    }

    public Property PageSizeProperty
    {
      get { return _pageSize; }
      set { _pageSize = value; }
    }

    /// <summary>
    /// Gets or sets the styles.
    /// </summary>
    /// <value>The styles.</value>
    public StylesCollection Styles
    {
      get { return _styles; }
      set { _styles = value; }
    }

    /// <summary>
    /// Gets or sets the current offset in ListContainer.Items 
    /// </summary>
    /// <value>The page offset.</value>
    public int PageOffset
    {
      get { return (int)_pageOffset.GetValue(); }
      set { _pageOffset.SetValue(value); }
    }

    public Property PageOffsetProperty
    {
      get { return _pageOffset; }
      set { _pageOffset = value; }
    }

    /// <summary>
    /// Resets the control and its animations.
    /// </summary>
    public override void Reset()
    {
      HasFocus = false;
      base.Reset();
      _styles.Reset();
      if (Items == null || Items.Count == 0 || UpdateItems() != Items)
      {
        Items = UpdateItems();
        PageSize = -1;
        PageOffset = 0;
        SelectedSubItemIndex = 0;
      }
      EnsureSelectedItemIsVisible();
    }

    /// <summary>
    /// Ensures that the selected item is visible.
    /// </summary>
    private void EnsureSelectedItemIsVisible()
    {
      if (Items == null) return;
      for (int i = 0; i < Items.Count; ++i)
      {
        if (Items[i].Selected)
        {
          int index = PageOffset = ((i / PageSize) * PageSize);
          if (index > 0) index--;
          PageOffset = index;
          return;
        }
      }
    }
    /// <summary>
    /// Updates the items by refreshing the databinding from listcontainer<->model
    /// </summary>
    /// <returns></returns>
    private ItemsCollection UpdateItems()
    {
      try
      {
        if (_itemsProperty != null)
        {
          string[] parts = _itemsProperty.Split(new char[] { '.' });
          object model = ObjectFactory.GetObject(Window, parts[0]);
          if (model != null)
          {
            if (parts[1].StartsWith("#script"))
            {
              string scriptName = parts[1].Substring("#script:".Length);
              if (ScriptManager.Instance.Contains(scriptName))
              {
                IProperty property = (IProperty)ScriptManager.Instance.GetScript(scriptName);
                return (ItemsCollection)property.Evaluate(model);
              }
              return new ItemsCollection();
            }

            int partNr = 1;
            object obj = null;
            while (partNr < parts.Length)
            {
              Type classType = model.GetType();
              PropertyInfo property = classType.GetProperty(parts[partNr],
                                                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                                            BindingFlags.InvokeMethod | BindingFlags.ExactBinding);
              if (property == null)
              {
                ServiceScope.Get<ILogger>().Error("Property {0} is not found", _itemsProperty);
                return new ItemsCollection();
              }
              obj = property.GetValue(model, null);
              //obj = classType.InvokeMember(parts[partNr], BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty, System.Type.DefaultBinder, model, null);
              partNr++;
              if (partNr < parts.Length)
              {
                model = obj;
                if (obj == null)
                {
                  return null;
                }
              }
            }
            return (ItemsCollection)obj;
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("ListContainer error updating items from model:{0}", _itemsProperty);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return new ItemsCollection();
    }


    /// <summary>
    /// Checks if control is positioned at coordinates (x,y)
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns></returns>
    public override bool HitTest(float x, float y)
    {
      return _styles.HitTest(x, y);
    }

    /// <summary>
    /// handles mouse movements
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      _styles.SelectedStyle.OnMouseMove(x, y);
      HasFocus = HitTest(x, y);

      UpdateSelectedItem();
    }


    /// <summary>
    /// Moves 1 page up
    /// </summary>
    public void PageUp()
    {
      PageOffset -= PageSize;
      if (PageOffset < 0)
      {
        PageOffset = 0;
      }

      UpdateSelectedItem();
      SelectedSubItemIndex = 0;
    }

    /// <summary>
    /// Moves 1 page down
    /// </summary>
    public void PageDown()
    {
      PageOffset += PageSize;
      if (PageOffset + PageSize >= Items.Count)
      {
        PageOffset = (Items.Count - PageSize);
      }
      SelectedSubItemIndex = 0;

      UpdateSelectedItem();
      SelectedSubItemIndex = 0;
    }

    /// <summary>
    /// Moves 1 item up
    /// </summary>
    private void MoveUp()
    {
      SelectedSubItemIndex = 0;
      PageOffset--;
      if (Wrap)
      {
        if (PageOffset < 0)
        {
          PageOffset += Items.Count;
        }
        return;
      }
      if (PageOffset < 0)
      {
        PageOffset = 0;
      }
    }

    /// <summary>
    /// Moves 1 item down
    /// </summary>
    private void MoveDown()
    {
      SelectedSubItemIndex = 0;
      PageOffset++;
      if (Wrap)
      {
        if (PageOffset + PageSize >= Items.Count)
        {
          PageOffset -= Items.Count;
        }
        return;
      }
      if (PageOffset + PageSize >= Items.Count)
      {
        PageOffset = (Items.Count - PageSize);
      }
    }

    private void MoveLeft()
    {
      if (SelectedSubItemIndex > 0)
      {
        SelectedSubItemIndex--;
      }
    }

    private void MoveRight()
    {
      if (SelectedItem == null)
      {
        SelectedSubItemIndex = 0;
        return;
      }
      if (SelectedSubItemIndex + 1 < SelectedItem.SubItems.Count)
      {
        SelectedSubItemIndex++;
      }
    }

    /// <summary>
    /// value indicating if the list should be wrapped or not
    /// if wrap=true then when you reach the end of ListContainer.ListItems
    /// the first item will be shown
    /// </summary>
    /// <value><c>true</c> if wrap is on; otherwise, <c>false</c>.</value>
    public bool Wrap
    {
      get
      {
        if (Items == null)
          return false;
        if (Items.Count == 0)
        {
          return false;
        }
        if (_styles.SelectedStyle == null)
        {
          return false;
        }
        if (Items.Count < PageSize)
        {
          return false;
        }
        return _styles.SelectedStyle.Wrap;
      }
    }

    /// <summary>
    /// Gets the index of the selected sub item.
    /// </summary>
    /// <value>The index of the selected sub item.</value>
    public int SelectedSubItemIndex
    {
      get { return (int)_subPageSize.GetValue(); }
      set { _subPageSize.SetValue(value); }
    }

    public Property SelectedSubItemIndexProperty
    {
      get { return _subPageSize; }
      set { _subPageSize = value; }
    }

    public Property SelectedItemProperty
    {
      get { return _selectedItemProperty; }
    }

    /// <summary>
    /// Gets the selected item.
    /// </summary>
    /// <value>The selected item.</value>
    public ListItem SelectedItem
    {
      get
      {
        if (!HasFocus || Items == null)
        {
          return null;
        }
        int index = _styles.SelectedStyle.FocusedControlIndex;

        int itemNr = index + PageOffset;
        if (itemNr >= 0 && itemNr < Items.Count)
        {
          return Items[itemNr];
        }
        if (Wrap)
        {
          if (itemNr < 0)
          {
            itemNr += Items.Count;
          }
          else
          {
            itemNr -= Items.Count;
          }
          if (itemNr >= 0 && itemNr < Items.Count)
          {
            return Items[itemNr];
          }
        }
        return null;
      }
    }



    private void Items_OnChanged(bool refreshAll)
    {
      if (refreshAll == false)
      {
        Items = new ItemsCollection();
        Items = UpdateItems();
        return;
      }
      Thread t = new Thread(new ThreadStart(RefreshDataBinding));
      t.Name = "ListRefresh";
      t.IsBackground = true;
      t.Start();
    }

    void RefreshDataBinding()
    {
      // Trace.WriteLine("--items changed event--");
      bool hasFocus = HasFocus;
      ItemsCollection itemsOld = new ItemsCollection();
      foreach (ListItem item in Items)
      {
        itemsOld.Add(item);
      }
      lock (_listItems)
      {
        // Trace.WriteLine("--set ContentAboutToChange--");
        ContentAboutToChange = true;
        _hasRendered = false;
      }
      if (ContentAboutToChange)
      {
        DateTime dt = SkinContext.Now;
        while (!_hasRendered)
        {
          Thread.Sleep(10);
          TimeSpan ts = SkinContext.Now - dt;
          if (ts.TotalSeconds >= 2)
          {
            break;
          }
        }
        TimeSpan ts1 = DateTime.Now - dt;
        Trace.WriteLine("--rendered :" + ts1.TotalMilliseconds.ToString());
        lock (_listItems)
        {
          //Trace.WriteLine("--update items--");
          ContentAboutToChange = false;
          Items = UpdateItems();

          //reset selected item to 1st item in the list...
          PageOffset = 0;
          PageSize = -1;
          SelectedSubItemIndex = 0;
          if (FocusedControl != null)
          {
            FocusedControl.HasFocus = false;
          }
          if (hasFocus)
            _styles.SelectedStyle.SetFocus(0);
          EnsureSelectedItemIsVisible();
          //Items.Changed += _handler;
          ContentChange = true;
          // Trace.WriteLine("--set ContentChange--");
        }
        DateTime dtNow = DateTime.Now;
        while (Animations.IsAnimating || _styles.SelectedStyle.IsAnimating)
        {
          Thread.Sleep(10);
          TimeSpan ts = DateTime.Now - dtNow;
          if (ts.TotalSeconds >= 2) break;
        }
        ts1 = DateTime.Now - dt;
        Trace.WriteLine("--animation done :" + ts1.TotalMilliseconds.ToString());

      }
      ContentChange = false;
      PageSize = -1;
      if (itemsOld.Count == Items.Count || itemsOld.Count == 0)
      {
        PageOffset = -1;
        PageOffset = 0;
      }

      // Trace.WriteLine("--all done--");
    }

    /// <summary>
    /// Gets the focused control.
    /// </summary>
    /// <value>The focused control.</value>
    public Control FocusedControl
    {
      get
      {
        if (_styles.SelectedStyle == null)
        {
          return null;
        }
        return _styles.SelectedStyle.FocusedControl;
      }
    }

    /// <summary>
    /// Predicts the next control which is position above this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public override Control PredictFocusUp(Control focusedControl, ref Key key, bool strict)
    {
      lock (this)
      {
        if (_scrollContext != null)
        {
          _scrollContext.End();
          _scrollContext = null;
          Thread.Sleep(50);
          if (HasFocus && FocusedControl != null)
          {
            focusedControl = FocusedControl;
          }
        }
      }
      if (HasFocus && _styles.SelectedStyle != null)
      {
        if (PageOffset + _styles.SelectedStyle.FocusedControlIndex > 0 || Wrap)
        {
          Control c = _styles.SelectedStyle.PredictFocusUp(focusedControl, ref key, strict);
          if (c == null)
          {
            if (_styles.SelectedStyle.Move.Y > 0)
            {
              ScrollingUp = true;
              Key k = key;
              Control cLeft = _styles.SelectedStyle.PredictControlUp(focusedControl, ref k);
              if (cLeft != null)
              {
                cLeft.HasFocus = true;
                focusedControl.HasFocus = false;
              }
              /*
              while (Animations.IsAnimating || _styles.SelectedStyle.IsAnimating)
              {
                System.Threading.Thread.Sleep(10);
              }
              _scrollingUp = false;

              for (int i = 0; i < (int)_styles.SelectedStyle.Move.Y; ++i)
                MoveUp();
              key = Key.None;
              if (cLeft != null)
              {
                focusedControl.HasFocus = true;
                cLeft.HasFocus = false;
                focusedControl.SetState();
                cLeft.SetState();
              }*/

              key = Key.None;
              lock (this)
              {
                _scrollContext = new ScrollContext(this, focusedControl, cLeft);
              }
              return null;
            }
          }
          else
          {
            key = Key.None;
            return c;
          }
        }
      }
      return _styles.SelectedStyle.PredictFocusUp(focusedControl, ref key, strict);
    }

    /// <summary>
    /// Predicts the next control which is position below this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public override Control PredictFocusDown(Control focusedControl, ref Key key, bool strict)
    {
      if (HasFocus && _styles.SelectedStyle != null)
      {
        lock (this)
        {
          if (_scrollContext != null)
          {
            _scrollContext.End();
            _scrollContext = null;
            Thread.Sleep(50);
            if (HasFocus && FocusedControl != null)
            {
              focusedControl = FocusedControl;
            }
          }
        }
        if (PageOffset + _styles.SelectedStyle.FocusedControlIndex + 1 < Items.Count || Wrap)
        {
          Control c = _styles.SelectedStyle.PredictFocusDown(focusedControl, ref key, strict);
          if (c == null)
          {
            if (_styles.SelectedStyle.Move.W > 0)
            {
              ScrollingDown = true;
              Key k = key;
              Control cLeft = _styles.SelectedStyle.PredictControlDown(focusedControl, ref k);
              if (cLeft != null)
              {
                focusedControl.HasFocus = false;
                cLeft.HasFocus = true;
              }
              /*
              //wait for animation should be done in the render thread
              //so we can keep responding to keypresses
              while (Animations.IsAnimating || _styles.SelectedStyle.IsAnimating)
              {
                System.Threading.Thread.Sleep(5);
                System.Windows.Forms.Application.DoEvents();
              }
              _scrollingDown = false;
              for (int i = 0; i < (int)_styles.SelectedStyle.Move.W; ++i)
                MoveDown();
              key = Key.None;
              if (cLeft != null)
              {
                focusedControl.HasFocus = true;
                cLeft.HasFocus = false;
                focusedControl.SetState();
                cLeft.SetState();
              }*/
              key = Key.None;
              lock (this)
              {
                _scrollContext = new ScrollContext(this, focusedControl, cLeft);
              }
              return null;
            }
          }
          else
          {
            key = Key.None;
            return c;
          }
        }
      }
      return _styles.SelectedStyle.PredictFocusDown(focusedControl, ref key, strict);
    }

    /// <summary>
    /// Predicts the next control which is position left of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public override Control PredictFocusLeft(Control focusedControl, ref Key key, bool strict)
    {
      lock (this)
      {
        if (_scrollContext != null)
        {
          _scrollContext.End();
          _scrollContext = null;
          Thread.Sleep(50);
          if (HasFocus && FocusedControl != null)
          {
            focusedControl = FocusedControl;
          }
        }
      }
      if (HasFocus && _styles.SelectedStyle != null)
      {
        if (PageOffset + _styles.SelectedStyle.FocusedControlIndex > 0 || Wrap)
        {
          Control c = _styles.SelectedStyle.PredictFocusLeft(focusedControl, ref key, strict);
          if (c == null)
          {
            if (_styles.SelectedStyle.Move.X > 0)
            {
              if (SelectedItem.SubItems.Count == 0 || SelectedSubItemIndex > 0)
              {
                ScrollingLeft = true;
                Key k = key;
                Control cLeft = _styles.SelectedStyle.PredictControlLeft(focusedControl, ref k);
                if (cLeft != null)
                {
                  focusedControl.HasFocus = false;
                  cLeft.HasFocus = true;
                }
                /*
                while (Animations.IsAnimating || _styles.SelectedStyle.IsAnimating)
                {
                  System.Threading.Thread.Sleep(10);
                }
                _scrollingLeft = false;
                for (int i = 0; i < (int)_styles.SelectedStyle.Move.X; ++i)
                  MoveUp();
                if (cLeft != null)
                {
                  focusedControl.HasFocus = true;
                  cLeft.HasFocus = false;
                  focusedControl.SetState();
                  cLeft.SetState();
                }
                key = Key.None;
                */
                key = Key.None;
                lock (this)
                {
                  _scrollContext = new ScrollContext(this, focusedControl, cLeft);
                }
              }
              return null;
            }
          }
          else
          {
            key = Key.None;
            return c;
          }
        }
      }
      return _styles.SelectedStyle.PredictFocusLeft(focusedControl, ref key, strict);
    }

    /// <summary>
    /// Predicts the next control which is position right of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public override Control PredictFocusRight(Control focusedControl, ref Key key, bool strict)
    {
      lock (this)
      {
        if (_scrollContext != null)
        {
          _scrollContext.End();
          _scrollContext = null;
          Thread.Sleep(50);
          if (HasFocus && FocusedControl != null)
          {
            focusedControl = FocusedControl;
          }
        }
      }
      if (HasFocus && _styles.SelectedStyle != null)
      {
        if (PageOffset + _styles.SelectedStyle.FocusedControlIndex + 1 < Items.Count || Wrap)
        {
          Control c = _styles.SelectedStyle.PredictFocusRight(focusedControl, ref key, strict);
          if (c == null)
          {
            if (_styles.SelectedStyle.Move.Z > 0)
            {
              if (SelectedItem.SubItems.Count == 0 || SelectedSubItemIndex + 1 < SelectedItem.SubItems.Count)
              {
                Key k = key;
                ScrollingRight = true;
                Control cLeft = _styles.SelectedStyle.PredictControlRight(focusedControl, ref k);
                if (cLeft != null)
                {
                  focusedControl.HasFocus = false;
                  cLeft.HasFocus = true;
                }
                /*
              
                while (Animations.IsAnimating || _styles.SelectedStyle.IsAnimating)
                {
                  System.Threading.Thread.Sleep(10);
                }
                _scrollingRight = false;
                for (int i = 0; i < (int)_styles.SelectedStyle.Move.Z; ++i)
                  MoveDown();
                if (cLeft != null)
                {
                  focusedControl.HasFocus = true;
                  cLeft.HasFocus = false;
                  focusedControl.SetState();
                  cLeft.SetState();
                }
                key = Key.None;*/
                key = Key.None;
                lock (this)
                {
                  _scrollContext = new ScrollContext(this, focusedControl, cLeft);
                }
              }
              return null;
            }
          }
          else
          {
            key = Key.None;
            return c;
          }
        }
      }
      return _styles.SelectedStyle.PredictFocusRight(focusedControl, ref key, strict);
    }

    public override void Render(uint timePassed)
    {
      IsAnimatingProperty.SetValue(this.IsAnimating);
      bool handling = SkinContext.HandlingInput;
      lock (this)
      {
        if (_scrollContext != null)
        {
          if (_scrollContext.Process())
          {
            SkinContext.HandlingInput = true;
            _scrollContext.End();
            _scrollContext = null;
          }
        }
      }
      lock (_listItems)
      {
        if (!IsVisible)
        {
          if (false == Animations.IsAnimating && false == _styles.SelectedStyle.IsAnimating)
          {
            return;
          }
        }
        base.Render(timePassed);
        _styles.SelectedStyle.DoRender(timePassed);
        _hasRendered = true;
      }
      SkinContext.HandlingInput = handling;
    }

    /// <summary>
    /// Switches to the next available style.
    /// </summary>
    public void SwitchStyle()
    {
      int style = _styles.SelectedStyleIndex + 1;
      if (style >= _styles.Styles.Count)
      {
        style = 0;
      }
      _styles.SelectedStyleIndex = style;
      PageOffset = 0;
      PageSize = -1;
      SelectedSubItemIndex = 0;

      UpdateSelectedItem();
    }

    /// <summary>
    /// Gets a value indicating whether this control is animating.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this control is animating; otherwise, <c>false</c>.
    /// </value>
    public override bool IsAnimating
    {
      get { return base.IsAnimating || _styles.SelectedStyle.IsAnimating; }
    }

    public override void OnChildFocusChanged(Control childControl)
    {
      if (!_reactOnChildFocusChanges)
      {
        return;
      }
      base.OnChildFocusChanged(childControl);
      //      if (HasFocus != _styles.SelectedStyle.HasFocus)
      //        Trace.WriteLine(String.Format("List:  " + this.Name + "child focuschange {0}", _styles.SelectedStyle.HasFocus));
      HasFocus = _styles.SelectedStyle.HasFocus;
    }

    /// <summary>
    /// Gets or sets the command to execute when the selected item has changed
    /// </summary>
    /// <value>The  command.</value>
    public ICommand OnSelectedItemChangeCommand
    {
      get
      {
        return _onSelectedItemChangeCommand;
      }
      set
      {
        _onSelectedItemChangeCommand = value;
      }
    }
    void UpdateSelectedItem()
    {
      if (_selectedItemProperty.GetValue() != SelectedItem)
      {
        _selectedItemProperty.SetValue(SelectedItem);

        if (_onSelectedItemChangeCommand != null)
        {
          _onSelectedItemChangeCommand.Execute(new StringParameter("this.SelectedItem"));
        }
      }
    }
  }
}