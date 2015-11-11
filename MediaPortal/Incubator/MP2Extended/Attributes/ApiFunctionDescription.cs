using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.Attributes
{
  class ApiFunctionDescription : Attribute
  {
    public enum FunctionType
    {
      Json,
      Stream,
      Html
    }

    public string Summary;
    public Type ReturnType { set; get; }
    public FunctionType Type;
  }
  [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
  public class ApiFunctionParam : Attribute
  {
    public string Name { set; get; }
    public Type Type { set; get; }
    public bool Nullable { set; get; }
  }
}
