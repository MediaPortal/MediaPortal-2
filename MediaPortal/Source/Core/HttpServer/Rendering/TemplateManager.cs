using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Fadd;
#if DEBUG
using HttpServer.Rendering.Haml;
using Xunit;
#endif

namespace HttpServer.Rendering
{
    /// <summary>
    /// Purpose if this class is to take template objects and keep them in
    /// memory. It will also take a filename and the code generator to use
    /// if when the template have been changed on disk.
    /// </summary>
    public class TemplateManager
    {
        private readonly Dictionary<string, TemplateInfoImp> _compiledTemplates = new Dictionary<string, TemplateInfoImp>();
        private readonly Dictionary<string, ITemplateGenerator> _generators = new Dictionary<string, ITemplateGenerator>();
        private readonly List<Type> _includedTypes = new List<Type>();
        private readonly List<ITemplateLoader> _templateLoaders = new List<ITemplateLoader>();
        //TODO: Create a path/template index (to avoid unnecessary IO).

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateManager"/> class.
        /// </summary>
        /// <param name="loaders">
        /// Template loaders used to load templates from any source.
        /// The loaders will be invoked in the order they are given, that is the first loader will always be asked to give a template
        /// first.
        /// </param>
        public TemplateManager(params ITemplateLoader[] loaders)
        {
			Check.Require(loaders.Length, "Parameter loaders must contain at least one ITemplateLoader.");
            _templateLoaders.AddRange(loaders);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateManager"/> class.
        /// </summary>
        /// <remarks>Uses the file template loader.</remarks>
        public TemplateManager()
        {
			_templateLoaders.Add(new FileTemplateLoader());
        }

        /// <summary>
        /// Add a template generator
        /// </summary>
        /// <param name="fileExtension">File extension without the dot.</param>
        /// <param name="generator">Generator to handle the extension</param>
        /// <exception cref="InvalidOperationException">If the generator already exists.</exception>
        /// <exception cref="ArgumentException">If file extension is incorrect</exception>
        /// <exception cref="ArgumentNullException">If generator is not specified.</exception>
        /// <example>
        /// <code>
        /// cache.Add("haml", new HamlGenerator());
        /// </code>
        /// </example>
        public void Add(string fileExtension, ITemplateGenerator generator)
        {
            if (string.IsNullOrEmpty(fileExtension) || fileExtension.Contains("."))
                throw new ArgumentException("Invalid file extension.");
            if (generator == null)
                throw new ArgumentNullException("generator");

            if (_generators.ContainsKey(fileExtension))
                throw new InvalidOperationException("A generator already exists for " + fileExtension);

            _generators.Add(fileExtension, generator);
        }

        /// <summary>
        /// This type should be included, so it may be called from the scripts (name space and assembly).
        /// </summary>
        /// <param name="type"></param>
        public void AddType(Type type)
        {
            bool assemblyExists = false;
            bool nsExists = false;
            foreach (Type includedType in _includedTypes)
            {
                if (includedType.Namespace == type.Namespace)
                    nsExists = true;
                if (includedType.Assembly == type.Assembly)
                    assemblyExists = true;
                if (nsExists && assemblyExists)
                    break;
            }

            if (!assemblyExists || !nsExists)
                _includedTypes.Add(type);
        }

        /// <summary>
        /// Checks the template.
        /// </summary>
        /// <param name="info">Template information, filename must be set.</param>
        /// <returns>true if template exists and have been compiled.</returns>
        private bool CheckTemplate(ITemplateInfo info)
        {
            if (info == null)
                return false;

			foreach (ITemplateLoader loader in _templateLoaders)
				if (loader.HasTemplate(info.Filename))
					return loader.CheckTemplate(info);

        	return false;
        }

        /// <summary>
        /// Compiles the specified code.
        /// </summary>
        /// <param name="fileName">Name of template.</param>
        /// <param name="code">c# code generated from a template.</param>
        /// <param name="arguments">Arguments as in name, value, name, value, name, value</param>
        /// <param name="templateId">
        /// An id to specify the exact instance of a template. Made from joining the 'TemplateClass' with the hashcode of the filename
        /// and the hashcode of the supplied arguments
        /// </param>
        /// <returns>Template</returns>
        /// <exception cref="TemplateException">If compilation fails</exception>
        protected ITinyTemplate Compile(string fileName, string code, TemplateArguments arguments, string templateId)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Code is not specified.");

            TemplateCompiler compiler = new TemplateCompiler();
            foreach (Type type in _includedTypes)
                compiler.Add(type);

            try
            {
                return compiler.Compile(arguments, code, templateId);
            }
            catch(CompilerException err)
            {
                throw new TemplateException(fileName, err);
            }
        }

