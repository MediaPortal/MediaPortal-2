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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class AnimatedScrollContentPresenter : ScrollContentPresenter
  {
    public const string SCROLL_EVENT = "AnimatedScrollContentPresenter.Scroll";
    protected AbstractProperty _scrollOffsetMultiplierProperty;
    protected AbstractProperty _enableAnimationsProperty;
    protected float _startOffsetX;
    protected float _startOffsetY;
    protected float _diffOffsetX;
    protected float _diffOffsetY;

    public AnimatedScrollContentPresenter()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _scrollOffsetMultiplierProperty = new SProperty(typeof(double), 0d);
      _enableAnimationsProperty = new SProperty(typeof(bool), true);
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
      var ascp = (AnimatedScrollContentPresenter)source;
      ScrollOffsetMultiplier = ascp.ScrollOffsetMultiplier;
      EnableAnimations = ascp.EnableAnimations;
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

    public AbstractProperty EnableAnimationsProperty
    {
      get { return _enableAnimationsProperty; }
    }

    public bool EnableAnimations
    {
      get { return (bool)_enableAnimationsProperty.GetValue(); }
      set { _enableAnimationsProperty.SetValue(value); }
    }

    public override void SetScrollOffset(float scrollOffsetX, float scrollOffsetY)
    {
      if (!EnableAnimations)
      {
        base.SetScrollOffset(scrollOffsetX, scrollOffsetY);
        return;
      }
      _startOffsetX = _scrollOffsetX;
      _startOffsetY = _scrollOffsetY;
      _diffOffsetX = scrollOffsetX - _startOffsetX;
      _diffOffsetY = scrollOffsetY - _startOffsetY;
      FireEvent(SCROLL_EVENT, RoutingStrategyEnum.VisualTree);
    }

    protected void OnMultiplierChanged(AbstractProperty property, object oldValue)
    {
      float multiplier = (float)ScrollOffsetMultiplier;
      float newX = _startOffsetX + (_diffOffsetX * multiplier);
      float newY = _startOffsetY + (_diffOffsetY * multiplier);
      base.SetScrollOffset(newX, newY);
    }
  }
}
