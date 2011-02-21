using System;
using System.IO;
using System.Text;

namespace HttpServer.Rendering.Tiny
{
    /// <summary>
    /// Generates C# rendering object using ASP similiar tags in the HTML code.
    /// </summary>
    public class TinyGenerator : ITemplateGenerator
    {
        readonly StringBuilder _sb = new StringBuilder();

        #region ITemplateGenerator Members

        /// <summary>
        /// Generate C# code from the template.
        /// </summary>
        /// <param name="writer">A textwriter that the generated code will be written to.</param>
        /// <exception cref="InvalidOperationException">If the template have not been parsed first.</exception>
        /// <exception cref="CodeGeneratorException">If template is incorrect</exception>
        public void GenerateCode(TextWriter writer)
        {
            writer.Write(_sb.ToString());
        }

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
        public void Parse(string fullPath)
        {
            FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            TextReader reader = new StreamReader(fs);
            Parse(reader);
        }

        /// <summary>
        /// Parse a file and convert into to our own template object code.
        /// </summary>
        /// <param name="reader">A textreader containing our template</param>
        /// <exception cref="CodeGeneratorException">If something is incorrect in the template.</exception>
        public void Parse(TextReader reader)
        {
            bool inCode = false;
            bool isOutput = false;

            _sb.Length = 0;
            StringBuilder sb = _sb;
            sb.Append("sb.Append(@\"");

            //bool quoteStarted = false;
            //bool inQuote = false;
            string line = reader.ReadLine();
            while(line != null)
            {
                for (int i = 0; i < line.Length; ++i)
                {
                    char ch = line[i];
                    char nextCh = i < line.Length - 1 ? line[i + 1] : char.MinValue;
                    /*
                    if (isOutput && inCode && !quoteStarted && !char.IsWhiteSpace(ch))
                    {
                        sb.Append("\"");
                        quoteStarted = true;
                    }
                    else if (quoteStarted)
                    {
                        if (ch == '\"')
                        {
                            sb.Append("\\");
                            inQuote = !inQuote;
                        }

                        // We are in a <%= %> tag, we need to scan after end
                        // to be able to insert " directly after the statement (and before whitespaces)
                        if (!inQuote)
                        {
                            for (int j = i; j < line.Length; ++j)
                            {
                                if (line[j] == '%')
                                {
                                    if (j < line.Length && line[j+1] == '>')
                                    {
                                        sb.Append("\"");
                                        ch = line[j];
                                        nextCh = line[j + 1];
                                        i = j;
                                        break;
                                    }
                                }
                                if (!char.IsWhiteSpace(line[j]))
                                    break;
                            }
                        }
                    }*/

                    if (ch == '"')
                    {
                        sb.Append(ch);
                        if (!inCode)
                            sb.Append(ch);
                    }
                    else if (ch == '<' && nextCh == '%')
                    {
                        char thirdCh = i < line.Length - 2 ? line[i + 2] : char.MinValue;
                        ++i;

                        sb.Append("\");");
                        if (thirdCh == '=')
                        {
                            ++i;
                            isOutput = true;
                            sb.Append("sb.Append(");
                        }

                        inCode = true;
                    }
                    else if (ch == '%' && nextCh == '>')
                    {
                        ++i;
                        if (isOutput)
                            sb.Append(");");

                        sb.Append("sb.Append(@\"");
                        inCode = false;
                        isOutput = false;
                    }
                    else 
                        sb.Append(ch);
                }

                sb.AppendLine();
                line = reader.ReadLine();
            } //while

            // to avoid compile errors.
            if (inCode && isOutput)
                    sb.Append(");");
            else
                sb.Append("\");");
        }

        #endregion
    }
}
