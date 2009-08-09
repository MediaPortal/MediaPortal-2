using HttpServer.Rendering.Haml.Nodes;
using HttpServer.Rendering.Haml;

namespace HttpServer.Rendering.Haml.Nodes
{
    /// <summary>
    /// Child nodes may not be the first node on a line
    /// </summary>
    public abstract class ChildNode : Node
    {
        /// <summary>
        /// Child nodes may not be the first node on a line
        /// </summary>
        /// <param name="parent">parent node</param>
        public ChildNode(Node parent) : base(parent)
        {
            
        }

        /// <summary>
        /// Creates a DIV node and add's the specified node to it.
        /// </summary>
        /// <param name="prototypes">Contains all prototypes for each control char. used to instanciate new nodes.</param>
        /// <param name="parent">parent node</param>
        /// <param name="line">current line information</param>
        /// <param name="me">node to add to the DIV node</param>
        /// <returns>current node</returns>
        public Node AddMe(NodeList prototypes, Node parent, LineInfo line, Node me)
        {
            if (parent == null)
            {
                TagNode tag = (TagNode)prototypes.CreateNode("%", parent);
                tag.Name = "div";
                tag.LineInfo = line;
                tag.AddModifier(me);
                return tag;
            }

            return me;
        }

        /// <summary>
        /// Get endposition for this modifier.
        /// </summary>
        /// <param name="offset">where to start searching</param>
        /// <param name="line">contents of the current line</param>
        /// <param name="terminator">char that tells us that this is the end position</param>
        /// <returns>index on the current line</returns>
        protected int GetEndPos(int offset, string line, char terminator)
        {
            // find string to parse
            bool inQuote = false;
            for (int i = offset + 1; i < line.Length; ++i)
            {
                char ch = line[i];
                if (ch == '"')
                    inQuote = !inQuote;
                else if (ch == terminator && !inQuote)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// This is a plain text node
        /// </summary>
        public override bool IsTextNode
        {
            get { return false; }
        }

        /// <summary>
        /// Get endposition for this modifier.
        /// </summary>
        /// <param name="offset">where to start searching</param>
        /// <param name="line">contents of the current line</param>
        /// <returns>index on the current line</returns>
        protected int GetEndPos(int offset, string line)
        {
            // find string to parse
            bool inQuote = false;
            for (int i = offset + 1; i < line.Length; ++i)
            {
                char ch = line[i];
                if (ch == '"')
                    inQuote = !inQuote;
                else if (!char.IsLetterOrDigit(ch) && !inQuote && ch != '_' && !(ch == '[' || ch == ']'))
                    return i;
            }

            return -1;
        }
    }
}