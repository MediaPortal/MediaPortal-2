using System;

namespace HttpServer
{
    /// <summary>
    /// An unhandled exception have been caught by the system.
    /// </summary>
    public class ExceptionEventArgs : EventArgs
    {
        private readonly Exception _exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="exception">Caught exception.</param>
        public ExceptionEventArgs(Exception exception)
        {
            _exception = exception;
        }

        /// <summary>
        /// caught exception
        /// </summary>
        public Exception Exception
        {
            get { return _exception; }
        }
    }
}
