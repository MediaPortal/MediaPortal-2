using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HttpServer.Exceptions;

namespace MediaPortal.Plugins.MP2Extended.ErrorPages
{
  partial class InternalServerExceptionTemplate
  {
    private Exception ex;
    private int errorCode = 500;
    private string errorName = "Internal Server Error";

    public InternalServerExceptionTemplate(Exception exception)
    {
      ex = exception;
      HttpException httpEx = exception as HttpException;
      if (httpEx != null)
      {
        errorCode = (int)httpEx.HttpStatusCode;
        errorName = Regex.Replace(httpEx.HttpStatusCode.ToString(), "([a-z])([A-Z])", "$1 $2");
      }
    }
  }
}
