using System;
using System.Collections.Generic;
using System.IO;
using HttpServer.Rendering.Haml.Nodes;
using HttpServer.Rendering.Haml.Rules;

namespace HttpServer.Rendering.Haml
{
    /// <summary>
    /// Generates C#/HTML from HAML code.
    /// </summary>
    /// <remarks>HAML documentation: http://haml.hamptoncatlin.com/docs/rdoc/classes/Haml.html</remarks>
    public class HamlGenerator : ITemplateGenerator
    {
        private LineInfo _currentLine;
        private int _lineNo = -1;
        private LineInfo _mother;
        private Node _parentNode;
        private LineInfo _prevLine;
        private TextReader _reader;
        private readonly List<Rule> _rules = new List<Rule>();
        private readonly ILogWriter _log;


        /// <summary>
        /// Initializes a new instance of the <see cref="HamlGenerator"/> class.
        /// </summary>
        public HamlGenerator()
        {
            _rules.Add(new NewLineRule());
            _rules.Add(new AttributesRule());
            _log = NullLogWriter.Instance;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="HamlGenerator"/> class.
        /// </summary>
        /// <param name="logWriter">The log writer.</param>
        public HamlGenerator(ILogWriter logWriter)
        {
            _rules.Add(new NewLineRule());
            _rules.Add(new AttributesRule());
            _log = logWriter ?? NullLogWriter.Instance;
        }

		/// <summary>
		/// Property to retrieve the root node for the latest parsed document
		/// </summary>
		public Node RootNode
		{
			get { return _parentNode; }
		}

        /// <summary>
        /// Check and validate indentation
        /// </summary>
        /// <param name="line">line to check</param>
        /// <param name="ws">number of white spaces</param>
        /// <param name="intendation">number of indentations (2 white spaces = 1 intend, 1 tab = 1 intend)</param>
        protected static void CheckIntendation(LineInfo line, out int ws, out int intendation)
        {
            intendation = 0;
            ws = -1;

            char prevUnusedCh = line.UnparsedData[0];
            if (prevUnusedCh == '\t')
            {
                ++intendation;
                prevUnusedCh = char.MinValue;
            }
            else if (prevUnusedCh != ' ')
            {
                ws = 0;
                return;
            }

            for (int i = 1; i < line.UnparsedData.Length; ++i)
            {
                char ch = line.UnparsedData[i];

                if (ch == ' ')
                {
                    if (prevUnusedCh == '\t')
                    {
                        ++intendation;
                        prevUnusedCh = ' ';
                        continue;
                    }
                    if (prevUnusedCh == ' ')
                    {
                        prevUnusedCh = char.MinValue;
                        ++intendation;
                        continue;
                    }
                    
                    prevUnusedCh = ' ';
                }
                else if (ch == '\t')
                {
                    if (prevUnusedCh == ' ')
                        throw new CodeGeneratorException(line.LineNumber, line.Data,
                                                         "Invalid intendation sequence: One space + one tab. Should either be one tab or two spaces.");
                    if (prevUnusedCh == char.MinValue)
                    {
                        ++intendation;
                        prevUnusedCh = char.MinValue;
                        continue;
                    }
                }
                else
                {
                    if (prevUnusedCh != char.MinValue)
                        throw new CodeGeneratorException(line.LineNumber, line.Data,
                                                         "Invalid intendation at char " + i + ", expected a space.");

                    if (i == 1 && !char.IsWhiteSpace(line.UnparsedData[0]))
                        ws = 0;
                    else
                        ws = i;
                    return;
                }
            }
        }

        /// <summary>
        /// Check indentation
        /// </summary>
        /// <param name="line">fills line with intend info</param>
        protected static void CheckIntendation(LineInfo line)
        {
            int ws, intendation;
            CheckIntendation(line, out ws, out intendation);
            if (ws == -1)
                throw new CodeGeneratorException(line.LineNumber, line.Data,
                                                 "Failed to find indentation on line #" + line.LineNumber);

            line.Set(ws, intendation);
        }

        /// <summary>
        /// check if current line is a multi line
        /// </summary>
        /// <param name="prevLine">previous line</param>
        /// <param name="line">current line</param>
        protected void CheckMultiLine(LineInfo prevLine, LineInfo line)
        {
            if (prevLine != null && prevLine.UnfinishedRule != null)
            {
                if (prevLine.UnfinishedRule.IsMultiLine(line, true))
                {
                    _log.Write(this, LogPrio.Trace, line.LineNumber + ": " + prevLine.UnfinishedRule.GetType().Name +
                                      " says that the next line should be appended.");
                    line.AppendNextLine = true;
                    return;
                }
            }

            foreach (Rule rule in _rules)
            {
                if (rule.IsMultiLine(line, false))
                {
                    _log.Write(this, LogPrio.Trace, line.LineNumber + ": " + rule.GetType().Name +
                                      " says that the next line should be appended.");
                    line.AppendNextLine = true;
                    continue;
                }
            }
        }

        /// <summary>
        /// Generate HTML code from the template.
        /// Code is encapsulated in &lt;% and &lt;%=
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter"/> that the generated code will be written to.</param>
        /// <exception cref="InvalidOperationException">If the template have not been parsed first.</exception>
        /// <exception cref="CodeGeneratorException">If template is incorrect</exception>
        public void GenerateHtml(TextWriter writer)
        {
            foreach (Node child in _parentNode.Children)
                writer.Write(child.ToHtml());
        }

        /// <summary>
        /// Get the first word (letters and digits only) from the specified offset.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static string GetWord(string data, int offset)
        {
            for (int i = offset; i < data.Length; ++i)
            {
                if (!char.IsLetterOrDigit(data[i]) && data[i] != '!')
                    return data.Substring(offset, i - offset + 1);
            }

            return data;
        }

        /// <summary>
        /// Check indentation / node placement
        /// </summary>
        protected void HandlePlacement()
        {
            // Check intendation so that we know where to place the line

            // Larger intendation = child
            if (_currentLine.Intendation > _prevLine.Intendation)
            {
                if (_currentLine.Intendation != _prevLine.Intendation + 1)
                    throw new CodeGeneratorException(_currentLine.LineNumber,
                                                     "Too large indentation, " + (_currentLine.Intendation -
                                                                                  _prevLine.Intendation) +
                                                     " steps instead of 1.");

                _currentLine.Parent = _prevLine;
            }
                // same intendation = same parent.
            else if (_currentLine.Intendation == _prevLine.Intendation)
                _currentLine.Parent = _prevLine.Parent;

                // Node should be placed on a node up the chain.
            else
            {
                // go back until we find someone at the same level
                LineInfo sameLevelNode = _prevLine;
                while (sameLevelNode != null && sameLevelNode.Intendation > _currentLine.Intendation)
                    sameLevelNode = sameLevelNode.Parent;

                if (sameLevelNode == null)
                {
                    if (_currentLine.Intendation > 0)
                        throw new CodeGeneratorException(_currentLine.LineNumber, "Failed to find parent.");

                    _currentLine.Parent = _mother;
                }
                else
                    _currentLine.Parent = sameLevelNode.Parent;
            }
        }


        /// <summary>
        /// Parse a node
        /// todo: improve doc
        /// </summary>
        /// <param name="theLine"></param>
        /// <param name="prototypes"></param>
        /// <param name="parent"></param>
        /// <param name="textNode"></param>
        protected static void ParseNode(LineInfo theLine, NodeList prototypes, Node parent, TextNode textNode)
        {
            Node curNode = null;
            int offset = 0;

            // parse each part of a line
            while (offset <= theLine.Data.Length - 1)
            {
                Node node = prototypes.GetPrototype(GetWord(theLine.Data, offset), curNode == null) ?? textNode;
                node = node.Parse(prototypes, curNode, theLine, ref offset);

                // first node on line, set it as current
                if (curNode == null)
                {
                    curNode = node;
                    curNode.LineInfo = theLine;
                    parent.Children.AddLast(node);
                }
                else
                    curNode.AddModifier(node); // append attributes etc.
            }

            foreach (LineInfo child in theLine.Children)
                ParseNode(child, prototypes, curNode, textNode);
        }

        /// <summary>
        /// PreParse goes through the text add handles indentation
        /// and all multi line cases.
        /// </summary>
        /// <param name="reader">Reader containing the text</param>
        protected void PreParse(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            // Read first line to be able to assign it to the mother.
            if (!ReadLine())
                throw new CodeGeneratorException(1, "No data.");

            if (_currentLine.Intendation != 0)
                throw new CodeGeneratorException(1, "Invalid indentation, should be 0.");
            _currentLine.Parent = _mother;
            CheckIntendation(_currentLine);
            CheckMultiLine(_prevLine, _currentLine);

            while (ReadLine())
            {
                if (_currentLine.UnparsedData.Length == 0)
                    continue;

                CheckIntendation(_currentLine);
                CheckMultiLine(_prevLine, _currentLine);
                if (_prevLine.AppendNextLine)
                {
                    _prevLine.Append(_currentLine);
					if(_currentLine.AppendNextLine)
						_prevLine.AppendNextLine = true;
                    _currentLine = _prevLine;
                    continue;
                }

                HandlePlacement();
            }
        }

        /// <summary>
        /// print the entire document
        /// </summary>
        public void PrintDocument()
        {
            PrintNode(_mother);
        }

        /// <summary>
        /// Print line information to the console
        /// </summary>
        /// <param name="line"></param>
        public void PrintNode(LineInfo line)
        {
            _log.Write(this, LogPrio.Debug, Spaces(line.Intendation) + line.Data);
            foreach (LineInfo info in line.Children)
                PrintNode(info);
        }

        /// <summary>
        /// Read next line from file
        /// </summary>
        /// <returns>true if line could be read; false if EOF.</returns>
        protected bool ReadLine()
        {
            string line = _reader.ReadLine();
            string trimmedLine = (line != null) ? line.Trim(new char[] {' ', '\t'}) : string.Empty;

            while (line != null && (trimmedLine == string.Empty
                                    || (trimmedLine.Length > 0 && trimmedLine[0] == '-' && trimmedLine[1] == '/')
                                   ))
            {
                ++_lineNo;
                line = _reader.ReadLine();
                trimmedLine = (line != null) ? line.Trim(new char[] {' ', '\t'}) : string.Empty;
            }
            if (line == null)
                return false;

            ++_lineNo;
            _prevLine = _currentLine;
            _currentLine = new LineInfo(_lineNo, line);
            return true;
        }

        /// <summary>
        /// Generates a string with spaces.
        /// </summary>
        /// <param name="count">number of spaces.</param>
        /// <returns>string of spaces.</returns>
        public string Spaces(int count)
        {
            return "".PadLeft(count);
        }

        #region ITemplateGenerator Members

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
        /// <exception cref="ArgumentException"></exception>
        public void Parse(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                throw new ArgumentException("Path must be specified.", "fullPath");

            Stream stream = null;
            try
            {
                stream = File.OpenRead(fullPath);
                TextReader reader = new StreamReader(stream);
                Parse(reader);
                reader.Close();
                reader.Dispose();
                stream.Dispose();
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
        }

        /// <summary>
        /// Parse a file and convert into to our own template object code.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> containing our template</param>
        /// <exception cref="CodeGeneratorException">If something is incorrect in the template.</exception>
        public void Parse(TextReader reader)
        {
            _lineNo = -1;
            _reader = reader;
            _mother = new LineInfo(-1, string.Empty);
            _prevLine = null;
            _currentLine = null;

            PreParse(reader);

            NodeList prototypes = new NodeList();
            prototypes.Add(new AttributeNode(null));
            prototypes.Add(new TagNode(null));
            prototypes.Add(new IdNode(null));
            prototypes.Add(new SilentCodeNode(null));
            prototypes.Add(new ClassNode(null));
            prototypes.Add(new DisplayCodeNode(null));
            prototypes.Add(new DocTypeTag(null, null));
			prototypes.Add(new PartialNode(null));
            TextNode textNode = new TextNode(null, "prototype");
            _parentNode = new TextNode(null, string.Empty);

            foreach (LineInfo info in _mother.Children)
                ParseNode(info, prototypes, _parentNode, textNode);
        }

        /// <summary>
        /// Generate C# code from the template.
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter"/> that the generated code will be written to.</param>
        /// <exception cref="InvalidOperationException">If the template have not been parsed first.</exception>
        /// <exception cref="CodeGeneratorException">If template is incorrect</exception>
        public void GenerateCode(TextWriter writer)
        {
            writer.Write("sb.Append(@\"");
            bool inString = true;
            foreach (Node child in _parentNode.Children)
                writer.Write(child.ToCode(ref inString));
            if (inString)
                writer.Write("\");");
        }

        #endregion
    }
}