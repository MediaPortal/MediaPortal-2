using System.Text;

namespace HttpServer.Rendering.Haml.Nodes
{
    /// <summary>
    /// A partial is a HAML template that is inserted into other HAML templates.
    /// </summary>
	public class PartialNode : Node
	{
		/// <summary>
		/// Contains the page/controller target for the partial.
		/// The PartialNode should be written as follows:
		/// ex.
		/// 
		/// _"requestedpage"{parametername="parametervalue",parametername2=parameter2,parametername3=parameter3:typeof(parameter3type)}
		/// </summary>
		private string _target;

        /// <summary>
        /// create  a new partial node.
        /// </summary>
        /// <param name="parent">parent node</param>
		public PartialNode(Node parent) : base(parent)
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
		/// Returns the page/controller target for the node
		/// </summary>
		public string Target
		{
			get { return _target; }
		}

		/// <summary>
		/// Determines if this node can handle the line (by checking the first word);
		/// </summary>
		/// <param name="word">Controller char (word)</param>
		/// <returns>True if text belongs to this node type</returns>
		/// <param name="firstNode">First node on line.</param>
		public override bool CanHandle(string word, bool firstNode)
		{
			if(firstNode == false)
				return false;

			if (word.Length >= 1 && word[0] == '_')
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

			if (line.Data[offset] != '_')
				throw new CodeGeneratorException(line.LineNumber, line.Data, "Not a PartialNode");

			// From the first " sign (offset + 2) find the next " sign
			int pos = -1;
			for (int i = offset + 2; i < line.Data.Length; ++i)
			{
				if (line.Data[i] == '\"')
				{
					pos = i;
					break;
				}
			}
			if (pos == -1)
				throw new CodeGeneratorException(line.LineNumber, line.Data, "PartialNode does not contain an end paranthesis.");

			// Cut out the data between the two above found " signs and then start processing the address
			// The address is converted from the format /example/example/ to \\example\\example.haml
			PartialNode node = (PartialNode)prototypes.CreateNode("_", parent);
			node._target = line.Data.Substring(offset + 2, pos - offset - 2);
			if (node._target[node._target.Length - 1] == '/')
				node._target = node._target.Substring(0, node._target.Length - 1);
			if (node._target[0] == '/')
				node._target = node._target.Substring(1);
			node._target = node._target.Replace("/", "\\\\");
			node._target += ".haml";

			offset = pos + 1;
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
			if(Children.Count > 0)
				ThrowError(" must not contain children.");

			string code = GetCode();
			
			StringBuilder sb = new StringBuilder();
			string intend = LineInfo == null ? string.Empty : string.Empty.PadLeft(LineInfo.Intendation, '\t');

			if (inString)
			{
				sb.Append("\");");
				inString = false;
			}

			sb.Append(intend);
			sb.Append("sb.Append(");
			sb.Append(code);
			sb.Append(");");

			return sb.ToString();
		}

        /// <summary>
        /// Convert node to HTML (with ASP-tags)
        /// </summary>
        /// <returns>HTML string</returns>
        public override string ToHtml()
		{
			string code = GetCode();

			string intend = LineInfo == null ? string.Empty : string.Empty.PadLeft(LineInfo.Intendation, '\t');
			return string.Format("{1}<% {0} %>", intend, code);
		}

		/// <summary>
		/// Helper function to retrieve the code for the partial request
		/// </summary>
		/// <returns>A string representing the code for invocating a render of the partial</returns>
		protected string GetCode()
		{
			string argumentString = string.Empty;
			if (Modifiers.Count == 1 && Modifiers.First.Value is AttributeNode)
			{
				foreach (AttributeNode.Attribute attrib in ((AttributeNode)Modifiers.First.Value).Attributes)
				{
					// Put the argument name within parenthesis
					argumentString += "\"" + attrib.Name + "\", ";

					// If the value contains a colon the part after the colon is extracted and used as a typeinfo argument
					if (attrib.Value.Contains(":"))
					{
						string[] parts = attrib.Value.Split(':');
						if (parts.Length > 2)
							ThrowError(" must only contain one colon(:) per attribute to define type.");

						argumentString += parts[0] + ", " + parts[1];
					}
					else
						argumentString += attrib.Value;

					argumentString += ", ";
				}
			}
			else if (Modifiers.Count > 1)
				ThrowError(" can at most contain one AttributeNode.");
			else if (Modifiers.Count == 1 && !(Modifiers.First.Value is AttributeNode))
				ThrowError(" can only contain an AttributeNode.");

			// Remove last comma if arguments were passed
			if (!string.IsNullOrEmpty(argumentString))
				argumentString = argumentString.Substring(0, argumentString.Length - 2);

			return "hiddenTemplateManager.RenderPartial(\"" + _target + "\", args, new TemplateArguments(" + argumentString + "))";
		}

        /// <summary>
        /// Throw an exception with predefined information
        /// </summary>
        /// <param name="reason">why the exception was thrown</param>
		protected void ThrowError(string reason)
		{
			if (LineInfo == null)
                throw new CodeGeneratorException(0, "PartialNode with target '" + _target + "'" + reason);

            throw new CodeGeneratorException(LineInfo.LineNumber, LineInfo.Data, "PartialNode with target '" + _target + "'" + reason);
		}
	}
}
