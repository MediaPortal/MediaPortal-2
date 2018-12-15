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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UiComponents.WMCSkin.Models;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;

namespace MediaPortal.UiComponents.WMCSkin.Controls
{
  public class SubItemsContentPresenter : AnimatedScrollContentPresenter
  {
    public const string CURRENT_INDEX_CHANGED_EVENT = "AnimatedScrollContentPresenter.CurrentIndexChanged";

    protected AbstractProperty _currentIndexProperty;

    public SubItemsContentPresenter()
    {
      Init();
    }

    void Init()
    {
      _currentIndexProperty = new SProperty(typeof(int), 0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      var sicp = (SubItemsContentPresenter)source;
      CurrentIndex = sicp.CurrentIndex;
    }

    public AbstractProperty CurrentIndexProperty
    {
      get { return _currentIndexProperty; }
    }

    public int CurrentIndex
    {
      get { return (int)_currentIndexProperty.GetValue(); }
      set { _currentIndexProperty.SetValue(value); }
    }

    public override void BringIntoView(UIElement element, RectangleF elementBounds)
    {
      //disable auto centering if using mouse, prevents items from scrolling
      if (AutoCentering != ScrollAutoCenteringEnum.None && IsMouseOverElement(element) && !ShouldBringIntoView(element))
        return;
      UpdateCurrentIndex(element);
      FireEvent(CURRENT_INDEX_CHANGED_EVENT, RoutingStrategyEnum.Bubble);
      base.BringIntoView(element, elementBounds);
    }

    protected void UpdateCurrentIndex(UIElement element)
    {
      var lvi = element.FindParentOfType<ListViewItem>();
      if (lvi != null)
        CurrentIndex = lvi.ItemIndex;
    }

    protected bool IsMouseOverElement(UIElement element)
    {
      if (!ServiceRegistration.Get<IInputManager>().IsMouseUsed)
        return false;
      FrameworkElement frameworkElement = element as FrameworkElement;
      return frameworkElement != null && frameworkElement.IsMouseOver;
    }

    protected bool ShouldBringIntoView(UIElement element)
    {
      var lvi = element.FindParentOfType<ListViewItem>();
      if (lvi == null)
        return false;
      var item = lvi.Context as SubItem;
      return item != null && item.BringIntoView;
    }
  }
}
