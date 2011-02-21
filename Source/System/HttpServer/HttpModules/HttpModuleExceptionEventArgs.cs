using System;

namespace HttpServer.HttpModules
{
  /// <summary>
  /// Used to inform http server that 
  /// </summary>
  public class HttpModuleExceptionEventArgs : EventArgs
  {
    private readonly Exception _exception;

    /// <summary>
    /// Eventarguments used when an exception is thrown by a module
    /// </summary>
    /// <param name="e">the exception</param>
    public HttpModuleExceptionEventArgs(Exception e)
    {
      _exception = e;
    }

    /// <summary>
    /// Exception thrown in a module
    /// </summary>
    public Exception Exception
    {
      get { return _exception; }
    }
  }
}