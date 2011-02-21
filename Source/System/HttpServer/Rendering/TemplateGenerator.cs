using System;
using System.IO;

namespace HttpServer.Rendering
{
    /// <summary>
    /// A code generator is used to convert template code to something that we can
    /// work with, as HTML or c# code.
    /// </summary>
    /// <seealso cref="TemplateManager"/>
    public interface TemplateGenerator
    {
        /// <summary>
        /// Generate C# code from the template.
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter"/> that the generated code will be written to.</param>
        /// <exception cref="InvalidOperationException">If the template have not been parsed first.</exception>
        /// <exception cref="CodeGeneratorException">If template is incorrect</exception>
        void GenerateCode(TextWriter writer);

        /// <summary>
        /// Parse a file and convert into to our own template object code.
        /// </summary>
        /// <param name="fullPath">Path and filename to a template</param>
        /// <exception cref="CodeGeneratorException">If something is incorrect in the template.</exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="PathTooLongException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        [Obsolete("Use the TextReader overload instead.")]
        void Parse(string fullPath);

        /// <summary>
        /// Parse a file and convert into to our own template object code.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> containing our template</param>
        /// <exception cref="CodeGeneratorException">If something is incorrect in the template.</exception>
        void Parse(TextReader reader);
    }
}