using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace CodedBindingRedirects
{
    [Generator]
    public class BindingRedirectGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var assemblyIdentity = context.Compilation.Assembly.Identity;
            var codeNamespaces = context.Compilation.Assembly.NamespaceNames;

            var analyserConfigOptions = context.AnalyzerConfigOptions;
            var globalOptions = context.AnalyzerConfigOptions.GlobalOptions;

            // Add our custom AutoGenerateBindingRedirectAttribute class
            context.AddSource("AutoGenerateBindingRedirectAttribute.generated.cs",
                SourceText.From($@"
using System;

namespace CodedBindingRedirects
{{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AutoGenerateBindingRedirectAttribute : Attribute
    {{
    }}
}}
                ", Encoding.UTF8));

            // now create the source we'll inject into the users compilation
            var sourceBuilder = new StringBuilder(@"
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
");

           sourceBuilder.AppendLine($"namespace CodedBindingRedirects");
            sourceBuilder.Append(@"
{
    public static class BindingRedirects
    {"
            );

            sourceBuilder.AppendLine(@"
        public static void RedirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
        {
            Trace.WriteLine($""CodedBindingRedirects: Will redirect {shortName} ({publicKeyToken}) to {targetVersion}..."");

            ResolveEventHandler handler = null;

            handler = (sender, args) => {
                // Use latest strong name & version when trying to load SDK assemblies
                var requestedAssembly = new AssemblyName(args.Name);
                if (requestedAssembly.Name != shortName) return null;

                Trace.WriteLine($""CodedBindingRedirects: Redirecting assembly load of { args.Name},\tloaded by { (args.RequestingAssembly == null ? ""(unknown)"" : args.RequestingAssembly.FullName)}"");

                requestedAssembly.Version = targetVersion;
                requestedAssembly.SetPublicKeyToken(new AssemblyName(""x, PublicKeyToken="" + publicKeyToken).GetPublicKeyToken());
                requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;

                AppDomain.CurrentDomain.AssemblyResolve -= handler;

                return Assembly.Load(requestedAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;
        }
            ");

            sourceBuilder.AppendLine(@"
            public static void Apply() 
        {
            ");

            var syntaxTrees = context.Compilation.SyntaxTrees;
            var fileFolders = syntaxTrees.Select(t => Path.GetDirectoryName(t.FilePath)).Distinct().OrderBy(t => t.Length);
            foreach (var folder in fileFolders)
            {
                var appConfigPath = Path.Combine(folder, "app.config");
                if (File.Exists(appConfigPath))
                {
                    sourceBuilder.Append(AddBindingRedirectsFor(appConfigPath));
                    break;
                }
            }

            sourceBuilder.AppendLine("        }");

            // finish creating the source to inject
            sourceBuilder.AppendLine(@"
    }
            }");

            // inject the created source into the users compilation
            context.AddSource("CodedBindingRedirects.Generated.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private string AddBindingRedirectsFor(string appConfigPath)
        {
            var builder = new StringBuilder();
            builder.AppendLine($@"            Trace.WriteLine(@""CodedBindingRedirects: Applying binding redirects found in {appConfigPath}"");");

            using (var fileStream = new FileStream(appConfigPath, FileMode.Open))
            {
                var configDoc = new XmlDocument();
                configDoc.Load(fileStream);

                var nsManager = new XmlNamespaceManager(configDoc.NameTable);
                nsManager.AddNamespace("asm", "urn:schemas-microsoft-com:asm.v1");

                var redirects = configDoc.SelectNodes(@"/configuration/runtime/asm:assemblyBinding/asm:dependentAssembly", nsManager);
                foreach (XmlNode node in redirects)
                {
                    var assembly = node.SelectSingleNode("asm:assemblyIdentity", nsManager)?.Attributes["name"]?.Value;
                    var publicKeyToken = node.SelectSingleNode("asm:assemblyIdentity", nsManager)?.Attributes["publicKeyToken"]?.Value;
                    var newVersion = node.SelectSingleNode("asm:bindingRedirect", nsManager)?.Attributes["newVersion"]?.Value;

                    if (!string.IsNullOrEmpty(assembly) &&
                        !string.IsNullOrEmpty(publicKeyToken) &&
                        !string.IsNullOrEmpty(newVersion))
                    {
                        builder.AppendLine($@"            RedirectAssembly(""{assembly}"", new Version(""{newVersion}""), ""{publicKeyToken}"");");
                    }
                }
            }

            return builder.ToString();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
