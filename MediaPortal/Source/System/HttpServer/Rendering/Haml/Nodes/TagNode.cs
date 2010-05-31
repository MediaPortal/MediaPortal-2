using System.Text;
using HttpServer.Rendering;
using HttpServer.Rendering.Haml;
using HttpServer.Rendering.Haml.Nodes;

namespace HttpServer.Rendering.Haml.Nodes
{
    /// <summary>
    /// Represents a HTML tag.
    /// </summary>
    public class TagNode : Node
    {
        private string _name;

        /// <summary>
        /// Create a new HTML tag node.
        /// </summary>
        /// <param name="parent">parent node</param>
        public TagNode(Node parent) : base(parent)
        {
        }

        /// <summary>
        /// This is a plain text node
        /// </summary>
        public override bool IsTextNode
        {
            get { return false; }
        }

        /// <summary>
        /// tag name
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { 
                _name = value; 
            }
        }

        /// <summary>
        /// determines if this node can handle the line (by checking the first word);
        /// </summary>
        /// <param name="word">Controller char (word)</param>
        /// <returns>true if text belongs to this node type</returns>
        /// <param name="firstNode">first node on line.</param>
        public override bool CanHandle(string word, bool firstNode)
        {
            if (word.Length >= 1 && word[0] == '%')
                return true;

            return false;
        }

        /// <summary>
        /// Parse node contents add return a fresh node.
        /// </summary>
        /// <param name="parent">Node that this is a subnode to. Can be null</param>
        /// <param name="prototypes">A list with node types</param>
        /// <param name="line">Line to parse</param>
        /// <param name="offset">Where to start the parsing. Will be set to where the next node should start parsing</param>
        /// <returns>A node corresponding to the bla bla; null if parsing failed.</returns>
        /// <exception cref="CodeGeneratorException"></exception>
        public override Node Parse(NodeList prototypes, Node parent, LineInfo line, ref int offset)
        {
            if (offset > line.Data.Length - 1)
				throw new CodeGeneratorException(line.LineNumber, line.Data, "Tried to parse after end of line");

            if (line.Data[offset] != '%')
				throw new CodeGeneratorException(line.LineNumber, line.Data, "Not a tag node");

            int pos = -1;
            for (int i = offset + 1; i < line.Data.Length; ++i)
            {
                if (!char.IsLetterOrDigit(line.Data[i]))
                {
                    pos = i;
                    break;
                }
            }
            if (pos == -1)
                pos = line.Data.Length;

            TagNode node = (TagNode) prototypes.CreateNode("%", parent);
            node.Name = line.Data.Substring(offset + 1, pos - offset - 1);
			if (node._name == "br" || node._name == "input" || node._name == "style" || node._name == "img")
            {
                line.SelfClosed = true;
            	node.LineInfo = line;
                node.LineInfo.SelfClosed = true;
            }

            offset = pos;
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
            StringBuilder sb = new StringBuilder();
            string intend = LineInfo == null ? string.Empty : string.Empty.PadLeft(LineInfo.Intendation, '\t');

            bool tempStr = inString;
            bool childSmallEnough = Children.Count == 0 ||
                (Children.Count == 1 && Children.First.Value.ToCode(ref tempStr).Length < 60 && AllChildrenCount <= 2);
            /*smallEnough = AllChildrenCount < 1 ||
                          (Children.Count <= 2 &&
                           Children.First.Value.ToCode(ref tempStr).Length < 40);
            */
            //smallEnough = false; // small enough needs a lot of love.
            if (!inString)
            {
                sb.Append("sb.Append(@\"");
                inString = true;
            }

            if (!smallEnough || LineInfo.Parent.Data == null)
                sb.Append(intend);
            sb.Append("<");
            sb.Append(_name);

            if (Modifiers.Count > 0)
                sb.Append(' ');

            // = is placed at the end of a tag definition, after class, id, and attribute declarations. 
            // It’s just a shortcut for inserting Ruby code into an element. It works the same as = without a tag: 
            // it inserts the result of the Ruby code into the template. 

            foreach (Node node in Modifiers)
                sb.Append(node.ToCode(ref inString, smallEnough));

            if (LineInfo.SelfClosed)
            {
                sb.Append("/>");
				if (!childSmallEnough)
                    sb.AppendLine();
                return sb.ToString();
            }

            if (childSmallEnough)
                sb.Append(">");
            else
                sb.AppendLine(">");

            foreach (Node node in Children)
                sb.Append(node.ToCode(ref inString, childSmallEnough));

            if (!inString)
            {
                sb.Append("sb.Append(@\"");
                inString = true;
            }

            if (!childSmallEnough)
            {
                // seems to be done by children
                //sb.AppendLine();
                // Quick fix for textareas (that'll otherwise include the indentation in the textarea content) until next haml parser is done
                if(_name != "textarea")
                    sb.Append(intend);
            }

            sb.Append("</");
            sb.Append(_name);
            sb.Append(">");
            if (!smallEnough || LineInfo.Parent.Data == null)
                sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Convert node to HTML (with ASP-tags)
        /// </summary>
        /// <returns>HTML string</returns>
        public override string ToHtml()
        {
            StringBuilder sb = new StringBuilder();
            string intend = LineInfo == null ? string.Empty : string.Empty.PadLeft(LineInfo.Intendation, '\t');
            sb.Append(intend);
            sb.Append("<");
            sb.Append(_name);

            if (Modifiers.Count != 0)
                sb.Append(' ');

            TextNode textNode = null;
            foreach (Node node in Modifiers)
            {
                if (node.IsTextNode)
                    textNode = node as TextNode;
                else
                    sb.Append(node.ToHtml());
            }

            if (LineInfo.SelfClosed)
            {
                sb.Append("/>");
                return sb.ToString();
            }
            else
                sb.Append(">");

            if (textNode != null)
                sb.Append(textNode.ToHtml());

            if (Children.Count != 0)
                sb.AppendLine();

            foreach (Node node in Children)
                sb.Append(node.ToHtml());

            
            sb.Append(intend);
            sb.Append("</");
            sb.Append(_name);
            sb.AppendLine(">");

            return sb.ToString();
        }
    }
}