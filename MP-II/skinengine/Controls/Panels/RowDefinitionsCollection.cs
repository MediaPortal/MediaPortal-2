using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyXaml.Core;
namespace SkinEngine.Controls.Panels
{
  public class RowDefinitionsCollection : List<RowDefinition>, IAddChild
  {

    #region IAddChild Members

    public void AddChild(object o)
    {
      Add((RowDefinition)o);
    }

    #endregion
  }
}
