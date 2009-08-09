using System;
using System.Collections.Generic;
using System.Text;

namespace Tiny.Web.Templates
{
    /// <summary>
    /// The purpose of this class is to provide template engines
    /// to the controllers.
    /// </summary>
    public class TemplateProvider
    {
        private IDictionary<string, TemplateEngine> _engines = new Dictionary<string, TemplateEngine>();
        private TemplateEngine _defaultEngine = new TinyTemplate();

        public TemplateEngine DefaultEngine
        {
            get { return _defaultEngine; }
            set { _defaultEngine = value; }
        }

        public void Add(string extension, TemplateEngine engine)
        {
            if (!extension.Equals("tm"))
                Console.WriteLine("Warning: key is set to 'tm' regardless of the value of extension");
            _engines.Add("tm", engine);
        }

        /// <summary>
        /// Get a template engine.
        /// The default template engine is TemplateMachine and it's extension is "tm".
        /// </summary>
        /// <param name="extension">File type that we want to get an engine for</param>
        /// <returns></returns>
        /// <example>
        /// ITemplateEngine engine = server.GetEngine("tm");
        /// </example>
        public TemplateEngine GetEngine(string extension)
        {
            if (_engines.ContainsKey(extension))
                return _engines[extension];
            else
                return DefaultEngine;
        }

    }
}