        /// <summary>
        /// Will generate code from the template.
        /// Next step is to compile the code.
        /// </summary>
        /// <param name="path">Path and filename to template.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException">If no template generator exists for the specified extension.</exception>
        /// <exception cref="CodeGeneratorException">If parsing/compiling fails</exception>
        /// <see cref="Render(string, TemplateArguments)"/>
        private string GenerateCode(ref string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("No filename was specified.");

            int pos = path.LastIndexOf('.');
            if (pos == -1)
                throw new ArgumentException("Filename do not contain a file extension.");
            if (pos == path.Length - 1)
                throw new ArgumentException("Invalid filename '" + path + "', should not end with a dot.");

            string extension = path.Substring(pos + 1);


            lock (_generators)
            {
                ITemplateGenerator generator = null;
                if (extension == "*")
                    generator = GetGeneratorForWildCard(ref path);
                else
                {
                    if (_generators.ContainsKey(extension))
                        generator = _generators[extension];
                }

                if (generator == null)
                    throw new InvalidOperationException("No template generator exists for '" + path + "'.");

                TextReader reader = null;
                try
                {
                    foreach (ITemplateLoader loader in _templateLoaders)
            	    {
            		    reader = loader.LoadTemplate(path);
					    if (reader != null)
						    break;
            	    }

                    if (reader == null)
                        throw new FileNotFoundException("Did not find template: " + path);

                    generator.Parse(reader);
                    reader.Close();
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();                    
                }

                StringBuilder sb = new StringBuilder();
                using (TextWriter writer = new StringWriter(sb))
                {
                    generator.GenerateCode(writer);
                    return sb.ToString();
                }
            }
        }

        /// <summary>
        /// Find a template using wildcards in filename.
        /// </summary>
        /// <param name="filePath">Full path (including wildcards in filename) to where we should find a template.</param>
        /// <returns>First found generator if an extension was matched; otherwise null.</returns>
        /// <remarks>method is not thread safe</remarks>
        private ITemplateGenerator GetGeneratorForWildCard(ref string filePath)
        {
            int pos = filePath.LastIndexOf('\\');
            if (pos == -1)
                throw new InvalidOperationException("Failed to find path in filename.");

            string path = filePath.Substring(0, pos);
            string filename = filePath.Substring(pos + 1);

			List<string> files = new List<string>();
        	foreach (ITemplateLoader loader in _templateLoaders)
				files.AddRange(loader.GetFiles(path, filename));
            
            for (int i = 0; i < files.Count; ++i)
            {
                pos = files[i].LastIndexOf('.');
                string extension = files[i].Substring(pos + 1);

                if (!_generators.ContainsKey(extension)) 
                    continue;

                if(filePath.EndsWith("*"))
                    filePath = filePath.TrimEnd('*') + extension;

                return _generators[extension] ;
            }

            return null;
        }

#if DEBUG
        [Fact]
        private void TestGetGeneratorForWildCard()
        {
            string resource = "rendering\\resourcetest.*";
            Add("haml", new HamlGenerator());
            Add("tiny", new Tiny.TinyGenerator());
			_templateLoaders.Clear();
        	ResourceTemplateLoader loader = new ResourceTemplateLoader();
			loader.LoadTemplates("rendering/", loader.GetType().Assembly, "HttpServer.Rendering");
            _templateLoaders.Add(loader);
            ITemplateGenerator gen = GetGeneratorForWildCard(ref resource);
            Assert.NotNull(gen);
            Assert.IsType(typeof (HamlGenerator), gen);
        }

