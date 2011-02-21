using System;
using System.CodeDom.Compiler;
using Fadd;

namespace HttpServer.Rendering
{
    /// <summary>
    /// Thrown when a template cannot be compiled.
    /// </summary>
    public class TemplateException : Exception
    {
        private readonly CompilerException _err;
        private readonly string _templateName;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateException"/> class.
        /// </summary>
        /// <param name="templateName">Template that failed compilation.</param>
        /// <param name="err">Exception thrown by the compiler.</param>
        public TemplateException(string templateName, CompilerException err) : base(string.Empty, err)
        {
            _err = err;
            _templateName = templateName;
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The error message that explains the reason for the exception, or an empty string("").
        /// </returns>
        public override string Message
        {
            get
            {
                string msg = "Failed to compile '" + _templateName + "'\r\n";
                foreach (CompilerError error in _err.Errors)
                {
                    if (error.IsWarning)
                        continue;
                    msg += "Line: " + error.Line + " " + error.ErrorText;
                }

                return msg;
            }
        }
        /// <summary>
        /// Creates and returns a string representation of the current exception.
        /// </summary>
        /// <returns>
        /// A string representation of the current exception.
        /// </returns>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" PathDiscovery="*AllFiles*"/>
        /// </PermissionSet>
        public override string ToString()
        {
#if DEBUG
            return Message + "\r\n" + _err.Data["code"];
#else
            return Message + "\r\n";
#endif
        }
    }
}
