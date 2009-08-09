using System.Collections.Generic;
using System.Text;
using HttpServer.Rendering;
using HttpServer.Rendering.Haml.Nodes;
using HttpServer.Rendering.Haml;

namespace HttpServer.Rendering.Haml.Nodes
{
    /// <summary>
    /// Contains HTML attributes.
    /// </summary>
    public class AttributeNode : ChildNode
    {
        /// <summary>
        /// A attribute
        /// </summary>
        public class Attribute
        {
            /// <summary>
            /// value is a simple word or quoted text
            /// </summary>
            public bool Simple;
            /// <summary>
            /// Name of the attribute
            /// </summary>
            public string Name;
            /// <summary>
            /// Value, can be a statement, variable or quoted text.
            /// </summary>
            public string Value;
        }
        private List<Attribute> _attributes;

        /// <summary>
        /// Create a new node
        /// </summary>
        /// <param name="parent">parent node</param>
        /// <param name="col">collection of attributes</param>
        public AttributeNode(Node parent, List<Attribute> col)
            : base(parent)
        {
            _attributes = col;
        }

        /// <summary>
        /// create an attribute node
        /// </summary>
        /// <param name="parent">parent node</param>
        public AttributeNode(Node parent) : base(parent)
        {
            _attributes = new List<Attribute>();
        }

        /// <summary>
        /// Get an attribute
        /// </summary>
        /// <param name="name">name of the attribute (case sensitive)</param>
        /// <returns>attribute if found; otherwise null.</returns>
        public Attribute GetAttribute(string name)
        {
            foreach (Attribute attribute in _attributes)
            {
                if (attribute.Name == name)
                    return attribute;
            }

            return null;
        }

        /// <summary>
        /// html attributes
        /// </summary>
        public List<Attribute> Attributes
        {
            get { return _attributes; }
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
            if (line.Data[offset] != '{')
                throw new CodeGeneratorException(line.LineNumber, line.Data, "Attribute cant handle info at char " + offset + 1);

            int endPos = GetEndPos(offset, line.Data, '}');
            if (endPos == -1)
                throw new CodeGeneratorException(line.LineNumber, line.Data, "Failed to find end of attribute list: '" + line.UnparsedData + "'.");

            List<Attribute> col = new List<Attribute>();
            string attributes = line.Data.Substring(offset + 1, endPos - offset - 1);
            ParseAttributes(line, attributes, col);
            offset = endPos + 1;


            AttributeNode node = (AttributeNode)prototypes.CreateNode("{", parent);
            node._attributes = col;
            return AddMe(prototypes, parent, line, node);
        }

        private static void ParseAttributes(LineInfo line, string attributes, List<Attribute> col)
        {
            bool inQuote = false;
            int parenthisCount = 0;
            string name = null;
            int start = -1;
            int step = 0; //0 = start of name, 1 = end of name, 2 = equal sign, 3 = start of value, 4 = end of value, 5 = comma
            for (int i = 0; i < attributes.Length; ++i)
            {
                char ch = attributes[i];

                if (ch == '"')
                {
                    inQuote = !inQuote;
                    if (inQuote && step == 3)
                    {
                        ++step;
                        start = i;
                    }
                }

                if (inQuote)
                    continue;

                if (ch == '(')
                    ++parenthisCount;
                if (ch == ')')
                    --parenthisCount;
                if (parenthisCount > 0)
                    continue;

                // find start of name
                if (step == 0)
                {
                    if (!char.IsWhiteSpace(ch))
                    {
                        start = i;
                        ++step;
                    }
                }
                    // find end of name
                else if (step == 1)
                {
                    if (char.IsWhiteSpace(ch) || ch == '=')
                    {
                        name = attributes.Substring(start, i - start);
                        start = -1;
                        ++step;
                    }
                }

                // find equal
                if (step == 2)
                {
                    if (ch == '=')
                        ++step;
                    continue;
                }

                // start of value
                if (step == 3)
                {
                    if (!char.IsWhiteSpace(ch))
                    {
                        start = i;
                        ++step;
                    }
                }

                    // end of value
                else if (step == 4)
                {
                    if (ch == ',')
                    {
                        AddAttribute(col, name, attributes.Substring(start, i - start).Trim());
                        start = -1;
                        ++step;
                    }
                }

                // find comma
                if (step == 5)
                {
                    if (ch == ',')
                        step = 0;

                    continue;
                }
            }

            if (step > 0 && step < 4)
                throw new CodeGeneratorException(line.LineNumber, line.Data, "Invalid attributes");

            if (step == 4)
                AddAttribute(col, name, attributes.Substring(start, attributes.Length - start));
        }

        private static void AddAttribute(List<Attribute> col, string name, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            // need to end with a ", else parsing will fail.
/*            
if (value[0] == '\"' && value[value.Length -1] != '\"')
                col.Add(name, value + "+ @\"\"");            else
            {*/
            bool complex = false;
            bool inStr = false;
            for (int i = 0; i < value.Length; ++i )
            {
                if (value[i] == '"')
                {
                    inStr = !inStr;
                    continue;
                }
                if (!inStr)
                {
                    complex = true;
                    break;
                }
            }

                //if (pos != -1)
                  //  value = value.Insert(pos, "@");

            Attribute attr = new Attribute();
            attr.Simple = !complex;
            attr.Name = name;
            attr.Value = value;
            col.Add(attr);
            //}
        }

        /// <summary>
        /// determines if this node can handle the line (by checking the first word);
        /// </summary>
        /// <param name="word">Controller char (word)</param>
        /// <returns>true if text belongs to this node type</returns>
        /// <param name="firstNode">first node on line</param>
        public override bool CanHandle(string word, bool firstNode)
        {
            if (word.Length >= 1 && word[0] == '{' && !firstNode)
                return true;

            return false;
        }

        /// <summary>
        /// Convert node to HTML (with ASP-tags)
        /// </summary>
        /// <returns>HTML string</returns>
        public override string ToHtml()
        {
            StringBuilder attrs = new StringBuilder();
            for (int i = 0; i < Attributes.Count; ++i)
            {
                if (!Attributes[i].Simple)
                    attrs.AppendFormat("{0}=<%= {1} %> ", Attributes[i].Name, Attributes[i].Value);
                else
                    attrs.AppendFormat("{0}={1} ", Attributes[i].Name, Attributes[i].Value);
            }

            return attrs.ToString();
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
            StringBuilder attrs = new StringBuilder();
            for (int i = 0; i < Attributes.Count; ++i)
            {
                if (!Attributes[i].Simple)
                    attrs.AppendFormat("{0}=\"\"\"); sb.Append({1}); sb.Append(@\"\"\" ", Attributes[i].Name,
                                       Attributes[i].Value);
                else
                    attrs.AppendFormat("{0}=\"{1}\" ", Attributes[i].Name, Attributes[i].Value);
            }

            if (attrs.Length > 1)
                attrs.Length = attrs.Length - 1;
            return attrs.ToString();            
        }

    }
}