		[Fact]
		private void TestMultipleLoaders()
		{
			const string resource = "rendering\\resourcetest.*";
			Add("haml", new HamlGenerator());
			Add("tiny", new Tiny.TinyGenerator());
			if (_templateLoaders.Count == 0)
				_templateLoaders.Add(new FileTemplateLoader());
			ResourceTemplateLoader loader = new ResourceTemplateLoader();
			loader.LoadTemplates("rendering/", loader.GetType().Assembly, "HttpServer.Rendering");
			_templateLoaders.Add(loader);
			string result = Render(resource, null);
			
			Assert.NotNull(result);
			Assert.True(result.StartsWith("This file is used to test the resource template loader"));

			((FileTemplateLoader)_templateLoaders[0]).PathPrefix = "..\\..\\";
			result = Render(resource, null);

			Assert.NotNull(result);
			Assert.True(result.StartsWith("This file is used to test the resource template loader"));
		}
#endif
		/// <summary>
		/// Render a partial
		/// </summary>
		/// <param name="filename">Path and filename</param>
		/// <param name="templateArguments">Variables used in the template. Should be specified as "name, value, name, value" where name is variable name and value is variable contents.</param>
		/// <param name="partialArguments">Arguments passed from parent template</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="TemplateException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public string RenderPartial(string filename, TemplateArguments templateArguments, TemplateArguments partialArguments)
		{
			templateArguments.Update(partialArguments);
			return Render(filename, templateArguments);
		}

		/// <summary>
		/// Generate HTML from a template.
		/// </summary>
		/// <param name="filename">Path and filename</param>
		/// <param name="args">Variables used in the template. Should be specified as "name, value, name, value" where name is variable name and value is variable contents.</param>
		/// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="CompilerException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <example>
		/// <code>
		/// string html = cache.Generate("views\\users\\view.haml", new TemplateArguments("user", dbUser, "isAdmin", dbUser.IsAdmin), null);
        /// </code>
		/// </example>
        public string Render(string filename, TemplateArguments args)
        {
			if (args == null)
				args = new TemplateArguments();

			// Generate a new proper filename (the generator gets saved aswell) : todo, this works perfectly but isnt so good looking, is it?
			GetGeneratorForWildCard(ref filename);

			// Generate a name identifying the template
			string templateName = "TemplateClass" + filename.GetHashCode() + args.GetHashCode();
			templateName = templateName.Replace('-', 'N');
			
            TemplateInfoImp info;
            lock (_compiledTemplates)
            {
				if (_compiledTemplates.ContainsKey(templateName))
					info = _compiledTemplates[templateName];
                else
                {
                    info = new TemplateInfoImp();
                    info.Filename = filename;
                    info.Template = null;
                    info.CompiledWhen = DateTime.MinValue;
					_compiledTemplates.Add(templateName, info);
                }
            }

            lock (info)
            {
                if (!CheckTemplate(info) || info.Template == null)
                {
                    string code = GenerateCode(ref filename);
                    info.Template = Compile(filename, code, args, templateName);
                    info.CompiledWhen = DateTime.Now;
                	info.Filename = filename;
                }

                return info.Template.Invoke(args, this);
            }
        }

        #region Nested type: TemplateInfoImp

        /// <summary>
        /// Keeps information about templates, so we know when to regenerate it.
        /// </summary>
        private class TemplateInfoImp : ITemplateInfo
        {
            private DateTime _compiledWhen;
            private string _filename;
            private ITinyTemplate _template;

            public DateTime CompiledWhen
            {
                get { return _compiledWhen; }
                set { _compiledWhen = value; }
            }

            public string Filename
            {
                get { return _filename; }
                set { _filename = value; }
            }

            public ITinyTemplate Template
            {
                get { return _template; }
                set { _template = value; }
            }
        }


        #endregion
    }
}
