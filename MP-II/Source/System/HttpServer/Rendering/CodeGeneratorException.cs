using System;

namespace HttpServer.Rendering
{
    /// <summary>
    /// Contains information on where in the template the error occurred, and what the error was.
    /// </summary>
    public class CodeGeneratorException : Exception
    {
        private readonly int _lineNumber;
    	private readonly string _line = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGeneratorException"/> class.
        /// </summary>
        /// <param name="lineNumber">Line that the error appeared on.</param>
        /// <param name="error">error description.</param>
        public CodeGeneratorException(int lineNumber, string error) : base(error)
        {
            _lineNumber = lineNumber;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGeneratorException"/> class.
        /// </summary>
        /// <param name="lineNumber">Line that the error appeared on.</param>
        /// <param name="error">error description.</param>
        /// <param name="line">line contents.</param>
		public CodeGeneratorException(int lineNumber, string line, string error) : base(error + "\nLine: " + line)
		{
			_lineNumber = lineNumber;
			_line = line;
		}

		/// <summary>
		/// Returns the actual line where the error originated
		/// </summary>
    	public string Line
    	{
			get { return _line; }
    	}

        /// <summary>
        /// Line number in template
        /// </summary>
        public int LineNumber
        {
            get { return _lineNumber; }
        }
    }
}