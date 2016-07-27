using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FRC_Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SubsystemDefaultCommandConstructorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SubsystemDefaultCommandConstructor";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.DefaultCommandConstructorTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.DefaultCommandConstructorMessage), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.DefaultCommandConstructorDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Correctness";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var subsystemType = context.Symbol as INamedTypeSymbol;
            var attributes = subsystemType.GetAttributes();
            if (attributes.Length == 0) return;
            else
            {
                var exportSubsystemAttribute = attributes.FirstOrDefault(attribute => attribute.AttributeClass.Name == "ExportSubsystemAttribute");
                if (exportSubsystemAttribute == null) return;
                var defaultCommandType = exportSubsystemAttribute.NamedArguments
                    .FirstOrDefault(parameter => parameter.Key == "DefaultCommandType").Value.Value as INamedTypeSymbol;
                if (defaultCommandType == null) return;
                var subsystemConstructor = defaultCommandType.InstanceConstructors.FirstOrDefault(methodSymbol => methodSymbol.Parameters.Length == 1 && methodSymbol.Parameters[0].Type == subsystemType);
                if (subsystemConstructor == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, exportSubsystemAttribute.ApplicationSyntaxReference.GetSyntax().GetLocation()));
                }
            }
        }
    }
}
