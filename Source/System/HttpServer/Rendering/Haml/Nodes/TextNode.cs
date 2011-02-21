using System;
using System.Text;
using HttpServer.Rendering;
using HttpServer.Rendering.Haml;

namespace HttpServer.Rendering.Haml.Nodes
{
    /// <summary>
    /// A text only node.
    /// </summary>
    public class TextNode : Node
    {
        private string _text;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent">parent node</param>
        /// <param name="text">plain text</param>
        public TextNode(Node parent, string text) : base(parent)
        {
            Text = text;
        }
        
        /// <summary>
        /// The text.
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        /// <summary>
        /// Is this a text node (containing plain text)?
        /// </summary>
        public override bool IsTextNode
        {
            get { return true; }
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
            // text on tag rows are identified by a single space.
            if (parent != null && line.Data[offset] == ' ')
                ++offset;

            TextNode node = new TextNode(parent, line.Data.Substring(offset));
            if (parent == null)
                node.LineInfo = line;
            offset = line.Data.Length;
            return node;
        }

        /// <summary>
        /// determines if this node can handle the line (by checking the first word);
        /// </summary>
        /// <param name="word">Controller char (word)</param>
        /// <returns>true if text belongs to this node type</returns>
        /// <param name="firstNode">true if this is the first node on the line.</param>
        public override bool CanHandle(string word, bool firstNode)
        {
            return word.Length > 0 && (char.IsWhiteSpace(word[0]));
        }

        /// <summary>
        /// Generate HTML for this node.
        /// </summary>
        /// <returns></returns>
        public override string ToHtml()
        {
            // lineinfo = first node on line
            if (LineInfo != null)
                return string.Empty.PadLeft(GetIntendation(), '\t') + _text + Environment.NewLine;
            else
                return _text;
        }

        /// <summary>
        /// Convert the node to c# code
        /// </summary>
        /// <param name="inString">True if we are inside the internal stringbuilder</param>
        /// <param name="smallEnough">true if all subnodes fit on one line</param>
        /// <param name="smallEnoughIsDefaultValue">todo: add description</param>
        /// <returns>c# code</returns>
        protected override string ToCode(ref bool inString, bool smallEnough, bool smallEnoughIsDefaultValue)
        {
            int intendCount = GetIntendation();
            string intend = string.Empty.PadLeft(intendCount, '\t');
            string text = _text.Replace("\"", "\"\"");

            StringBuilder sb = new StringBuilder();
            if (!inString)
            {
                sb.Append("sb.Append(@\"");
                inString = true;
            }

            if (Children.Count > 0)
            {
                if (smallEnough)
                    sb.Append(text);
                else
                {
                    sb.Append(intend);
                    sb.AppendLine(text);
                }

                foreach (Node node in Children)
                    sb.Append(node.ToCode(ref inString, smallEnough));

            }
            else
            {
                // lineinfo = first node on line
                if (LineInfo != null && !smallEnough)
                    sb.AppendLine(intend + text);
                else
                    sb.Append(text);
                
            }

            return sb.ToString();
        }

    }
}
