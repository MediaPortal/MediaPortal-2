using System;
using HttpServer.Rendering.Haml;
using HttpServer.Rendering.Haml.Nodes;

namespace HttpServer.Rendering.Haml.Nodes
{
    internal class DocTypeTag : Node
    {
        private readonly string _docType;

        public DocTypeTag(string docType, Node parent) : base(parent)
        {
            _docType = docType;
        }

        /// <summary>
        /// Text nodes should be added as child.
        /// </summary>
        public override bool IsTextNode
        {
            get { return false; }
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
            offset = line.Data.Length;
            return new DocTypeTag(
                @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">".Replace("\"", "\"\""),
                parent);

        }

        /// <summary>
        /// determines if this node can handle the line (by checking the first word);
        /// </summary>
        /// <param name="word">Controller char (word)</param>
        /// <returns>true if text belongs to this node type</returns>
        /// <param name="firstNode">First node on line, used since some nodes cannot exist on their own on a line.</param>
        public override bool CanHandle(string word, bool firstNode)
        {
            return word.Length >= 3 && word.Substring(0, 3) == "!!!" && firstNode;
        }

        /// <summary>
        /// Convert node to HTML (with ASP-tags)
        /// </summary>
        /// <returns>HTML string</returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string ToHtml()
        {
            throw new NotImplementedException();
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
            return _docType + Environment.NewLine;
        }
    }
}
