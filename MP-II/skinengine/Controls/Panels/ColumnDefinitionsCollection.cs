using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyXaml.Core;
namespace SkinEngine.Controls.Panels
{
  public class ColumnDefinitionsCollection : List<ColumnDefinition>, IAddChild
  {

    #region IAddChild Members

    public void AddChild(object o)
    {
      Add((ColumnDefinition)o);
    }

    #endregion
  }
}
