using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editing;

namespace FRC_Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SubsystemDefaultCommandConstructorFixer)), Shared]
    public class SubsystemDefaultCommandConstructorFixer : CodeFixProvider
    {
        private const string title = "Make uppercase";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SubsystemDefaultCommandConstructorAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddConstructorToCommandType(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> AddConstructorToCommandType(Document document, TypeDeclarationSyntax subsystemTypeDeclaration, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(subsystemTypeDeclaration, cancellationToken);
            
            var commandTypeSymbol = typeSymbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass.Name == "ExportSubsystemAttribute")?.NamedArguments
                .FirstOrDefault(parameter => parameter.Key == "DefaultCommandType").Value.Value as INamedTypeSymbol;
            var commandTypeSyntax = (await commandTypeSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken));
            var commandDocument = document.Project.GetDocument(commandTypeSyntax.SyntaxTree);
            var editor = await DocumentEditor.CreateAsync(commandDocument, cancellationToken);

            var generator = editor.Generator;

            var classSyntax = generator.GetDeclaration(commandTypeSyntax, DeclarationKind.Class);
            var subsystemParameter = generator.ParameterDeclaration("subsystem", generator.TypeExpression(semanticModel.GetDeclaredSymbol(subsystemTypeDeclaration)));
            var constructor = generator.ConstructorDeclaration(commandTypeSymbol.Name, new[] { subsystemParameter }, Accessibility.Public) as ConstructorDeclarationSyntax;

            editor.AddMember(commandTypeSyntax, constructor);

            return editor.GetChangedDocument();
        }
    }
};