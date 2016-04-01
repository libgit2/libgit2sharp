using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.FormattableString;

namespace CodeGeneration
{
    public class OfferFriendlyInteropOverloadsGenerator : ICodeGenerator
    {
        private static readonly TypeSyntax IntPtrTypeSyntax = SyntaxFactory.ParseTypeName("IntPtr");
        private static readonly IdentifierNameSyntax resultLocal = SyntaxFactory.IdentifierName("_result");
        private static readonly ExpressionSyntax IntPtrZeroExpressionSyntax = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IntPtrTypeSyntax,
            SyntaxFactory.IdentifierName(nameof(IntPtr.Zero)));

        /// <summary>
        /// Initializes a new instance of the <see cref="OfferFriendlyInteropOverloadsGenerator"/> class.
        /// </summary>
        /// <param name="data">Generator attribute data.</param>
        public OfferFriendlyInteropOverloadsGenerator(AttributeData data)
        {
        }

        public async Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(MemberDeclarationSyntax applyTo, Document document, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            Func<ParameterSyntax, AttributeListSyntax, MarshaledParameter> findMarshalAttribute = (p, al) =>
            {
                var marshalAttribute = al.Attributes.FirstOrDefault(a => (a.Name as SimpleNameSyntax)?.Identifier.ValueText == "CustomMarshaler");
                if (marshalAttribute == null)
                {
                    return default(MarshaledParameter);
                }

                var customMarshalerExpression = (TypeOfExpressionSyntax)marshalAttribute.ArgumentList.Arguments[0].Expression;
                var customMarshaler = customMarshalerExpression.Type;
                var friendlyTypeExpression = (TypeOfExpressionSyntax)marshalAttribute.ArgumentList.Arguments[1].Expression;
                var friendlyType = friendlyTypeExpression.Type;
                return new MarshaledParameter(p, customMarshaler, friendlyType);
            };

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var type = (ClassDeclarationSyntax)applyTo;
            var generatedType = type
                .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>())
                .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());
            foreach (var method in type.Members.OfType<MethodDeclarationSyntax>())
            {
                var marshaledParameters = from p in method.ParameterList.Parameters
                                          from al in p.AttributeLists
                                          let a = findMarshalAttribute(p, al)
                                          where a.OriginalParameter != null
                                          select a;
                var marshaledResult = from al in method.AttributeLists
                                      where al.Target?.Identifier.IsKind(SyntaxKind.ReturnKeyword) ?? false
                                      let a = findMarshalAttribute(null, al)
                                      where a.FriendlyType != null
                                      select a;
                if (marshaledParameters.Any() || marshaledResult.Any())
                {
                    var wrapperMethod = method
                        .WithModifiers(RemoveModifier(method.Modifiers, SyntaxKind.ExternKeyword, SyntaxKind.PrivateKeyword).Insert(0, SyntaxFactory.Token(SyntaxKind.InternalKeyword)))
                        .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>())
                        .WithLeadingTrivia(method.GetLeadingTrivia().Where(t => !t.IsDirective))
                        .WithTrailingTrivia(method.GetTrailingTrivia().Where(t => !t.IsDirective))
                        .WithParameterList(CreateParameterListFor(method, marshaledParameters))
                        .WithReturnType(marshaledResult.FirstOrDefault().FriendlyType ?? method.ReturnType)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithExpressionBody(null)
                        .WithBody(CreateBodyFor(method, marshaledParameters, marshaledResult.FirstOrDefault()));

                    if (!marshaledParameters.Any())
                    {
                        wrapperMethod = wrapperMethod
                            .WithIdentifier(SyntaxFactory.Identifier(wrapperMethod.Identifier.ValueText.TrimEnd('_')));
                    }

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

        private BlockSyntax CreateBodyFor(MethodDeclarationSyntax innerMethod, IEnumerable<MarshaledParameter> marshaledParameters, MarshaledParameter marshaledResult)
        {
            var body = SyntaxFactory.Block();
            var finallyBlock = SyntaxFactory.Block();
            var argsByParameter = innerMethod.ParameterList.Parameters.ToDictionary(
                p => p,
                p => ForwardParameter(p));
            var marshalerInitializers = new List<StatementSyntax>();
            var inputMarshaling = new List<StatementSyntax>();
            var outputMarshaling = new List<StatementSyntax>();

            var marshalersCreated = new HashSet<string>();
            Func<TypeSyntax, string> acquireMarshaler = type =>
            {
                var marshalerLocalName = Invariant($"_{type}");
                if (marshalersCreated.Add(marshalerLocalName))
                {
                    marshalerInitializers.Add(
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(ICustomMarshalerTypeSyntax)
                                .AddVariables(SyntaxFactory.VariableDeclarator(marshalerLocalName)
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                type,
                                                SyntaxFactory.IdentifierName("GetInstance")),
                                            SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""))))))))));
                }

                return marshalerLocalName;
            };

            foreach (var parameter in marshaledParameters)
            {
                string marshalerLocalName = acquireMarshaler(parameter.MarshalerType);
                var isOutParameter = parameter.OriginalParameter.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword));
                TypeSyntax localVarType = isOutParameter ? parameter.OriginalParameter.Type : IntPtrTypeSyntax;
                ExpressionSyntax initialValue = isOutParameter
                    ? (ExpressionSyntax)SyntaxFactory.DefaultExpression(parameter.OriginalParameter.Type)
                    : SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(marshalerLocalName),
                            SyntaxFactory.IdentifierName(nameof(ICustomMarshaler.MarshalManagedToNative))))
                        .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.OriginalParameter.Identifier)));
                var localVarIdentifier = SyntaxFactory.IdentifierName(Invariant($"_{parameter.OriginalParameter.Identifier.ValueText}"));
                inputMarshaling.Add(
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(localVarType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(localVarIdentifier.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(initialValue)))));

                argsByParameter[parameter.OriginalParameter] = argsByParameter[parameter.OriginalParameter]
                    .WithExpression(
                        isOutParameter
                            ? (ExpressionSyntax)localVarIdentifier
                            : SyntaxFactory.CastExpression(parameter.OriginalParameter.Type, localVarIdentifier));

                if (isOutParameter)
                {
                    outputMarshaling.Add(
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(parameter.OriginalParameter.Identifier),
                            SyntaxFactory.CastExpression(
                                parameter.FriendlyType,
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(marshalerLocalName),
                                        SyntaxFactory.IdentifierName(nameof(ICustomMarshaler.MarshalNativeToManaged))),
                                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(
                                        SyntaxFactory.ObjectCreationExpression(
                                            IntPtrTypeSyntax,
                                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(localVarIdentifier))),
                                            null)))))))));
                }

                var cleanUpExpression = isOutParameter
                    ? (ExpressionSyntax)SyntaxFactory.ObjectCreationExpression(IntPtrTypeSyntax).AddArgumentListArguments(
                        SyntaxFactory.Argument(localVarIdentifier))
                    : localVarIdentifier;
                finallyBlock = finallyBlock.AddStatements(
                    SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(marshalerLocalName),
                            SyntaxFactory.IdentifierName(nameof(ICustomMarshaler.CleanUpNativeData))))
                    .AddArgumentListArguments(SyntaxFactory.Argument(cleanUpExpression))));
            }

            var args = SyntaxFactory.ArgumentList().AddArguments(
                (from p in innerMethod.ParameterList.Parameters
                 select argsByParameter[p]).ToArray());
            var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(innerMethod.Identifier),
                args);
            StatementSyntax invocationStatement;
            StatementSyntax returnStatement = null;
            if (innerMethod.ReturnType != null && (innerMethod.ReturnType as PredefinedTypeSyntax)?.Keyword.Kind() != SyntaxKind.VoidKeyword)
            {
                if (marshaledResult.MarshalerType != null)
                {
                    string marshalerLocalName = acquireMarshaler(marshaledResult.MarshalerType);
                    inputMarshaling.Add(
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(IntPtrTypeSyntax)
                            .AddVariables(SyntaxFactory.VariableDeclarator(resultLocal.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(IntPtrZeroExpressionSyntax)))));

                    var intPtrResultExpression = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        resultLocal,
                        SyntaxFactory.ObjectCreationExpression(
                            IntPtrTypeSyntax,
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(invocation))),
                            null));
                    var castToManagedExpression = SyntaxFactory.CastExpression(
                        marshaledResult.FriendlyType,
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(marshalerLocalName),
                                SyntaxFactory.IdentifierName(nameof(ICustomMarshaler.MarshalNativeToManaged))),
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(resultLocal)))));

                    invocationStatement = SyntaxFactory.ExpressionStatement(intPtrResultExpression);
                    returnStatement = SyntaxFactory.ReturnStatement(castToManagedExpression);

                    finallyBlock = finallyBlock.AddStatements(
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(marshalerLocalName),
                                SyntaxFactory.IdentifierName(nameof(ICustomMarshaler.CleanUpNativeData))))
                        .AddArgumentListArguments(SyntaxFactory.Argument(resultLocal))));
                }
                else
                {
                    invocationStatement = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(innerMethod.ReturnType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(resultLocal.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(invocation))));
                    returnStatement = SyntaxFactory.ReturnStatement(resultLocal);
                }
            }
            else
            {
                invocationStatement = SyntaxFactory.ExpressionStatement(invocation);
            }

            var tryBlock = SyntaxFactory.Block()
                .AddStatements(invocationStatement)
                .AddStatements(outputMarshaling.ToArray());

            if (returnStatement != null)
            {
                tryBlock = tryBlock
                    .AddStatements(returnStatement);
            }

            body = body
                .AddStatements(marshalerInitializers.ToArray())
                .AddStatements(inputMarshaling.ToArray())
                .AddStatements(SyntaxFactory.TryStatement(tryBlock, SyntaxFactory.List<CatchClauseSyntax>(), SyntaxFactory.FinallyClause(finallyBlock)));

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
