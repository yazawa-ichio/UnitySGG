using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitySGGTest
{

	[Generator]
	public class UnitySGGTestGenerator : ISourceGenerator
	{
		private const string AttributeText = @"
using System;
namespace UnitySGGTest
{
	[AttributeUsage(AttributeTargets.Enum)]
	sealed class FastEnumAttribute : Attribute
	{
		public FastEnumAttribute()
		{
		}
	}
}
";
		public void Execute(GeneratorExecutionContext context)
		{
			context.AddSource($"FastEnumAttribute.g.cs", AttributeText);

			try
			{
				SyntaxReceiver receiver = context.SyntaxReceiver as SyntaxReceiver;

				foreach (var node in receiver.List)
				{
					SemanticModel semanticModel = context.Compilation.GetSemanticModel(node.SyntaxTree);
					ISymbol symbol = semanticModel.GetDeclaredSymbol(node, context.CancellationToken);
					StringBuilder builder = new($@"");
					builder.AppendLine($"static class {symbol.Name}FastEnum");
					builder.AppendLine($"{{");
					builder.AppendLine($"    public static string ToFastString(this {symbol.Name} self)");
					builder.AppendLine($"    {{");
					builder.AppendLine($"        switch(self)");
					builder.AppendLine($"        {{");
					foreach (var member in node.Members)
					{
						ISymbol memberSymbol = semanticModel.GetDeclaredSymbol(member, context.CancellationToken);
						builder.AppendLine($"            case {symbol.Name}.{memberSymbol.Name}: return \"{memberSymbol.Name}\";");
					}
					builder.AppendLine($"        }}");
					builder.AppendLine($"        return \"\";");
					builder.AppendLine($"    }}");
					builder.AppendLine($"}}");
					context.AddSource($"{symbol.Name}.g.cs", builder.ToString());
				}
			}
			catch (System.Exception e)
			{
				context.AddSource($"Error.g.cs", e.ToString());
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}
	}

	class SyntaxReceiver : ISyntaxReceiver
	{
		public List<EnumDeclarationSyntax> List { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is EnumDeclarationSyntax declarationSyntax)
			{
				var isTarget = declarationSyntax.AttributeLists
					.SelectMany(attributeList => attributeList.Attributes)
					.Any(x => IsTarget(x));
				if (isTarget)
				{
					List.Add(declarationSyntax);
				}
			}

			bool IsTarget(AttributeSyntax attribute)
			{
				switch (attribute.Name.ToFullString().Trim())
				{
					case "UnitySGGTest.FastEnum":
					case "FastEnum":
					case "UnitySGGTest.FastEnumAttribute":
					case "FastEnumAttribute":
						return true;
					default:
						return false;
				}
			}
		}
	}
}