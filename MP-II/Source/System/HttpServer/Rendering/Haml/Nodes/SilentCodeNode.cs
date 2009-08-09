using System.Text;
using HttpServer.Rendering;
using HttpServer.Rendering.Haml.Nodes;
using HttpServer.Rendering.Haml;

namespace HttpServer.Rendering.Haml.Nodes
{
    /// <summary>
    /// The ’-’ character makes the text following it into “silent” code: C# code that is evaluated, but not output.
    /// It is not recommended that you use this widely; almost all processing code and logic should be restricted to the Controller, Helpers, or partials.
    /// 
    /// For example
    /// <code>
    /// - string foo = "hello" 
    /// - foo += " there" 
    /// - foo += " you!" 
    /// %p= foo
    /// </code>
    /// 
    /// Is compiled to
    /// <example>
    /// <p>
    ///  hello there you!
    ///</p>
    /// </example>
    /// </summary>
    internal class SilentCodeNode : ChildNode
    {
        private string _code = string.Empty;

        public SilentCodeNode(Node parent) : base(parent)
        {}

        /// <summary>
        /// determines if this node can handle the line (by checking the first word);
        /// </summary>
        /// <param name="word">Controller char (word)</param>
        /// <returns>true if text belongs to this node type</returns>
        /// <param name="firstNode">first node on line</param>
        public override bool CanHandle(string word, bool firstNode)
        {
            return word.Length > 0 && word[0] == '-';
        }

        /// <summary>
        /// Parse node contents add return a fresh node.
        /// </summary>
        /// <param name="prototypes">List containing all node types</param>
        /// <param name="parent">Node that this is a subnode to. Can be null</param>
        /// <param name="line">Line to parse</param>
        /// <param name="offset">Where to start the parsing. Should be set to where the next node should start parsing.</param>
        /// <returns>A node corresponding to the bla bla; null if parsing failed.</returns>
        /// <exception cref="CodeGeneratorException"></exception>
        public override Node Parse(NodeList prototypes, Node parent, LineInfo line, ref int offset)
        {
            if (offset > line.Data.Length)
				throw new CodeGeneratorException(line.LineNumber, line.Data, "Too little data");

            int pos = line.Data.Length;
            ++offset;
            string code = line.Data.Substring(offset, pos - offset);
            offset = pos;

            SilentCodeNode node = (SilentCodeNode) prototypes.CreateNode("-", parent);
            node._code = code;
            if (parent != null)
                node.LineInfo = line;
            return node;
        }

        /// <summary>
        /// Convert the node to c# code
        /// </summary>
        /// <param name="inString">True if we are inside the internal stringbuilder</param>
        /// <param name="smallEnough">true if all subnodes fit on one line</param>
        /// <param name="smallEnoughIsDefaultValue">smallEnough is a default value, recalc it</param>
        /// <returns>c# code</returns>
        protected override string ToCode(ref bool inString, bool smallEnough, bool smallEnoughIsDefaultValue)
        {

            //if (LineInfo == null)
            if ((Parent != null && Parent.Children.Last != null && Parent.Children.Last.Value != this) && LineInfo == null)
            {
                if (inString)
                    return string.Format("\"); {0} sb.Append(@\"", _code);
                else
                    return _code;
            }

            StringBuilder sb = new StringBuilder();
            string intend = LineInfo == null ? string.Empty : string.Empty.PadLeft(LineInfo.Intendation, '\t');
            if (inString)
            {
                sb.Append("\");");
                inString = false;
            }

            sb.Append(intend);
            sb.Append(_code);

            if (AllChildrenCount > 0)
            {
                sb.Append("{");
                if (Modifiers.Count != 0)
					throw new CodeGeneratorException(LineInfo.LineNumber, LineInfo.Data, "Code tags should not have any modifiers.");

                foreach (Node node in Children)
                    sb.Append(node.ToCode(ref inString));

                if (inString)
                {
                    sb.Append("\");");
                    inString = false;
                }

                sb.Append("}" + intend);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert node to HTML (with ASP-tags)
        /// </summary>
        /// <returns>HTML string</returns>
        public override string ToHtml()
        {
            if (LineInfo == null)
                return string.Format("<% {0} %>", _code);

            StringBuilder sb = new StringBuilder();
            string intend = LineInfo == null ? string.Empty : string.Empty.PadLeft(LineInfo.Intendation, '\t');
            sb.Append(intend);
            sb.Append("<%");
            sb.Append(_code);

            if (Children.Count != 0)
                sb.AppendLine();

            if (Modifiers.Count != 0)
				throw new CodeGeneratorException(LineInfo.LineNumber, LineInfo.Data, "Code tags should not have any modifiers.");

            foreach (Node node in Children)
                sb.Append(node.ToHtml());

            sb.Append(intend);
            sb.AppendLine("%>");

            return sb.ToString();
        }
    }
}