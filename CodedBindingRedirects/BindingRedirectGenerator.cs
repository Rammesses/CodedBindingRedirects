using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Text;

namespace CodedBindingRedirects
{
    [Generator]
    public class BindingRedirectGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // begin creating the source we'll inject into the users compilation
            var sourceBuilder = new StringBuilder(@"
using System;
namespace CodedBindingRedirects
{
    public static class BindingRedirects
    {
        public static void Apply() 
        {
            Console.WriteLine(""Hello from generated code!"");
");

            sourceBuilder.AppendLine(@"Console.WriteLine(""\nInfo:"");");
            sourceBuilder.AppendLine($@"Console.WriteLine(@"" - Assembly = {context.Compilation.Assembly}"");");

            sourceBuilder.AppendLine(@"Console.WriteLine(""\nSyntax Trees:"");");

            var syntaxTrees = context.Compilation.SyntaxTrees;
            foreach (SyntaxTree tree in syntaxTrees)
            {
                sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {tree.FilePath}"");");
            }

            sourceBuilder.AppendLine(@"Console.WriteLine(""\nAdditional Files:"");");

            var additionalFiles = context.AdditionalFiles;
            foreach (AdditionalText file in additionalFiles)
            {
                sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {file.Path}"");");
            }

            // finish creating the source to inject
            sourceBuilder.AppendLine(@"
        }
    }
}");

            // inject the created source into the users compilation
            context.AddSource("CodedBindingRedirects.Generated.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
