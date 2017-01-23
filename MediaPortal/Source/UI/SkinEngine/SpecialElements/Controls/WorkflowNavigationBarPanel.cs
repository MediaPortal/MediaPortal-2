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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;


namespace MediaPortal.UI.SkinEngine.SpecialElements.Controls
{
  /// <summary>
  /// Visible control providing the workflow navigation bar for the skin.
  /// </summary>
  public class WorkflowNavigationBarPanel : Panel
  {
    #region Protected fields

    protected AbstractProperty _orientationProperty;
    protected AbstractProperty _ellipsisControlStyleProperty;
    protected FrameworkElement _ellipsisControl;

    #endregion

    #region Ctor

    public WorkflowNavigationBarPanel()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _orientationProperty = new SProperty(typeof(Orientation), Orientation.Vertical);
      _ellipsisControlStyleProperty = new SProperty(typeof(Style), null);
    }

    void Attach()
    {
      _orientationProperty.Attach(OnCompleteLayoutGetsInvalid);
      _ellipsisControlStyleProperty.Attach(OnCompleteLayoutGetsInvalid);
    }

    void Detach()
    {
      _orientationProperty.Detach(OnCompleteLayoutGetsInvalid);
      _ellipsisControlStyleProperty.Detach(OnCompleteLayoutGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      WorkflowNavigationBarPanel wnbp = (WorkflowNavigationBarPanel) source;
      EllipsisControlStyle = copyManager.GetCopy(wnbp.EllipsisControlStyle);
      Orientation = wnbp.Orientation;
      Attach();
    }

    #endregion

    #region Public properties

    public AbstractProperty EllipsisControlStyleProperty
    {
      get { return _ellipsisControlStyleProperty; }
    }

    public Style EllipsisControlStyle
    {
      get { return (Style) _ellipsisControlStyleProperty.GetValue(); }
      set { _ellipsisControlStyleProperty.SetValue(value); }
    }

    public AbstractProperty OrientationProperty
    {
      get { return _orientationProperty; }
    }

    public Orientation Orientation
    {
      get { return (Orientation) _orientationProperty.GetValue(); }
      set { _orientationProperty.SetValue(value); }
    }

    #endregion

    #region Layouting

    protected FrameworkElement CreateEllipsisControl()
    {
// ReSharper disable UseObjectOrCollectionInitializer
      FrameworkElement result = new SkinEngine.Controls.Visuals.Control
// ReSharper restore UseObjectOrCollectionInitializer
        {
            VisualParent = this,
            Screen = Screen,
            ElementState = _elementState,
        };
      // Set the style after all other properties have been set to avoid doing work multiple times
      result.Style = EllipsisControlStyle;
      return result;
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      // Return the biggest available child extents
      SizeF childSize;
      SizeF maxChildSize = new SizeF(float.NaN, float.NaN);
      foreach (FrameworkElement child in GetVisibleChildren())
      {
        childSize = new SizeF(totalSize.Width, totalSize.Height);
        child.Measure(ref childSize);
        if (float.IsNaN(maxChildSize.Width) || childSize.Width > maxChildSize.Width)
          maxChildSize.Width = childSize.Width;
        if (float.IsNaN(maxChildSize.Height) || childSize.Height > maxChildSize.Height)
          maxChildSize.Height = childSize.Height;
      }
      if (_ellipsisControl == null)
        _ellipsisControl = CreateEllipsisControl();
      childSize = new SizeF(totalSize.Width, totalSize.Height);
      _ellipsisControl.Measure(ref childSize);
      if (float.IsNaN(maxChildSize.Width) || childSize.Width > maxChildSize.Width)
        maxChildSize.Width = childSize.Width;
      if (float.IsNaN(maxChildSize.Height) || childSize.Height > maxChildSize.Height)
        maxChildSize.Height = childSize.Height;
      return maxChildSize;
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();

      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count > 0)
      {
        float actualWidth = (float) ActualWidth;
        float actualHeight = (float) ActualHeight;

        // First check how many children we must leave out
        float availableSize = Orientation == Orientation.Vertical ? actualHeight : actualWidth;
        FrameworkElement firstChild = visibleChildren[0];
        SizeF firstChildDesiredSize = firstChild.DesiredSize;
        SizeF desiredEllipsisSize = _ellipsisControl == null ? new SizeF() : _ellipsisControl.DesiredSize;
        // The first element is always shown
        availableSize -= Orientation == Orientation.Vertical ? firstChildDesiredSize.Height : firstChildDesiredSize.Width;
        List<FrameworkElement> reversedChildren = new List<FrameworkElement>(visibleChildren);
        reversedChildren.Reverse();
        reversedChildren.RemoveAt(reversedChildren.Count - 1); // Remove first (home) element
        int numShownChildrenAfterEllipsis = 0; // Number of children which fit behind the ellipsis control
        float ellipsisSize = Orientation == Orientation.Vertical ? desiredEllipsisSize.Height : desiredEllipsisSize.Width;
        foreach (FrameworkElement child in reversedChildren)
        {
          SizeF desiredChildSize = child.DesiredSize;
          float size = Orientation == Orientation.Vertical ? desiredChildSize.Height : desiredChildSize.Width;
          if (availableSize >= size + ellipsisSize ||
              (availableSize >= size && numShownChildrenAfterEllipsis == visibleChildren.Count - 2))
          {
            availableSize -= size;
            numShownChildrenAfterEllipsis++;
          }
          else
            break;
        }
        float startPositionX = 0;
        float startPositionY = 0;
        List<FrameworkElement> childrenAfterEllipsis = new List<FrameworkElement>(visibleChildren);
        if (numShownChildrenAfterEllipsis < visibleChildren.Count - 1)
        { // Ellipsis necessary
          // Lay out first (home) element
          SizeF childSize = firstChild.DesiredSize;
          PointF position = new PointF(ActualPosition.X + startPositionX, ActualPosition.Y + startPositionY);

          if (Orientation == Orientation.Vertical)
          {
            childSize.Width = actualWidth;
            ArrangeChildHorizontal(firstChild, firstChild.HorizontalAlignment, ref position, ref childSize);
            startPositionY += childSize.Height;
          }
          else
          {
            childSize.Height = actualHeight;
            ArrangeChildVertical(firstChild, firstChild.VerticalAlignment, ref position, ref childSize);
            startPositionX += childSize.Width;
          }

          firstChild.Arrange(SharpDXExtensions.CreateRectangleF(position, childSize));

          // Lay out ellipsis
          if (_ellipsisControl != null)
          {
            childSize = desiredEllipsisSize;
            position = new PointF(ActualPosition.X + startPositionX, ActualPosition.Y + startPositionY);

            if (Orientation == Orientation.Vertical)
            {
              childSize.Width = actualWidth;
              ArrangeChildHorizontal(_ellipsisControl, _ellipsisControl.HorizontalAlignment, ref position, ref childSize);
              startPositionY += childSize.Height;
            }
            else
            {
              childSize.Height = actualHeight;
              ArrangeChildVertical(_ellipsisControl, _ellipsisControl.VerticalAlignment, ref position, ref childSize);
              startPositionX += childSize.Width;
            }

            _ellipsisControl.Arrange(SharpDXExtensions.CreateRectangleF(position, childSize));
          }

          int numBeforeEllipsis = childrenAfterEllipsis.Count - numShownChildrenAfterEllipsis;
          for (int i = 1; i < numBeforeEllipsis; i++)
            childrenAfterEllipsis[i].Arrange(RectangleF.Empty);
          childrenAfterEllipsis.RemoveRange(0, numBeforeEllipsis);
        }
        else if (_ellipsisControl != null)
          // childrenAfterEllipsis contains all children in this case
          _ellipsisControl.Arrange(RectangleF.Empty);

        // Lay out all other elements after ellipsis
        foreach (FrameworkElement child in childrenAfterEllipsis)
        {
          SizeF childSize = child.DesiredSize;
          PointF position = new PointF(ActualPosition.X + startPositionX, ActualPosition.Y + startPositionY);

          if (Orientation == Orientation.Vertical)
          {
            childSize.Width = actualWidth;
            ArrangeChildHorizontal(child, child.HorizontalAlignment, ref position, ref childSize);
            startPositionY += childSize.Height;
          }
          else
          {
            childSize.Height = actualHeight;
            ArrangeChildVertical(child, child.VerticalAlignment, ref position, ref childSize);
            startPositionX += childSize.Width;
          }

          child.Arrange(SharpDXExtensions.CreateRectangleF(position, childSize));
        }
      }
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      if (_ellipsisControl != null)
        childrenOut.Add(_ellipsisControl);
    }

    protected override void RenderChildren(RenderContext localRenderContext)
    {
      base.RenderChildren(localRenderContext);
      if (_ellipsisControl != null)
        _ellipsisControl.Render(localRenderContext);
    }

    #endregion
  }
}
