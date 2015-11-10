using System;

namespace MediaPortal.Plugins.MP2Extended.ErrorPages
{
  partial class InternalServerExceptionTemplate
  {
    private Exception ex;
    public InternalServerExceptionTemplate(Exception exception) { this.ex = exception; }
  }
}
