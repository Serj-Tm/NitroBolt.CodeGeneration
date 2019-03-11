using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using s = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using NitroBolt.Functional;

namespace NitroBolt.CodeGeneration
{
    public class ImmutableGenerator
    {
        //http://roslynquoter.azurewebsites.net/

        public static string Generate(string code)
        {
            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);

                var compilation = CSharpCompilation.Create("data")
                  .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                  .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                  .AddReferences(MetadataReference.CreateFromFile(typeof(System.Collections.Immutable.ImmutableArray).Assembly.Location))
                  .AddSyntaxTrees(tree);

                var model = compilation.GetSemanticModel(tree);


                var @namespace = tree.GetCompilationUnitRoot().Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();


                var usings = tree.GetCompilationUnitRoot().Usings.ToArray();


                var r = s.CompilationUnit()
                  .AddUsings
                  (
                    new[] { "System" },
                    new[] { "System", "Linq" },
                    new[] { "System", "Collections", "Generic" },
                    new[] { "System", "Collections", "Immutable" },
                    new[] { "NitroBolt", "Functional" },
                    new[] { "NitroBolt", "Immutable" }
                  )
                  .AddUsings(usings);


                var isAdded = false;

                if (@namespace != null)
                {
                    var resultMembers = new List<MemberDeclarationSyntax>();
                    foreach (var @class in @namespace.Members.OfType<ClassDeclarationSyntax>())
                    {

                        var members = GetMembersForGeneration(@class, model);

                        if (!members.OrEmpty().Any())
                            continue;

                        resultMembers.Add(GenerateConstructorAndWithMethod(@class.Identifier.ValueText, members));

                        var byHelper = GenerateByMethod(@class.Identifier.ValueText, members);
                        if (byHelper != null)
                            resultMembers.Add(byHelper);
                    }

                    resultMembers.AddRange(InterfaceToClassGenerator.GenerateClasses(@namespace));

                    var resultNamespace = s.NamespaceDeclaration(@namespace.Name).AddMembers(resultMembers.ToArray());
                    r = r.AddMembers(resultNamespace);

                    isAdded = isAdded || resultMembers.Any();
                }
                if (!isAdded)
                    return null;
                return r.NormalizeWhitespace().ToString();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                return null;
            }
        }

        public static Member[] GetMembersForGeneration(ClassDeclarationSyntax @class, SemanticModel model)
        {
            if (@class.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword) || @class.Modifiers.All(modifier => modifier.Kind() != SyntaxKind.PartialKeyword))
                return null;

            if (MetaLiteral(@class.AttributeLists) == "skip")
                return null;

            return @class.Members
              .Select(member => ToMember(member, model)).Where(member => member != null).ToArray();
        }

        public static ClassDeclarationSyntax GenerateConstructorAndWithMethod(string name, Member[] members)
        {
            var resultClass = s.ClassDeclaration(name)
                 .AddModifiers(s.Token(SyntaxKind.PartialKeyword));

            var parameters = members.Where(member => member.IsParameter).ToArray();

            var lastParameterI = parameters.Select((member, i) => (member.ParameterTypeKind == ValueKind.Value || member.ParameterTypeKind == ValueKind.NotNullable) ? (i + 1) : (int?)null).Max() ?? 0;

            var constructor = s.ConstructorDeclaration(name)
              .AddModifiers(s.Token(SyntaxKind.PublicKeyword))
              .AddParameterListParameters
              (
                parameters.Select((member, i) =>
                  s.Parameter(member.ParameterIdentifier)
                   .WithType(member.ParameterType)
                   .WithDefault(member.ParameterTypeKind != ValueKind.Value && i >= lastParameterI ? s.EqualsValueClause(s.LiteralExpression(SyntaxKind.NullLiteralExpression)) : null)
                )
                .ToArray()
              )
              .WithBody(s.Block(members
                  .Select(member => s.ExpressionStatement(s.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, ThisIdentifier(member),
                    !member.IsParameter
                    ? member.Initializer
                    : (member.InitializerF
                       ?? (member.Initializer != null ? new Func<ExpressionSyntax, ExpressionSyntax>(p => s.BinaryExpression(SyntaxKind.CoalesceExpression, p, ThisIdentifier(member))) : null)
                       ?? (p => p)
                      )
                        (s.IdentifierName(member.ParameterIdentifier))

                  )))
              ));

            var with = s.MethodDeclaration(s.IdentifierName(name), "With")
              .AddModifiers(s.Token(SyntaxKind.PublicKeyword))
              .AddParameterListParameters
              (
                members.Select(member =>
                  s.Parameter(member.ParameterIdentifier)
                   .WithType(member.OptionType)
                   .WithDefault(s.EqualsValueClause(s.LiteralExpression(SyntaxKind.NullLiteralExpression)))
                )
                .ToArray()
              )
              .WithBody(s.Block(
                s.ReturnStatement(s.ObjectCreationExpression(s.IdentifierName(name))
                  .AddArgumentListArguments
                  (
                    members
                      .Where(member => member.IsOption)
                      .Select(member => s.Argument(
                         member.OptionValueKind == ValueKind.Option
                         ? s.InvocationExpression(s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.IdentifierName(member.ParameterIdentifier), s.IdentifierName("Else")))
                            .AddArgumentListArguments(s.Argument(ThisIdentifier(member)))
                         : (ExpressionSyntax)s.BinaryExpression(SyntaxKind.CoalesceExpression, s.IdentifierName(member.ParameterIdentifier), ThisIdentifier(member))
                      ))
                      .ToArray()
                  )
                )
              ));

            var resultMembers = new List<MemberDeclarationSyntax> {constructor, with};

            resultMembers.AddRange(members.Where(_member => IsArray(_member.Type))
                .Select(member => 
                    s.MethodDeclaration(s.IdentifierName(name), "With_" + member.Identifier.Text)
                      .AddModifiers(s.Token(SyntaxKind.PublicKeyword))
                      .AddParameterListParameters
                      (
                        s.Parameter(member.ParameterIdentifier)
                            .WithType(member.Type)
                            .AddModifiers(s.Token(SyntaxKind.ParamsKeyword))
                      )
                      .WithBody(s.Block(s.ReturnStatement
                      (
                          s.InvocationExpression(s.IdentifierName("With"))
                          .AddArgumentListArguments(s.Argument(s.IdentifierName(member.ParameterIdentifier)).WithNameColon(s.NameColon(s.IdentifierName(member.ParameterIdentifier))))))
                      )
                )
            );

            return resultClass.AddMembers(resultMembers.ToArray());
        }
        static ExpressionSyntax ThisIdentifier(Member member)
        {
            if (member.Identifier.ValueText != member.ParameterIdentifier.ValueText)
                return s.IdentifierName(member.Identifier);
            else
                return s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.ThisExpression(), s.IdentifierName(member.Identifier));
        }

        //static T ByMethod_Pattern<T, TValue>(this IEnumerable<T> items, Func<T, TValue> f, Option<TValue> v = null)
        //  where T : class
        //  where TValue : class
        //{
        //  if (v != null)
        //    return items.FirstOrDefault(item => f(item) == v.Value);

        //  return null;
        //}

        //static dynamic QQ(params dynamic[] args) { return null; }

        private static ClassDeclarationSyntax GenerateByMethod(string name, Member[] members)
        {
            var resultClass = s.ClassDeclaration(name + "Helper")
                 .AddModifiers(s.Token(SyntaxKind.PublicKeyword), s.Token(SyntaxKind.StaticKeyword), s.Token(SyntaxKind.PartialKeyword));

            var _members = members.Where(member => !IsCollection(member.Type)).ToArray();
            if (!_members.Any())
                return null;

            var by = s.MethodDeclaration(s.IdentifierName(name), "By")
              .AddParameterListParameters(s.Parameter(s.Identifier("items"))
                .WithType(s.GenericName("IEnumerable").AddTypeArgumentListArguments(s.IdentifierName(name)))
                .AddModifiers(s.Token(SyntaxKind.ThisKeyword))
              )
              .AddParameterListParameters
              (
                _members.Select(member =>
                  s.Parameter(member.ParameterIdentifier)
                   .WithType(member.OptionType)
                   .WithDefault(s.EqualsValueClause(s.LiteralExpression(SyntaxKind.NullLiteralExpression)))
                )
                .ToArray()
              )
              .AddModifiers(s.Token(SyntaxKind.PublicKeyword), s.Token(SyntaxKind.StaticKeyword))
              .WithBody(s.Block(
                  _members.Select(member =>
                    s.IfStatement
                    (
                      s.BinaryExpression(SyntaxKind.NotEqualsExpression, s.IdentifierName(member.ParameterIdentifier), s.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                      s.ReturnStatement
                      (
                         s.InvocationExpression(s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.IdentifierName("items"), s.IdentifierName("FirstOrDefault")))
                         .AddArgumentListArguments
                         (
                           s.Argument
                           (
                             s.SimpleLambdaExpression
                             (
                               s.Parameter(s.Identifier("_item")),
                               s.BinaryExpression
                               (
                                 SyntaxKind.EqualsExpression,
                                 s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.IdentifierName("_item"), s.IdentifierName(member.Identifier)),
                                 s.IdentifierName(member.ParameterIdentifier)
                                  .Wrap(_ => member.OptionValueKind == ValueKind.Option ? s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, _, s.IdentifierName("Value")) : (ExpressionSyntax)_)
                               )
                             )
                           )
                         )
                      )
                    )
                  )

                ).AddStatements(s.ReturnStatement(s.LiteralExpression(SyntaxKind.NullLiteralExpression)))
              );

            resultClass = resultClass.AddMembers(by);

            return resultClass;
        }


        static TypeSyntax ArrayElementType(TypeSyntax type)
        {
            if (type is ArrayTypeSyntax)
                return ((ArrayTypeSyntax)type).ElementType;
            return null;
        }
        static bool IsCollection(TypeSyntax type)
        {
            if (IsArray(type))
                return true;
            if (type is GenericNameSyntax)
            {
                var generic = (GenericNameSyntax)type;
                if (generic.Identifier.Text == "ImmutableArray")
                    return true;
            }
            return false;
        }

        static bool IsArray(TypeSyntax type)
        {
            return type is ArrayTypeSyntax;
        }
        static bool IsNullableType(TypeSyntax type, ITypeSymbol typeSymbol)
        {
            if (type is NullableTypeSyntax)
                return true;
            if (type is ArrayTypeSyntax)
                return true;

            for (; typeSymbol != null;)
            {
                switch (typeSymbol.Name)
                {
                    case "ValueType":
                        {
                            var ns = typeSymbol.ContainingNamespace;
                            if (ns != null && ns.Name == "System")
                                return false;
                            break;
                        }
                    case "Object":
                        {
                            var ns = typeSymbol.ContainingNamespace;
                            if (ns != null && ns.Name == "System")
                                return true;
                            break;
                        }
                }
                typeSymbol = typeSymbol.BaseType;
            }

            return true;
        }


        static string MetaLiteral(SyntaxList<AttributeListSyntax> attributes)
        {
            return attributes.SelectMany(attrs => attrs.Attributes)
              .Where(atr => atr.Name.As<SimpleNameSyntax>()?.Identifier.ValueText == "Meta")
              .Select(meta => meta.ArgumentList.Arguments.FirstOrDefault().Expression)
              .OfType<LiteralExpressionSyntax>()
              .Select(_literal => _literal.Token.ValueText)
              .FirstOrDefault();
        }

        public static Member ToMember(MemberDeclarationSyntax member, SemanticModel model)
        {
            if (member is FieldDeclarationSyntax)
            {
                var field = (FieldDeclarationSyntax)member;
                if (field.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword))
                    return null;
                if (!field.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.ReadOnlyKeyword))
                    return null;

                var variable = field.Declaration.Variables.First();

                return ToMember(variable.Identifier, field.Declaration.Type,
                  model.GetTypeInfo(field.Declaration.Type).Type,
                  MetaLiteral(field.AttributeLists),
                  variable.Initializer != null ? variable.Initializer.Value : null
                );
            }
            else if (member is PropertyDeclarationSyntax)
            {
                var property = (PropertyDeclarationSyntax)member;

                if (property.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword))
                    return null;

                if (property.AccessorList?.Accessors.Any(accessor => accessor.Kind() == SyntaxKind.SetAccessorDeclaration) ?? false)
                    return ToMember(property.Identifier, property.Type, model.GetTypeInfo(property.Type).Type, MetaLiteral(property.AttributeLists));
                else
                    return null;
            }
            return null;
        }
        public static string ToUpper(string text)
        {
            if (text == null)
                return null;
            return text.Substring(0, 1).ToUpper() + text.Substring(1);
        }
        public static string ToLower(string text)
        {
            if (text == null)
                return null;
            return text.Substring(0, 1).ToLower() + text.Substring(1);
        }
        public static string EncodeKeywordIdentifier(string name)
        {
            switch (name)
            {
                case "class":
                case "default":
                case "short":
                    return "@" + name;
                default:
                    return name;
            }
        }
        public static Member ToMember(SyntaxToken identifier, TypeSyntax type, ITypeSymbol typeSymbol, string literal, ExpressionSyntax initializer = null)
        {
            var elementType = ArrayElementType(type);
            var isCollection = IsCollection(type);

            var typeKind = IsNullableType(type, typeSymbol) ? ValueKind.NotNullable : ValueKind.Value;
            if (typeKind == ValueKind.NotNullable)
            {
                if (literal == "null")
                    typeKind = ValueKind.Nullable;
            }
            var isCache = false;
            if (initializer != null)
            {
                var invoke = initializer as InvocationExpressionSyntax;
                if (invoke != null)
                {
                    var invokeIdentifier = invoke.Expression as IdentifierNameSyntax;

                    var argument = invoke.ArgumentList.Arguments.FirstOrDefault()?.Expression as SimpleLambdaExpressionSyntax;
                    if (argument != null && invokeIdentifier != null && (invokeIdentifier.Identifier.ValueText == "Cache" || invokeIdentifier.Identifier.ValueText == "Default"))
                    {
                        initializer = new IdentifierReplacer(argument.Parameter.Identifier.ValueText).Visit(argument.Body) as ExpressionSyntax;
                        isCache = invokeIdentifier.Identifier.ValueText == "Cache";
                    }
                }
            }

            initializer = initializer ??
              (
                elementType != null
                ? s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.GenericName("Array").AddTypeArgumentListArguments(elementType), s.IdentifierName("Empty"))
                : null
              );
            Func<ExpressionSyntax, ExpressionSyntax> initializerF = null;
            if (isCollection && initializer == null)
            {
                initializerF = p => s.InvocationExpression(s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, p, s.IdentifierName("OrEmpty")));
            }

            var parameterType = type;
            var parameterTypeKind = typeKind;
            if (typeKind == ValueKind.Value && (initializer != null || initializerF != null))
            {
                parameterType = s.NullableType(type);
                parameterTypeKind = ValueKind.Nullable;
            }
            if (typeKind == ValueKind.NotNullable && (initializer != null || initializerF != null))
            {
                parameterType = type;
                parameterTypeKind = ValueKind.Nullable;
            }
            var optionType = type;
            var optionTypeKind = typeKind;
            if (typeKind == ValueKind.Nullable)
            {
                optionType = s.GenericName("Option").AddTypeArgumentListArguments(type);
                optionTypeKind = ValueKind.Option;
            }
            else if (typeKind == ValueKind.Value)
            {
                optionType = s.NullableType(type);
                optionTypeKind = ValueKind.Nullable;
            }


            return new Member(identifier, type, typeKind, typeSymbol,
              s.Identifier(EncodeKeywordIdentifier(ToLower(identifier.ValueText))), parameterType, parameterTypeKind, initializer, initializerF,
              optionType, optionTypeKind,
              isCache
            );
        }

        public class Member
        {
            public Member(SyntaxToken identifier, TypeSyntax type, ValueKind typeKind, ITypeSymbol typeSymbol,
              SyntaxToken parameterIdentifier, TypeSyntax parameterType, ValueKind parameterTypeKind, ExpressionSyntax initializer,
              Func<ExpressionSyntax, ExpressionSyntax> initializerF, TypeSyntax optionType, ValueKind optionValueKind, bool isCache = false)
            {
                this.Identifier = identifier;
                this.Type = type;
                this.TypeKind = typeKind;
                this.TypeSymbol = typeSymbol;

                this.IsParameter = !isCache;
                this.ParameterIdentifier = parameterIdentifier;
                this.ParameterType = parameterType;
                this.ParameterTypeKind = parameterTypeKind;
                this.Initializer = initializer;
                this.InitializerF = initializerF;

                this.IsOption = !isCache;
                this.OptionType = optionType;
                this.OptionValueKind = optionValueKind;

                this.IsCache = isCache;
            }
            public readonly SyntaxToken Identifier;
            public readonly TypeSyntax Type;
            public readonly ValueKind TypeKind;
            public readonly ITypeSymbol TypeSymbol;

            public readonly bool IsParameter; //создавать ли аргумент в конструкторе
            public readonly SyntaxToken ParameterIdentifier;
            public readonly TypeSyntax ParameterType;
            public readonly ValueKind ParameterTypeKind;
            public readonly ExpressionSyntax Initializer;
            public readonly Func<ExpressionSyntax, ExpressionSyntax> InitializerF;

            public readonly bool IsOption; //создавать ли аргумент в with
            public readonly TypeSyntax OptionType;
            public readonly ValueKind OptionValueKind;

            public readonly bool IsCache;
        }
        public enum ValueKind
        {
            Value,
            NotNullable, //null-type, который не принимает значение null
            Nullable,
            Option
        }
    }
    class IdentifierReplacer : CSharpSyntaxRewriter
    {
        public IdentifierReplacer(string name)
        {
            this.Name = name;
        }
        public readonly string Name;

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (node.Identifier.ValueText == Name)
                return SyntaxFactory.ThisExpression();
            return base.VisitIdentifierName(node);
        }
    }

    static class RoslynHlp
    {
        public static CompilationUnitSyntax AddUsings(this CompilationUnitSyntax compilation, params string[][] usings)
        {
            return compilation
              .AddUsings(usings.Select(@using => s.UsingDirective(ToIdentifierName(@using))).ToArray());
        }
        static NameSyntax ToIdentifierName(string[] names)
        {
            if (names.Length == 0)
                return null;
            var identifiers = names.Select(name => s.IdentifierName(name)).ToArray();
            if (identifiers.Length == 1)
                return identifiers.First();
            var qname = s.QualifiedName(identifiers[0], identifiers[1]);
            foreach (var identifier in identifiers.Skip(2))
            {
                qname = s.QualifiedName(qname, identifier);
            }
            return qname;
        }
        public static TResult Wrap<TSource, TResult>(this TSource source, Func<TSource, TResult> f)
        {
            return f(source);
        }
    }
}
