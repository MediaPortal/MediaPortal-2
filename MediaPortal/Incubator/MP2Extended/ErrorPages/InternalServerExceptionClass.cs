using System;
using System.Text.RegularExpressions;
using HttpServer.Exceptions;

namespace MediaPortal.Plugins.MP2Extended.ErrorPages
{
  partial class InternalServerExceptionTemplate
  {
    private readonly Exception _ex;
    private readonly int _errorCode = 500;
    private readonly string _errorName = "Internal Server Error";

    public InternalServerExceptionTemplate(Exception exception)
    {
      _ex = exception;
      HttpException httpEx = exception as HttpException;
      if (httpEx != null)
      {
        _errorCode = (int)httpEx.HttpStatusCode;
        _errorName = Regex.Replace(httpEx.HttpStatusCode.ToString(), "([a-z])([A-Z])", "$1 $2"); // Add a space before capital letters
      }
    }
  }
}
