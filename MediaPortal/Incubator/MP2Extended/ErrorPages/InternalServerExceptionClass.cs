using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.ErrorPages
{
  partial class InternalServerExceptionTemplate
  {
    private Exception ex;
    public InternalServerExceptionTemplate(Exception exception) { this.ex = exception; }
  }
}
