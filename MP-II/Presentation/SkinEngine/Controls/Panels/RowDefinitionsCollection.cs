using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyXaml.Core;
namespace Presentation.SkinEngine.Controls.Panels
{
  public class RowDefinitionsCollection : List<RowDefinition>, IAddChild
  {

    #region IAddChild Members

    public void AddChild(object o)
    {
      Add((RowDefinition)o);
    }

    #endregion


    public float GetHeight(int row, int rowSpan)
    {
      float height = 0.0f;
      for (int i = 0; i < rowSpan; ++i)
      {
        height += (float)this[row + i].Height.Length;
      }
      return height;
    }

    public float GetOffset(int row)
    {
      float height = 0.0f;
      for (int i = 0; i < row; ++i)
      {
        height += (float)this[i].Height.Length;
      }
      return height;
    }

    public double TotalHeight
    {
      get
      {
        double height = 0.0;
        foreach (RowDefinition rowDef in this)
        {
          height += rowDef.Height.Length;
        }
        return height;
      }
    }

    public void SetHeight(int row, int rowSpan, float height)
    {
      int relativeCount = 0;
      for (int i = 0; i < rowSpan; ++i)
      {
        RowDefinition rowDef = this[i + row];
        if (rowDef.Height.IsAbsolute)
        {
          height -= (float)rowDef.Height.Length;
        }
        else relativeCount++;
      }
      for (int i = 0; i < rowSpan; ++i)
      {
        RowDefinition rowDef = this[i + row];
        if (rowDef.Height.IsAuto || rowDef.Height.IsStar)
        {
          if (rowDef.Height.Length == 0.0)
          {
            rowDef.Height.Length = height / relativeCount;
          }
        }
      }
    }


    public void SetAvailableSize(double height)
    {
      double fixedHeight = 0.0f;
      int relativeCount = 0;
      double totalStar = 0;
      foreach (RowDefinition row in this)
      {
        if (row.Height.IsAbsolute)
        {
          row.Height.Length = (row.Height.Value * SkinContext.Zoom.Height);
          fixedHeight += row.Height.Length;
        }
        else
        {
          row.Height.Length = 0;
          if (row.Height.IsAuto)
            totalStar += 1.0;
          else
            totalStar += row.Height.Value;
          relativeCount++;
        }
      }
      if (height == 0.0) return;
      height -= fixedHeight;
      foreach (RowDefinition row in this)
      {
        if (row.Height.IsStar)
        {
          row.Height.Length = height * (row.Height.Value / totalStar);
        }
        else if (row.Height.IsAuto)
        {
          row.Height.Length = height * (1.0 / totalStar);
        }
      }

    }
  }
}
