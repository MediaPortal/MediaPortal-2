using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using MyXaml.Core;
namespace SkinEngine.Skin
{
  public class Include : IInclude
  {
    object _content;
    string _includeName;

    #region Public properties    
    public string Source
    {
      get
      {
        return _includeName;
      }
      set
      {
        _includeName = value;
        XamlLoader loader = new XamlLoader();
        _content = loader.Load(_includeName);
      }
    }
    #endregion

    #region IInclude implementation
    public object Content
    {
      get
      {
        return _content;
      }
    }               
    #endregion
  }
}
