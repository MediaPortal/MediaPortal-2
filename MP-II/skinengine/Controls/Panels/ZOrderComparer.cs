using System;
using System.Collections.Generic;
using System.Text;

using SkinEngine.Controls.Visuals;
namespace SkinEngine.Controls.Panels
{
  class ZOrderComparer : IComparer<UIElement>
  {
    #region IComparer<UIElement> Members

    public int Compare(UIElement x, UIElement y)
    {
      return x.ZIndex.CompareTo(y.ZIndex);
    }

    #endregion
  }
}
