using System;
using Fadd;

namespace HttpServer.Rendering
{
    /// <summary>
    /// The compiler is responsible of creating a render object which can be
    /// cached and used over and over again.
    /// </summary>
    /// <seealso cref="TemplateManager"/>
    /// <seealso cref="ITemplateGenerator"/>
    public class TemplateCompiler
    {
        /// <summary>
        /// Base c# code for a template object.
        /// </summary>
        public static string TemplateBase =
            @"namespace Tiny.Templates {
    class {id} :  ITinyTemplate
    {
        {members}

        public string Invoke(TemplateArguments args, TemplateManager hiddenTemplateManager)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            {body}

            return sb.ToString();
        }
    }
}";
        private readonly Compiler _compiler;
        private string _generatedTemplate;

        /// <summary>
        /// Create a new template compiler
        /// </summary>
        public TemplateCompiler()
        {
            _compiler = new Compiler();
            _compiler.Add(GetType());
            _compiler.Add(typeof(ITinyTemplate));
        }

        /// <summary>
        /// Adds the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void Add(Type type)
        {
            _compiler.Add(type);
        }

        /// <summary>
        /// Compiles the specified args.
        /// </summary>
        /// <param name="args">Arguments, should contain "name, value, name, value" etc.</param>
        /// <param name="template">c# code that will be included in the generated template class</param>
        /// <param name="templateId">Id of the template class</param>
        /// <returns>Tiny template if successful; otherwise null.</returns>
        /// <exception cref="CompilerException">If compilation fails</exception>
        /// <exception cref="ArgumentException">If args are incorrect</exception>
        public ITinyTemplate Compile(TemplateArguments args, string template, string templateId)
        {
            ArgumentContainer[] arguments = args.GetArguments();
            foreach (ArgumentContainer arg in arguments)
                _compiler.Add(arg.Type);

            string members = string.Empty;
            string body = string.Empty;
            foreach (ArgumentContainer arg in arguments)
            {
                members += Compiler.GetTypeName(arg.Type) + " " + arg.Name + ";" + Environment.NewLine;
                body += "this." + arg.Name + " = (" + Compiler.GetTypeName(arg.Type) + ")args[\"" + arg.Name + "\"].Object;" + Environment.NewLine;
            }
            
            body += template;

            _generatedTemplate =
				TemplateBase.Replace("{id}", templateId).Replace("{members}", members).Replace("{body}", body);

            _compiler.Compile(_generatedTemplate);
            return _compiler.CreateInstance<ITinyTemplate>();
        }


    }


}