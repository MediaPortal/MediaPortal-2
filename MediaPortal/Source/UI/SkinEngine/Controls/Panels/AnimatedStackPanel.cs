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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class AnimatedStackPanel : VirtualizingStackPanel
  {
    public const string SCROLL_EVENT = "AnimatedStackPanel.Scroll";

    protected AbstractProperty _scrollOffsetMultiplierProperty;
    protected double _startOffsetX;
    protected double _diffOffsetX;
    protected bool _scrollingToFirst;
    protected bool _isAnimating;

    public AnimatedStackPanel()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _scrollOffsetMultiplierProperty = new SProperty(typeof(double), 0d);
    }

    void Attach()
    {
      _scrollOffsetMultiplierProperty.Attach(OnMultiplierChanged);
    }

    void Detach()
    {
      _scrollOffsetMultiplierProperty.Detach(OnMultiplierChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      var ascp = (AnimatedStackPanel)source;
      ScrollOffsetMultiplier = ascp.ScrollOffsetMultiplier;
      Attach();
    }

    public AbstractProperty ScrollOffsetMultiplierProperty
    {
      get { return _scrollOffsetMultiplierProperty; }
    }

    public double ScrollOffsetMultiplier
    {
      get { return (double)_scrollOffsetMultiplierProperty.GetValue(); }
      set { _scrollOffsetMultiplierProperty.SetValue(value); }
    }

    public override void SetScrollIndex(double childIndex, bool first, bool force)
    {
      if (force)
      {
        _isAnimating = false;
        base.SetScrollIndex(childIndex, first, force);
      }
      else
      {
        _isAnimating = true;
        _startOffsetX = first ? _actualFirstVisibleChildIndex : _actualLastVisibleChildIndex;
        _diffOffsetX = childIndex - _startOffsetX;
        _scrollingToFirst = first;
        FireEvent(SCROLL_EVENT, RoutingStrategyEnum.VisualTree);
      }
    }

    protected void OnMultiplierChanged(AbstractProperty property, object oldValue)
    {
      if (!_isAnimating)
        return;
      double multiplier = ScrollOffsetMultiplier;
      double newIndex = _startOffsetX + (_diffOffsetX * multiplier);
      base.SetScrollIndex(newIndex, _scrollingToFirst, true);
    }
  }
}