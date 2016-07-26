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
