using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class AnimatedStackPanel : VirtualizingStackPanel
  {
    public const string SCROLL_EVENT = "AnimatedStackPanel.Scroll";

    protected AbstractProperty _scrollOffsetMultiplierProperty;
    protected float _startOffsetX;
    protected float _diffOffsetX;
    protected bool _scrollingToFirst;

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

    public override void SetScrollIndex(int childIndex, bool first)
    {
      _startOffsetX = first ? _actualFirstVisibleChildIndex : _actualLastVisibleChildIndex;
      _diffOffsetX = childIndex - _startOffsetX;
      _scrollingToFirst = first;
      FireEvent(SCROLL_EVENT, RoutingStrategyEnum.VisualTree);
    }

    protected void OnMultiplierChanged(AbstractProperty property, object oldValue)
    {
      double multiplier = ScrollOffsetMultiplier;
      double newIndex = _startOffsetX + (_diffOffsetX * multiplier);
      SetPartialScrollIndex(newIndex, _scrollingToFirst);
    }
  }
}