using System;

namespace HttpServer
{
  /// <summary>
  /// We dont want to let the server to die due to exceptions thrown in worker threads.
  /// therefore we use this delegate to give you a change to handle uncaught exceptions.
  /// </summary>
  /// <param name="source">Class that the exception was thrown in.</param>
  /// <param name="exception">Exception</param>
  /// <remarks>
  /// Server will throw a InternalServerException in release version if you dont
  /// handle this delegate.
  /// </remarks>
  public delegate void ExceptionHandler(object source, Exception exception);
}