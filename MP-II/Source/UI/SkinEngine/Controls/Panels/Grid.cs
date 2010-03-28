#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class Grid : Panel
  {
    #region Consts

    protected const string LEFT_ATTACHED_PROPERTY = "Grid.Left";
    protected const string RIGHT_ATTACHED_PROPERTY = "Grid.Right";
    protected const string TOP_ATTACHED_PROPERTY = "Grid.Top";
    protected const string BOTTOM_ATTACHED_PROPERTY = "Grid.Bottom";
    protected const string ROW_ATTACHED_PROPERTY = "Grid.Row";
    protected const string COLUMN_ATTACHED_PROPERTY = "Grid.Column";
    protected const string ROWSPAN_ATTACHED_PROPERTY = "Grid.RowSpan";
    protected const string COLUMNSPAN_ATTACHED_PROPERTY = "Grid.ColumnSpan";

    #endregion

    #region Protected fields

    protected AbstractProperty _rowDefinitionsProperty;
    protected AbstractProperty _columnDefinitionsProperty;

    #endregion

    #region Ctor

    public Grid()
    {
      Init();
    }

    void Init()
    {
      _rowDefinitionsProperty = new SProperty(typeof(RowDefinitionsCollection), new RowDefinitionsCollection());
      _columnDefinitionsProperty = new SProperty(typeof(ColumnDefinitionsCollection), new ColumnDefinitionsCollection());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Grid g = (Grid) source;
      foreach (RowDefinition row in g.RowDefinitions)
        RowDefinitions.Add(copyManager.GetCopy(row));
      foreach (ColumnDefinition col in g.ColumnDefinitions)
        ColumnDefinitions.Add(copyManager.GetCopy(col));
    }

    #endregion

    #region Properties

    public AbstractProperty RowDefinitionsProperty
    {
      get { return _rowDefinitionsProperty; }
    }

    public RowDefinitionsCollection RowDefinitions
    {
      get { return _rowDefinitionsProperty.GetValue() as RowDefinitionsCollection; }
    }

    public AbstractProperty ColumnDefinitionsProperty
    {
      get { return _columnDefinitionsProperty; }
    }

    public ColumnDefinitionsCollection ColumnDefinitions
    {
      get { return _columnDefinitionsProperty.GetValue() as ColumnDefinitionsCollection; }
    }

    #endregion

    #region Measure & Arrange

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      if (ColumnDefinitions.Count == 0)
        ColumnDefinitions.Add(new ColumnDefinition());
      if (RowDefinitions.Count == 0)
        RowDefinitions.Add(new RowDefinition());

      // Reset values before we start measure the children.
      ColumnDefinitions.ResetAllCellLengths();
      RowDefinitions.ResetAllCellLengths();

      // Set the Width/Hight of the Columns/Rows according to the sizes of the children.
      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) 
          continue;
        int col = GetColumn(child);
        int row = GetRow(child);
        if (col >= ColumnDefinitions.Count) col = ColumnDefinitions.Count - 1;
        if (col < 0) col = 0;
        if (row >= RowDefinitions.Count) row = RowDefinitions.Count - 1;
        if (row < 0) row = 0;

        SizeF childSize = new SizeF(totalSize.Width, totalSize.Height);
        // FIXME Albert: Would be better to use the size which is really available for the child here,
        // but this is not so easy to calculate (depends on col/row definition(s), colspan/rowspan)
        child.Measure(ref childSize);

        ColumnDefinitions.SetDesiredLength(col, GetColumnSpan(child), childSize.Width);
        RowDefinitions.SetDesiredLength(row, GetRowSpan(child), childSize.Height);
      }

      return new SizeF((float) ColumnDefinitions.TotalDesiredLength, (float) RowDefinitions.TotalDesiredLength);
    }

    protected override void ArrangeOverride(RectangleF finalRect)
    {
      base.ArrangeOverride(finalRect);

      ColumnDefinitions.SetAvailableSize(ActualWidth);
      RowDefinitions.SetAvailableSize(ActualHeight);

      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) 
          continue;
        int col = GetColumn(child);
        int row = GetRow(child);
        if (col >= ColumnDefinitions.Count) col = ColumnDefinitions.Count - 1;
        if (col < 0) col = 0;
        if (row >= RowDefinitions.Count) row = RowDefinitions.Count - 1;
        if (row < 0) row = 0;

        PointF position = new PointF(
            (float) ColumnDefinitions.GetOffset(col) + finalRect.Location.X, 
            (float) RowDefinitions.GetOffset(row) + finalRect.Location.Y);

        SizeF childSize = new SizeF(
            (float) ColumnDefinitions.GetLength(col, GetColumnSpan(child)),
            (float) RowDefinitions.GetLength(row, GetRowSpan(child)));

        ArrangeChild(child, ref position, ref childSize);

        child.Arrange(new RectangleF(position, childSize));
      }
    }

    #endregion

    #region Attached properties

    /// <summary>
    /// Getter method for the attached property <c>Row</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>Row</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static int GetRow(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<int>(ROW_ATTACHED_PROPERTY, 0);
    }

    /// <summary>
    /// Setter method for the attached property <c>Row</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>Row</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetRow(DependencyObject targetObject, int value)
    {
      targetObject.SetAttachedPropertyValue<int>(ROW_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>Row</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>Row</c> property.</returns>
    public static AbstractProperty GetRowAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<int>(ROW_ATTACHED_PROPERTY, 0);
    }

    /// <summary>
    /// Getter method for the attached property <c>Column</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>Column</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static int GetColumn(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue(COLUMN_ATTACHED_PROPERTY, 0);
    }

    /// <summary>
    /// Setter method for the attached property <c>Column</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>Column</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetColumn(DependencyObject targetObject, int value)
    {
      targetObject.SetAttachedPropertyValue<int>(COLUMN_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>Column</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>Column</c> property.</returns>
    public static AbstractProperty GetColumnAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<int>(COLUMN_ATTACHED_PROPERTY, 0);
    }

    /// <summary>
    /// Getter method for the attached property <c>RowSpan</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>RowSpan</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static int GetRowSpan(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue(ROWSPAN_ATTACHED_PROPERTY, 1);
    }

    /// <summary>
    /// Setter method for the attached property <c>RowSpan</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>RowSpan</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetRowSpan(DependencyObject targetObject, int value)
    {
      targetObject.SetAttachedPropertyValue<int>(ROWSPAN_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>RowSpan</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>RowSpan</c> property.</returns>
    public static AbstractProperty GetRowSpanAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<int>(ROWSPAN_ATTACHED_PROPERTY, 0);
    }

    /// <summary>
    /// Getter method for the attached property <c>ColumnSpan</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>ColumnSpan</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static int GetColumnSpan(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue(COLUMNSPAN_ATTACHED_PROPERTY, 1);
    }

    /// <summary>
    /// Setter method for the attached property <c>ColumnSpan</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>ColumnSpan</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetColumnSpan(DependencyObject targetObject, int value)
    {
      targetObject.SetAttachedPropertyValue<int>(COLUMNSPAN_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>ColumnSpan</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>ColumnSpan</c> property.</returns>
    public static AbstractProperty GetColumnSpanAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<int>(COLUMNSPAN_ATTACHED_PROPERTY, 0);
    }

    #endregion
  }
}
