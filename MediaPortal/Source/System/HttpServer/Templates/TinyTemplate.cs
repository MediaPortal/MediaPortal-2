using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace Tiny.Web.Templates
{
    /// <summary>
    /// Simple template engine.
    /// This engine turns the templates into valid C# code and compile it at runtime
    /// to valid .Net objects. This makes the templates blazing fast, the templates
    /// are only reparsed / recompiles if they have been changed on disk.
    /// </summary>
    public class TinyTemplate : TemplateEngine
    {
        class TemplateInfo
        {
            public string fileName;
            public DateTime Changed;
            public object compiledTemplate;
        }
        private IDictionary<string, TemplateInfo> _compiledTemplates =new Dictionary<string, TemplateInfo>();


        public string Render(string fileName, IDictionary<string, object> variables)
        {
            object[] args = DictionaryToArray(variables);
            return Render(fileName, args);
        }

        public string Render(string fileName, params object[] args)
        {
            // Validate the args
            if (args.Length % 2 != 0)
                throw new ArgumentException("args must consist of key/value pairs, where the key is a string", "args");
            for (int i = 0; i < args.Length; i += 2 )
            {
                if (args[i].GetType() != typeof(string))
                    throw new ArgumentException("Args array must be string, object, string, object ...., where the strings are the argument names", "args");
            }

            TemplateInfo template = null;
            // Template exists, invoke it if have not been changed on disk.
            if (_compiledTemplates.ContainsKey(fileName))
            {
                TemplateInfo ti = _compiledTemplates[fileName];

                // Template have changed, let's reload it
                if (ti.Changed < File.GetLastWriteTime(fileName))
                {
                    object templateClass = BuildTemplate(args, fileName);
                    if (templateClass == null)
                        return null;

                    ti.compiledTemplate = templateClass;
                    ti.Changed = DateTime.Now;
                }
                template = ti;
            }
            else
            {
                object templateClass = BuildTemplate(args, fileName);
                if (templateClass == null)
                    return null;
                template = new TemplateInfo();
                template.compiledTemplate = templateClass;
                template.fileName = fileName;
                template.Changed = DateTime.Now;
                _compiledTemplates.Add(fileName, template);
            }

            object o = template.compiledTemplate;
            return (string)o.GetType().InvokeMember("RunTemplate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, o, new object[] { args });            
        }

        private object[] DictionaryToArray(IDictionary<string, object> variables)
        {
            object[] args = new object[variables.Count];
            int index = 0;
            foreach (KeyValuePair<string, object> pair in variables)
            {
                args[index] = pair.Value;
                ++index;
            }

            return args;
        }

        private object BuildTemplate(object[] args, string fileName)
        {
            if (args.Length % 2 != 0)
                throw new ArgumentException("Args must consist of string, object, string, object, string, object as key/value couple.");

            string text = File.ReadAllText(fileName);
            StringBuilder sb = new StringBuilder(text.Length * 2);

            IList<string> assemblies = new List<string>();
            IList<string> namespaces = new List<string>();

            AddNameSpace(namespaces,typeof(Forms.ObjectForm));

            for (int i = 0; i < args.Length; i += 2)
            {
                AddAssembly(assemblies, args[i+1].GetType());
                AddNameSpace(namespaces, args[i+1].GetType());
            }

            foreach (string s in namespaces)
                sb.AppendLine("using " + s + ";");

            sb.Append("namespace Tiny.Templates { class TemplateClass {\r\n");

            for (int i = 0; i < args.Length; i += 2)
                sb.Append(GetTypeName(args[i + 1].GetType()) + " " + args[i] + ";\r\n");
            sb.Append("public string RunTemplate(object[] args) {\r\n");
            sb.Append("System.Text.StringBuilder sb = new System.Text.StringBuilder();\r\n");

            for (int i = 1; i < args.Length; i += 2)
                sb.Append("this." + args[i-1] + " = (" + GetTypeName(args[i].GetType()) + ")args[" + i  + "];\r\n");

            BuildTemplateString(sb, text);
            sb.Append("\r\nreturn sb.ToString(); }}}");

            return CompileTemplate(assemblies, sb);
        }


        private void AddNameSpace(IList<string> _namespaces, Type type)
        {
            string ns = type.Namespace;
            bool found = false;
            foreach (string s in _namespaces)
            {
                if (string.Compare(s, ns, true) == 0)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                _namespaces.Add(ns);

            foreach (Type argument in type.GetGenericArguments())
                AddNameSpace(_namespaces, argument);
        }

        private void AddAssembly(IList<string> _assemblies, Type type)
        {
            string path = type.Assembly.Location;
            bool found = false;
            foreach (string s in _assemblies)
            {
                if (string.Compare(s, path, true) == 0)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                _assemblies.Add(path);

            foreach (Type argument in type.GetGenericArguments())
                AddNameSpace(_assemblies, argument);
        }

        /// <summary>
        /// Used to get correct names for generics.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetTypeName(Type type)
        {
            string typeName = type.Name;
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(typeName.Substring(0, typeName.IndexOf('`')));
                sb.Append("<");
                bool first = true;
                foreach (Type genericArgumentType in type.GetGenericArguments())
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;
                    sb.Append(GetTypeName(genericArgumentType));
                }
                sb.Append(">");
                return sb.ToString();
            }
            else
                return typeName;
        }

        private object CompileTemplate(IList<string> _assemblies, StringBuilder classText)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = false;
            parameters.GenerateInMemory = true;
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            foreach (string assembly in _assemblies)
                parameters.ReferencedAssemblies.Add(ResolveAssemblyPath(assembly));

            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, classText.ToString());
            if (results.Errors.Count > 0)
            {
                string errs = "";

                foreach (CompilerError CompErr in results.Errors)
                {
                    errs += "Template: " + CompErr.FileName + Environment.NewLine +
                        "Line number: " + CompErr.Line + Environment.NewLine +
                        "Error: " + CompErr.ErrorNumber + " '" + CompErr.ErrorText + "'";
                }
                Console.WriteLine(errs);
                return null;
            }
            else
            {
                Assembly generatorAssembly = results.CompiledAssembly;
                object classObj = generatorAssembly.CreateInstance("Tiny.Templates.TemplateClass", false, BindingFlags.CreateInstance, null, null, null, null);
                return classObj;
            }

        }

        private string ResolveAssemblyPath(string name)
        {
            if (name.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                return name;

            name = name.ToLower();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (IsDynamicAssembly(assembly))
                {
                    continue;
                }

                if (Path.GetFileNameWithoutExtension(assembly.Location).ToLower().Equals(name))
                {
                    return assembly.Location;
                }
            }

            string foo = name.Substring(name.Length-4,1);
            if (!(foo.Equals(".")))
                name += ".dll";
                //return Path.GetFullPath(name);
            return Path.GetFullPath(name);
        }

        private void ParseInstructions(string code)
        {
            string tempcode = code;
            //Modify this part if you want to read the 

            //<%@ tags also you can implement your own tags here.

            tempcode = Regex.Replace(tempcode,
                       "(?i)<%@\\s*Property.*?%>", string.Empty);
            tempcode = Regex.Replace(tempcode,
                       "(?i)<%@\\s*Assembly.*?%>", string.Empty);
            tempcode = Regex.Replace(tempcode,
                       "(?i)<%@\\s*Import.*?%>", string.Empty);
            tempcode = Regex.Replace(tempcode,
                       "(?i)<%@\\s*CodeTemplate.*?%>", string.Empty);
            //For the demo I am only dealing with the <%= and <% tags            
        }

        private bool IsDynamicAssembly(Assembly assembly)
        {
            return assembly.ManifestModule.Name.StartsWith("<");
        }

        private string BuildTemplateString(StringBuilder sb, string data)
        {
            /*sb.Append("sb.Append(@\"");
            data = data.Replace("-%>" + Environment.NewLine, "%>");
            data = Regex.Replace(data, @"<%=.*?%>", new MatchEvaluator(RefineCalls), RegexOptions.Singleline);
            data = Regex.Replace(data, @"<%.*?%>", new MatchEvaluator(RefineCalls2), RegexOptions.Singleline);
            sb.Append(data);
            sb.Append("\");");
            return sb.ToString();*/

            // First replace string versions

            //StringBuilder sb = new StringBuilder();
            string substring;
            int lastStartPos = 0;
            int startTagPos = data.IndexOf("<%");
            int endPos = 0;
            sb.Append("sb.Append(@\"");
            while (startTagPos != -1)
            {
                endPos = data.IndexOf("%>", startTagPos);

                substring = data.Substring(lastStartPos, startTagPos - lastStartPos);
                string data2 = substring.Replace("\"", "\"\"");
                sb.Append(data2);

                //1. Is it an echo?
                if (data[startTagPos + 2] == '=')
                {
                    sb.Append("\");\r\n");
                    sb.Append("sb.Append(");
                    //sb.Append("sb.Append(;\r\n";
                    sb.Append(data.Substring(startTagPos + 3, endPos - startTagPos - 3));
                    //sb.Append("\");\r\n sb.Append(@\"");
                    sb.Append(");\r\n sb.Append(@\"");
                }
                else
                {
                    //1. Copy everything before our pos
                    sb.Append("\");\r\n");
                    //substring = ;
                    //data2 = substring.Replace("\"", "\"\"");
                    data2 = data.Substring(startTagPos + 2, endPos - startTagPos - 2);
                    sb.Append(data2);
                    sb.Append("\r\nsb.Append(@\"");
                }

                lastStartPos = endPos + 2;
                startTagPos = data.IndexOf("<%", endPos);
            }
            sb.Append(data.Substring(lastStartPos).Replace("\"", "\"\""));
            sb.Append("\");\r\n");
            //sb0.Append(sb);
            return sb.ToString();
        }

        private string RefineCalls(Match m)
        {
            // Get the matched string.
            string x = m.ToString();
            // If the first char is lower case...
            x = Regex.Replace(x, "<%=", "\");\r\nsb.Append(");
            x = Regex.Replace(x, "%>", ");\r\nsb.Append(@\"");

            return x;
        }
        private string RefineCalls2(Match m)
        {
            // Get the matched string.
            string x = m.ToString();
            // If the first char is lower case...
            x = Regex.Replace(x, "<%", "\");\r\n");
            x = Regex.Replace(x, "%>", "sb.Append(@\"");

            return x;
        }
    }
}
