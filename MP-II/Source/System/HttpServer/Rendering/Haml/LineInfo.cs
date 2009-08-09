using System;
using System.Collections.Generic;
using HttpServer.Rendering.Haml.Rules;

namespace HttpServer.Rendering.Haml
{
    /// <summary>
    /// Contains line text and state information about a line in a HAML template.
    /// </summary>
    public class LineInfo
    {
        private bool _appendNextLine;
        private readonly LinkedList<LineInfo> _children = new LinkedList<LineInfo>();
        private string _unparsedData;
        private readonly LinkedList<LineInfo> _extraLines = new LinkedList<LineInfo>();
        private int _intendation;
        private readonly int _lineNumber;
        private LineInfo _parent;
        private string _data;
        private Rule _unfinishedRule;
        private int _whiteSpaces;
        private bool _selfClosed ;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineInfo"/> class.
        /// </summary>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="unparsedData">The unparsed data (line contents).</param>
        public LineInfo(int lineNumber, string unparsedData)
        {
            _lineNumber = lineNumber;
            _unparsedData = unparsedData;
        }

        /// <summary>
        /// If the next line should be appended to us (multi line)
        /// </summary>
        public bool AppendNextLine
        {
            get
            {
                if (_appendNextLine || _unfinishedRule != null)
                    return true;

                foreach (LineInfo line in _extraLines)
                    if (line.UnfinishedRule != null)
                        return true;

                return false;
            }
            set
            {
                if (value == false)
                    throw new ArgumentException("Can only set AppendNextLine to true, false is set internally.");
                _appendNextLine = value;
            }
        }

        /// <summary>
        /// Will check that all rule conditions have been met.
        /// Will also remove the rules if they are done.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool CheckUnfinishedRule(LineInfo line)
        {
            if (_unfinishedRule != null)
            {
                // rule is done, remove it.
                // we can exit extra lines may have rules that are done too.
                if (!_unfinishedRule.IsMultiLine(line, false))
                    _unfinishedRule = null;
                else
                    return true;
            }

            bool res = false;
            foreach (LineInfo subLine in _extraLines)
            {
                if (subLine.CheckUnfinishedRule(line))
                    res = true;
            }

            return res;
        }

        /// <summary>
        /// Do NOT add yourself using the Add methods of the linkedList.
        /// Parent property will add node.
        /// </summary>
        public LinkedList<LineInfo> Children
        {
            get { return _children; }
        }

        /// <summary>
        /// Untouched line text
        /// </summary>
        public string UnparsedData
        {
            get { return _unparsedData; }
        }

        /// <summary>
        /// Has one or more children (intented more that this one)
        /// </summary>
        public bool HasChildren
        {
            get { return _children.Count > 0; }
        }

        /// <summary>
        /// Number of intends (two spaces = 1, one tab = 1)
        /// </summary>
        public int Intendation
        {
            get { return _intendation; }
        }

        /// <summary>
        /// Line number
        /// </summary>
        public int LineNumber
        {
            get { return _lineNumber; }
        }

        /// <summary>
        /// Parent node (one level up in intendation)
        /// </summary>
        public LineInfo Parent
        {
            get { return _parent; }
            set
            {
                _parent = value;
                _parent.Children.AddLast(this);
            }
        }

        /// <summary>
        /// All line data generated on one line
        /// </summary>
        public string Data
        {
            get { return _data; }
        }

        /// <summary>
        /// IRule have not got all needed information yet,
        /// keep appending lines to this LineInfo until rule says that it's done.
        /// </summary>
        public Rule UnfinishedRule
        {
            get { return _unfinishedRule; }
            set { _unfinishedRule = value; }
        }

        /// <summary>
        /// Number of whitespaces before actual entry beings.
        /// </summary>
        public int WhiteSpaces
        {
            get { return _whiteSpaces; }
        }

        /// <summary>
        /// True if node is selfclosed (i.e. &lt;br /&gt;)
        /// </summary>
        public bool SelfClosed
        {
            get { return _selfClosed; }
            set { _selfClosed = value; }
        }

        /// <summary>
        /// Append another line
        /// </summary>
        /// <param name="line"></param>
        public void Append(LineInfo line)
        {
            SetParsedData(_data + line.Data);
            _extraLines.AddLast(line);

            if (CheckUnfinishedRule(this))
                _appendNextLine = true;
            else
                _appendNextLine = false;
        }

        /// <summary>
        /// Parsed line contents (without whitespaces in the beginning)
        /// </summary>
        /// <param name="data">text contents</param>
        protected void SetParsedData(string data)
        {
            _data = data;
            if (_data[_data.Length - 1] == '/')
                _selfClosed = true;
        }

        /// <summary>
        /// Set intendation info to previously added line text.
        /// </summary>
        /// <param name="whiteSpaces"></param>
        /// <param name="intendation"></param>
        public void Set(int whiteSpaces, int intendation)
        {
            _whiteSpaces = whiteSpaces;
            _intendation = intendation;
            SetParsedData(_unparsedData.Substring(whiteSpaces));
        }

        /// <summary>
        /// Assign line text
        /// </summary>
        /// <param name="line"></param>
        /// <param name="whiteSpaces"></param>
        /// <param name="intendation"></param>
        public void Set(string line, int whiteSpaces, int intendation)
        {
            _unparsedData = line;
            Set(whiteSpaces, intendation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <exception cref="InvalidOperationException">If extra lines have been added.</exception>
        public void TrimRight(int count)
        {
            if (_extraLines.Count > 0)
                throw new InvalidOperationException("Have added other lines, cant trim.");
            if (_data.Length < count + 1 || _unparsedData.Length < count + 1)
                throw new InvalidOperationException("To little data left.");

            _unparsedData = _unparsedData.Substring(0, _unparsedData.Length - count);
            SetParsedData(_data.Substring(0, _data.Length - count));
        }
    }
}