using System;
using HttpServer.Rendering;

namespace HttpServer.Rendering.Haml.Rules
{
    /// <summary>
    /// Contains an (html) attribute list.
    /// </summary>
    public class AttributesRule : Rule
    {
        /// <summary>
        /// Determines if this node spans over multiple lines.
        /// </summary>
        /// <param name="line">contains line information (and text)</param>
        /// <param name="isContinued">true if the previous line was continued.</param>
        /// <returns>true if this line continues onto the next.</returns>
        public override bool IsMultiLine(LineInfo line, bool isContinued)
        {
            // hack to dont include code
            // a more proper way would have bene to scan after each tag
            if (!isContinued)
            {
                char ch = line.Data[0];
                if (ch != '#' && ch != '%' && ch != '.')
                    return false;
            }

            bool inQuote = false;
            bool inAttribute = false;
            if (isContinued && line.Data.IndexOf('{') == -1)
                inAttribute = true;
            foreach (char ch in line.Data)
            {
                if (ch == '"')
                    inQuote = !inQuote;
                else if (ch == '{' && !inQuote)
                {
                    if (inAttribute)
						throw new CodeGeneratorException(line.LineNumber, line.Data,
                            "Found another start of attributes, but no close tag. Have you forgot one '}'?");
                    inAttribute = true;
                }
                else if (ch == '}' && !inQuote)
                    inAttribute = false;
            }

            if (inQuote)
				throw new CodeGeneratorException(line.LineNumber, line.Data, "Attribute quotes can not span over multiple lines.");

            if (inAttribute)
            {
                //todo: Attach a log writer.
                //Console.WriteLine("Attribute is not closed, setting unfinished rule");
                line.UnfinishedRule = this;

            	line.Data.TrimEnd();
				if (line.Data.EndsWith("|"))
					line.TrimRight(1);
				
				return true;
            }

            return inAttribute;
        }


    }
}
