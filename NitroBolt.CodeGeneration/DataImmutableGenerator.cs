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
using System.Collections.Immutable;

namespace NitroBolt.CodeGeneration
{
    public class DataImmutableGenerator
    {
        public class Class
        {
            public Class(string name, ImmutableArray<Member>? members = null)
            {
                this.Name = name;
                this.Members = members ?? ImmutableArray<Member>.Empty;
            }
            public readonly string Name;

            public readonly ImmutableArray<Member> Members;
        }

        public static string Generate(IEnumerable<Class> clasess)
        {
            //Console.WriteLine(s.ParseName("x.r.y1").As<QualifiedNameSyntax>().Left);
            //Console.WriteLine(s.ParseName("x").As<QualifiedNameSyntax>().Left);
            try
            {

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
                  ;//.AddUsings(usings);


                var isAdded = false;

                foreach (var classGroup in clasess.SplitBy(@class => s.ParseName(@class.Name).As<QualifiedNameSyntax>()?.Left.ToString()))
                {

                    var resultMembers = new List<MemberDeclarationSyntax>();

                    foreach (var @class in classGroup.Items)
                    {
                        var members = @class.Members;

                        if (!members.OrEmpty().Any())
                            continue;

                        var parsedFullClassName = s.ParseName(@class.Name);
                        var className = parsedFullClassName.As<QualifiedNameSyntax>()?.Right.ToString() ?? @class.Name;

                        var rClass = GenerateConstructorAndWithMethod(className, members);
                        rClass = rClass.WithMembers(s.List(GenerateMembers(members).Concat(rClass.Members)));
                        resultMembers.Add(rClass);
                    }

                    var @namespace = classGroup.Key;
                    if (@namespace != null)
                    {
                        var resultNamespace = s.NamespaceDeclaration(s.ParseName(@namespace)).AddMembers(resultMembers.ToArray());
                        r = r.AddMembers(resultNamespace);
                    }
                    else
                    {
                        r = r.AddMembers(resultMembers.ToArray());
                    }

                    isAdded = isAdded || resultMembers.Any();
                    //var byHelper = GenerateByMethod(@class.Name, members);
                    //if (byHelper != null)
                    //    resultMembers.Add(byHelper);
                }

                //resultMembers.AddRange(InterfaceToClassGenerator.GenerateClasses(@namespace));

                //var @namespace = "ToDo";


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

        public static IEnumerable<MemberDeclarationSyntax> GenerateMembers(IEnumerable<Member> members)
        {
            return members.Select(member => s.FieldDeclaration(s.VariableDeclaration(s.ParseTypeName(member.Type))
                    .AddVariables(s.VariableDeclarator(member.Name))
                )
                .AddModifiers(s.Token(SyntaxKind.PublicKeyword), s.Token(SyntaxKind.ReadOnlyKeyword))
            );
        }

        public static ClassDeclarationSyntax GenerateConstructorAndWithMethod(string name, IEnumerable<Member> members)
        {
            var resultClass = s.ClassDeclaration(name)
                 .AddModifiers(s.Token(SyntaxKind.PartialKeyword));

            var parameters = members.Where(member => member.IsParameter).ToArray();

            var lastParameterI = parameters.Select((member, i) => member.ParameterDefaultValue == null ? (i + 1) : (int?)null).Max() ?? 0;

            var constructor = s.ConstructorDeclaration(name)
              .AddModifiers(s.Token(SyntaxKind.PublicKeyword))
              .AddParameterListParameters
              (
                parameters.Select((member, i) =>
                  s.Parameter(s.Identifier(member.ParameterName))
                   .WithType(s.ParseName(member.ParameterType))
                   .WithDefault(member.ParameterDefaultValue != null && i >= lastParameterI ? s.EqualsValueClause(s.LiteralExpression(SyntaxKind.NullLiteralExpression)) : null)
                )
                .ToArray()
              )
              .WithBody(s.Block(members
                  .Select(member => s.ExpressionStatement(s.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, s.IdentifierName(member.Name),
                    
                    !member.IsParameter
                    ? s.ParseExpression(member.ParameterDefaultValue)
                    : member.ParameterDefaultValue != null
                    ? (ExpressionSyntax)s.BinaryExpression(SyntaxKind.CoalesceExpression, s.IdentifierName(member.ParameterName), s.ParseExpression(member.ParameterDefaultValue))
                    : s.IdentifierName(member.ParameterName)
                  )))
              ));

            var with = s.MethodDeclaration(s.IdentifierName(name), "With")
              .AddModifiers(s.Token(SyntaxKind.PublicKeyword))
              .AddParameterListParameters
              (
                members.Select(member =>
                  s.Parameter(s.Identifier(member.OptionName))
                   .WithType(s.ParseTypeName(member.OptionType))
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
                         member.OptionIsElse
                         ? s.InvocationExpression(s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.IdentifierName(member.OptionName), s.IdentifierName("Else")))
                            .AddArgumentListArguments(s.Argument(s.IdentifierName(member.Name)))
                         : (ExpressionSyntax)s.BinaryExpression(SyntaxKind.CoalesceExpression, s.IdentifierName(member.OptionName), s.IdentifierName(member.Name))
                      ))
                      .ToArray()
                  )
                )
              ));

            var resultMembers = new List<MemberDeclarationSyntax> {constructor, with};
            //var resultMembers = new List<MemberDeclarationSyntax> {constructor};

            //resultMembers.AddRange(members.Where(_member => IsArray(_member.Type))
            //    .Select(member => 
            //        s.MethodDeclaration(s.IdentifierName(name), "With_" + member.Identifier.Text)
            //          .AddModifiers(s.Token(SyntaxKind.PublicKeyword))
            //          .AddParameterListParameters
            //          (
            //            s.Parameter(member.ParameterIdentifier)
            //                .WithType(member.Type)
            //                .AddModifiers(s.Token(SyntaxKind.ParamsKeyword))
            //          )
            //          .WithBody(s.Block(s.ReturnStatement
            //          (
            //              s.InvocationExpression(s.IdentifierName("With"))
            //              .AddArgumentListArguments(s.Argument(s.IdentifierName(member.ParameterIdentifier)).WithNameColon(s.NameColon(s.IdentifierName(member.ParameterIdentifier))))))
            //          )
            //    )
            //);

            return resultClass.AddMembers(resultMembers.ToArray());
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

        //private static ClassDeclarationSyntax GenerateByMethod(string name, IEnumerable<Member> members)
        //{
        //    var resultClass = s.ClassDeclaration(name + "Helper")
        //         .AddModifiers(s.Token(SyntaxKind.PublicKeyword), s.Token(SyntaxKind.StaticKeyword), s.Token(SyntaxKind.PartialKeyword));

        //    var _members = members.Where(member => !IsCollection(member.Type)).ToArray();
        //    if (!_members.Any())
        //        return null;

        //    var by = s.MethodDeclaration(s.IdentifierName(name), "By")
        //      .AddParameterListParameters(s.Parameter(s.Identifier("items"))
        //        .WithType(s.GenericName("IEnumerable").AddTypeArgumentListArguments(s.IdentifierName(name)))
        //        .AddModifiers(s.Token(SyntaxKind.ThisKeyword))
        //      )
        //      .AddParameterListParameters
        //      (
        //        _members.Select(member =>
        //          s.Parameter(member.ParameterIdentifier)
        //           .WithType(member.OptionType)
        //           .WithDefault(s.EqualsValueClause(s.LiteralExpression(SyntaxKind.NullLiteralExpression)))
        //        )
        //        .ToArray()
        //      )
        //      .AddModifiers(s.Token(SyntaxKind.PublicKeyword), s.Token(SyntaxKind.StaticKeyword))
        //      .WithBody(s.Block(
        //          _members.Select(member =>
        //            s.IfStatement
        //            (
        //              s.BinaryExpression(SyntaxKind.NotEqualsExpression, s.IdentifierName(member.ParameterIdentifier), s.LiteralExpression(SyntaxKind.NullLiteralExpression)),
        //              s.ReturnStatement
        //              (
        //                 s.InvocationExpression(s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.IdentifierName("items"), s.IdentifierName("FirstOrDefault")))
        //                 .AddArgumentListArguments
        //                 (
        //                   s.Argument
        //                   (
        //                     s.SimpleLambdaExpression
        //                     (
        //                       s.Parameter(s.Identifier("_item")),
        //                       s.BinaryExpression
        //                       (
        //                         SyntaxKind.EqualsExpression,
        //                         s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.IdentifierName("_item"), s.IdentifierName(member.Identifier)),
        //                         s.IdentifierName(member.ParameterIdentifier)
        //                          .Wrap(_ => member.OptionValueKind == ValueKind.Option ? s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, _, s.IdentifierName("Value")) : (ExpressionSyntax)_)
        //                       )
        //                     )
        //                   )
        //                 )
        //              )
        //            )
        //          )

        //        ).AddStatements(s.ReturnStatement(s.LiteralExpression(SyntaxKind.NullLiteralExpression)))
        //      );

        //    resultClass = resultClass.AddMembers(by);

        //    return resultClass;
        //}


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

        //public static Member ToMember(MemberDeclarationSyntax member, SemanticModel model)
        //{
        //    if (member is FieldDeclarationSyntax)
        //    {
        //        var field = (FieldDeclarationSyntax)member;
        //        if (field.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword))
        //            return null;
        //        if (!field.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.ReadOnlyKeyword))
        //            return null;

        //        var variable = field.Declaration.Variables.First();

        //        return ToMember(variable.Identifier, field.Declaration.Type,
        //          model.GetTypeInfo(field.Declaration.Type).Type,
        //          MetaLiteral(field.AttributeLists),
        //          variable.Initializer != null ? variable.Initializer.Value : null
        //        );
        //    }
        //    else if (member is PropertyDeclarationSyntax)
        //    {
        //        var property = (PropertyDeclarationSyntax)member;

        //        if (property.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword))
        //            return null;

        //        if (property.AccessorList?.Accessors.Any(accessor => accessor.Kind() == SyntaxKind.SetAccessorDeclaration) ?? false)
        //            return ToMember(property.Identifier, property.Type, model.GetTypeInfo(property.Type).Type, MetaLiteral(property.AttributeLists));
        //        else
        //            return null;
        //    }
        //    return null;
        //}
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
                case "public":
                    return "@" + name;
                default:
                    return name;
            }
        }
        //public static Member ToMember(SyntaxToken identifier, TypeSyntax type, ITypeSymbol typeSymbol, string literal, ExpressionSyntax initializer = null)
        //{
        //    var elementType = ArrayElementType(type);
        //    var isCollection = IsCollection(type);

        //    var typeKind = IsNullableType(type, typeSymbol) ? ValueKind.NotNullable : ValueKind.Value;
        //    if (typeKind == ValueKind.NotNullable)
        //    {
        //        if (literal == "null")
        //            typeKind = ValueKind.Nullable;
        //    }
        //    var isCache = false;
        //    if (initializer != null)
        //    {
        //        var invoke = initializer as InvocationExpressionSyntax;
        //        if (invoke != null)
        //        {
        //            var invokeIdentifier = invoke.Expression as IdentifierNameSyntax;

        //            var argument = invoke.ArgumentList.Arguments.FirstOrDefault()?.Expression as SimpleLambdaExpressionSyntax;
        //            if (argument != null && invokeIdentifier != null && (invokeIdentifier.Identifier.ValueText == "Cache" || invokeIdentifier.Identifier.ValueText == "Default"))
        //            {
        //                initializer = new IdentifierReplacer(argument.Parameter.Identifier.ValueText).Visit(argument.Body) as ExpressionSyntax;
        //                isCache = invokeIdentifier.Identifier.ValueText == "Cache";
        //            }
        //        }
        //    }

        //    initializer = initializer ??
        //      (
        //        elementType != null
        //        ? s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.GenericName("Array").AddTypeArgumentListArguments(elementType), s.IdentifierName("Empty"))
        //        : null
        //      );
        //    Func<ExpressionSyntax, ExpressionSyntax> initializerF = null;
        //    if (isCollection && initializer == null)
        //    {
        //        initializerF = p => s.InvocationExpression(s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, p, s.IdentifierName("OrEmpty")));
        //    }

        //    var parameterType = type;
        //    var parameterTypeKind = typeKind;
        //    if (typeKind == ValueKind.Value && (initializer != null || initializerF != null))
        //    {
        //        parameterType = s.NullableType(type);
        //        parameterTypeKind = ValueKind.Nullable;
        //    }
        //    if (typeKind == ValueKind.NotNullable && (initializer != null || initializerF != null))
        //    {
        //        parameterType = type;
        //        parameterTypeKind = ValueKind.Nullable;
        //    }
        //    var optionType = type;
        //    var optionTypeKind = typeKind;
        //    if (typeKind == ValueKind.Nullable)
        //    {
        //        optionType = s.GenericName("Option").AddTypeArgumentListArguments(type);
        //        optionTypeKind = ValueKind.Option;
        //    }
        //    else if (typeKind == ValueKind.Value)
        //    {
        //        optionType = s.NullableType(type);
        //        optionTypeKind = ValueKind.Nullable;
        //    }


        //    return new Member(identifier, type, typeKind, typeSymbol,
        //      s.Identifier(EncodeKeywordIdentifier(ToLower(identifier.ValueText))), parameterType, parameterTypeKind, initializer, initializerF,
        //      optionType, optionTypeKind,
        //      isCache
        //    );
        //}

        public class Member
        {
            public Member(string name, string type, 
                bool isParameter, string parameterName, string parameterType, string parameterDefaultValue,
                bool isOption, string optionName, string optionType, bool optionIsElse)
            {
                this.Name = name;
                this.Type = type;

                this.IsParameter = isParameter;
                this.ParameterName = parameterName;
                this.ParameterType = parameterType;
                this.ParameterDefaultValue = parameterDefaultValue;

                this.IsOption = isOption;
                this.OptionName = optionName;
                this.OptionType = optionType;
                this.OptionIsElse = optionIsElse;
            }
            public readonly string Name;
            public readonly string Type;

            public readonly bool IsParameter; //создавать ли аргумент в конструкторе
            public readonly string ParameterName;
            public readonly string ParameterType;
            public readonly string ParameterDefaultValue;

            public readonly bool IsOption; //создавать ли аргумент в with
            public readonly string OptionName;
            public readonly string OptionType;
            public readonly bool OptionIsElse;

        }
        public enum ValueKind
        {
            Value,
            NotNullable, //null-type, который не принимает значение null
            Nullable,
            Option
        }
    }

}
