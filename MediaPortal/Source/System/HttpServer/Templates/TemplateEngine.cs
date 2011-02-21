using System.Collections.Generic;
using System.IO;

namespace Tiny.Web.Templates
{
    public interface TemplateEngine
    {
        /// <summary>
        /// Render the template
        /// </summary>
        /// <param name="variables"></param>
        /// <param name="fileName"></param>
        /// <returns>A generated file</returns>
        /// <exception cref="FileNotFoundException">If template is not found.</exception>
        string Render(string fileName, IDictionary<string, object> variables);

        string Render(string fileName, params object[] args);
    }
}