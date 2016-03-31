using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGeneration
{
    public class OfferFriendlyInteropOverloadsGenerator : ICodeGenerator
    {
        private static readonly TypeSyntax IntPtrTypeSyntax = SyntaxFactory.ParseTypeName("IntPtr");

        /// <summary>
        /// Initializes a new instance of the <see cref="OfferFriendlyInteropOverloadsGenerator"/> class.
        /// </summary>
        /// <param name="data">Generator attribute data.</param>
        public OfferFriendlyInteropOverloadsGenerator(AttributeData data)
        {
        }

        public async Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(MemberDeclarationSyntax applyTo, Document document, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var type = (ClassDeclarationSyntax)applyTo;
            var generatedType = type
                .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>())
                .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());
            foreach (var method in type.Members.OfType<MethodDeclarationSyntax>())
            {
                var marshaledParameters = from p in method.ParameterList.Parameters
                                          let marshalAttribute = p.AttributeLists.SelectMany(al => al.Attributes).FirstOrDefault(a => (a.Name as SimpleNameSyntax)?.Identifier.ValueText == "CustomMarshaler")
                                          where marshalAttribute != null
                                          let customMarshalerExpression = (TypeOfExpressionSyntax)marshalAttribute.ArgumentList.Arguments[0].Expression
                                          let customMarshaler = customMarshalerExpression.Type
                                          let friendlyTypeExpression = (TypeOfExpressionSyntax)marshalAttribute.ArgumentList.Arguments[1].Expression
                                          let friendlyType = friendlyTypeExpression.Type
                                          select new MarshaledParameter(p, customMarshaler, friendlyType);
                if (marshaledParameters.Any())
                {
                    var wrapperMethod = method
                        .WithModifiers(RemoveModifier(method.Modifiers, SyntaxKind.ExternKeyword, SyntaxKind.PrivateKeyword).Insert(0, SyntaxFactory.Token(SyntaxKind.InternalKeyword)))
                        .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>())
                        .WithLeadingTrivia(method.GetLeadingTrivia().Where(t => !t.IsDirective))
                        .WithTrailingTrivia(method.GetTrailingTrivia().Where(t => !t.IsDirective))
                        .WithParameterList(CreateParameterListFor(method, marshaledParameters))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithExpressionBody(null)
                        .WithBody(CreateBodyFor(method, marshaledParameters));

                    generatedType = generatedType.AddMembers(wrapperMethod);
                }
            }

            return SyntaxFactory.List<MemberDeclarationSyntax>().Add(generatedType);
        }

        private static SyntaxTokenList RemoveModifier(SyntaxTokenList list, params SyntaxKind[] modifiers)
        {
            return SyntaxFactory.TokenList(list.Where(t => Array.IndexOf(modifiers, t.Kind()) < 0));
        }

        private static readonly TypeSyntax ICustomMarshalerTypeSyntax = SyntaxFactory.ParseTypeName("ICustomMarshaler");

        private static ArgumentSyntax ForwardParameter(ParameterSyntax parameter)
        {
            var refOrOut = parameter.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.RefKeyword) || m.IsKind(SyntaxKind.OutKeyword));
            var arg = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Identifier))
                .WithRefOrOutKeyword(refOrOut);
            return arg;
        }

        private ParameterListSyntax CreateParameterListFor(MethodDeclarationSyntax innerMethod, IEnumerable<MarshaledParameter> marshaledParameters)
        {
            var modifiedParameterList = SyntaxFactory.ParameterList();

            foreach (var p in innerMethod.ParameterList.Parameters)
            {
                var marshaledInfo = marshaledParameters.FirstOrDefault(m => m.OriginalParameter == p);
                var modifiedParameter = p
                    .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>())
                    .WithType(marshaledInfo.FriendlyType ?? p.Type);
                modifiedParameterList = modifiedParameterList.AddParameters(modifiedParameter);
            }

            return modifiedParameterList;
        }

        private BlockSyntax CreateBodyFor(MethodDeclarationSyntax innerMethod, IEnumerable<MarshaledParameter> marshaledParameters)
        {
            var body = SyntaxFactory.Block();
            var finallyBlock = SyntaxFactory.Block();
            var argsByParameter = innerMethod.ParameterList.Parameters.ToDictionary(
                p => p,
                p => ForwardParameter(p));

            var marshalersCreated = new HashSet<string>();
            var marshalerTypes = from marshaler in marshaledParameters
                                 group marshaler by marshaler.MarshalerType into types
                                 select types;
            foreach (var type in marshalerTypes)
            {
                var marshalerLocalName = $"_{type.Key}";
                if (marshalersCreated.Add(marshalerLocalName))
                {
                    body = body.AddStatements(
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(ICustomMarshalerTypeSyntax)
                                .AddVariables(SyntaxFactory.VariableDeclarator(marshalerLocalName)
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                type.Key,
                                                SyntaxFactory.IdentifierName("GetInstance")),
                                            SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""))))))))));
                }

                body = body.AddStatements(
                    type.Select(p =>
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(IntPtrTypeSyntax)
                                .AddVariables(SyntaxFactory.VariableDeclarator($"_{p.OriginalParameter.Identifier.ValueText}")
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(marshalerLocalName),
                                                SyntaxFactory.IdentifierName("MarshalManagedToNative")))
                                        .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.OriginalParameter.Identifier)))))))).ToArray());
                finallyBlock = finallyBlock.AddStatements(
                    type.Select(p =>
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(marshalerLocalName),
                                SyntaxFactory.IdentifierName("CleanUpNativeData")))
                        .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"_{p.OriginalParameter.Identifier.ValueText}"))))).ToArray());
                foreach (var p in type)
                {
                    argsByParameter[p.OriginalParameter] = argsByParameter[p.OriginalParameter]
                        .WithExpression(
                            SyntaxFactory.CastExpression(
                                p.OriginalParameter.Type,
                                SyntaxFactory.IdentifierName($"_{p.OriginalParameter.Identifier.ValueText}")));
                }
            }

            var args = SyntaxFactory.ArgumentList().AddArguments(
                (from p in innerMethod.ParameterList.Parameters
                 select argsByParameter[p]).ToArray());
            var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(innerMethod.Identifier),
                args);
            var tryBlock = SyntaxFactory.Block();
            if (innerMethod.ReturnType != null && (innerMethod.ReturnType as PredefinedTypeSyntax)?.Keyword.Kind() != SyntaxKind.VoidKeyword)
            {
                tryBlock = tryBlock.AddStatements(SyntaxFactory.ReturnStatement(invocation));
            }
            else
            {
                tryBlock = tryBlock.AddStatements(SyntaxFactory.ExpressionStatement(invocation));
            }

            body = body.AddStatements(SyntaxFactory.TryStatement(
                tryBlock,
                SyntaxFactory.List<CatchClauseSyntax>(),
                SyntaxFactory.FinallyClause(finallyBlock)));

            return body;
        }

        private struct MarshaledParameter
        {
            internal MarshaledParameter(ParameterSyntax originalParameter, TypeSyntax marshalerType, TypeSyntax friendlyType)
            {
                this.OriginalParameter = originalParameter;
                this.MarshalerType = marshalerType;
                this.FriendlyType = friendlyType;
            }

            internal ParameterSyntax OriginalParameter { get; }

            internal TypeSyntax MarshalerType { get; }

            internal TypeSyntax FriendlyType { get; }
        }
    }
}
