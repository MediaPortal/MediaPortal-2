using System.Text;
using HttpServer.Rendering;
using HttpServer.Rendering.Haml.Nodes;
using HttpServer.Rendering.Haml;

namespace HttpServer.Rendering.Haml.Nodes
{
    /// <summary>
    /// The follow node allows users to display code in a haml document
    /// </summary>
    /// <example>
    /// #main Welcome =user.FirstName
    /// </example>
    public class DisplayCodeNode : ChildNode
    {
        private string _code;

        /// <summary>
        /// Contains C# code that will be rendered into the view.
        /// </summary>
        /// <param name="parent">Parent node</param>
        public DisplayCodeNode(Node parent) : base(parent)
        {}

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
            if (offset >= line.Data.Length)
                throw new CodeGeneratorException(line.LineNumber, line.Data, "Too little data");

            int pos = line.Data.Length;

            ++offset;
            string name = line.Data.Substring(offset, pos - offset);
            offset = pos;

            string trimmedData = line.Data.Trim();
            if (trimmedData.Length > 0 && trimmedData[trimmedData.Length-1] == ';')
                throw new CodeGeneratorException(line.LineNumber, line.Data, "Displayed code should not end with semicolon.");

            DisplayCodeNode node = (DisplayCodeNode)prototypes.CreateNode("=", parent);
            node._code = name;
            if (parent == null)
                node.LineInfo = line;
            return node;
        }

        /// <summary>
        /// determines if this node can handle the line (by checking the first word);
        /// </summary>
        /// <param name="word">Controller char (word)</param>
        /// <returns>true if text belongs to this node type</returns>
        /// <param name="firstNode">first node on line</param>
        public override bool CanHandle(string word, bool firstNode)
        {
            return word.Length > 0 && word[0] == '=';
        }

        /// <summary>
        /// Determines if this is a textnode (containg plain text)
        /// </summary>
        public override bool IsTextNode
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Generate HTML for this node (with asp tags for code)
        /// </summary>
        /// <returns></returns>
        public override string ToHtml()
        {
            if (Parent == null || (Parent.Children.Last.Value != this && LineInfo == null))
                return string.Format("<%= {0} %>", _code);

            StringBuilder sb = new StringBuilder();
            string intend = LineInfo == null ? string.Empty : string.Empty.PadLeft(LineInfo.Intendation, '\t');
            sb.Append(intend);
            sb.Append("<%=");
            sb.Append(_code);

            if (Children.Count == 0)
                sb.AppendLine();

            foreach (Node node in Children)
                sb.Append(node.ToHtml());

            sb.Append(intend);
            sb.AppendLine("%>");

            return sb.ToString();
        }

        /// <summary>
        /// = is placed at the end of a tag definition, after class, id, and attribute declarations. 
        /// It’s just a shortcut for inserting Ruby code into an element. It works the same as = without a tag: 
        /// it inserts the result of the Ruby code into the template. 
        /// However, if the result is short enough, it is displayed entirely on one line.
        /// </summary>
        /// <param name="inString">True if we are inside the internal stringbuilder</param>
        /// <param name="smallEnough">true if all subnodes fit on one line</param>
        /// <param name="smallEnoughIsDefaultValue">smallEnough is a default value, recalc it</param>
        /// <returns>c# code</returns>
        protected override string ToCode(ref bool inString, bool smallEnough, bool smallEnoughIsDefaultValue)
        {
            if (LineInfo == null)
            {
                if (inString)
                    return string.Format("\");sb.Append({0});sb.Append(@\"", _code);
                else
                    return "sb.Append(" + _code + ");";
            }

            StringBuilder sb = new StringBuilder();
            string intend = LineInfo == null ? string.Empty : string.Empty.PadLeft(LineInfo.Intendation, '\t');
            if (inString)
            {
                sb.Append("\");");
                inString = false;
            }

            if (smallEnough)
                sb.Append("sb.Append(");
            else
            {
                sb.Append("sb.AppendLine(");
                sb.Append(intend);
            }

            // on same line
            sb.Append(_code);
            sb.Append(");");

            foreach (Node node in Children)
                sb.Append(node.ToCode(ref inString, smallEnough));

            if (!smallEnough)
                sb.AppendLine();

            return sb.ToString();
        }

        bool IsLastNode(Node parent)
        {
            if (parent == null)
                return false;
            if (parent.ModifierCount == 0)
                return false;
            return parent.LastModifier == this;
        }

    }
}