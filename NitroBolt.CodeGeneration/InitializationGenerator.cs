using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NitroBolt.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using s = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NitroBolt.CodeGeneration
{
    public class InitializationGenerator
    {
        public static string Generate(string code)
        {
            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);

                //var compilation = CSharpCompilation.Create("data")
                //  .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                //  .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                //  .AddReferences(MetadataReference.CreateFromFile(typeof(System.Collections.Immutable.ImmutableArray).Assembly.Location))
                //  .AddSyntaxTrees(tree);

                //var model = compilation.GetSemanticModel(tree);


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
                    //var resultMembers = new List<MemberDeclarationSyntax>();
                    var statements = new List<StatementSyntax>();
                    foreach (var @class in @namespace.Members.OfType<ClassDeclarationSyntax>())
                    {

                        var members = GetMembersForGeneration(@class);

                        if (!members.OrEmpty().Any())
                            continue;

                        //resultMembers.Add(GenerateConstructorAndWithMethod(@class.Identifier.ValueText, members));

                        //var byHelper = GenerateByMethod(@class.Identifier.ValueText, members);
                        //if (byHelper != null)
                        //    resultMembers.Add(byHelper);
                        var assignment = s.LocalDeclarationStatement(s.VariableDeclaration(s.IdentifierName("var"))
                            .AddVariables(s.VariableDeclarator("x")
                                .WithInitializer(s.EqualsValueClause(s.ObjectCreationExpression(s.IdentifierName(@class.Identifier))
                                   .WithInitializer(s.InitializerExpression(SyntaxKind.ObjectInitializerExpression,
                                     s.SeparatedList<ExpressionSyntax>(
                                         members.SelectMany(member => new SyntaxNodeOrToken[]
                                         {
                                           s.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, s.IdentifierName(member.Token),
                                             s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.IdentifierName("source"), s.IdentifierName(member.Token))
                                           ),
                                           s.Token(SyntaxKind.CommaToken).WithTrailingTrivia(s.LineFeed)
                                         }
                                        )
                                        .ToArray()

                                     )
                                   ))
                                ))
                             )
                         );
                        statements.Add(assignment);
                    }

                    var resultClass = s.ClassDeclaration("C")
                        .AddMembers(
                            s.MethodDeclaration(s.PredefinedType(s.Token(SyntaxKind.VoidKeyword)), "M")
                            .WithBody(s.Block(statements))
                        );

                    var resultNamespace = s.NamespaceDeclaration(@namespace.Name).AddMembers(resultClass);
                    r = r.AddMembers(resultNamespace);

                    isAdded = isAdded || statements.Any();
                }
                if (!isAdded)
                    return null;
                return r.NormalizeWhitespace().ToString();
                //return r.ToString();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                return null;
            }
        }
        public static Member[] GetMembersForGeneration(ClassDeclarationSyntax @class)
        {
            //if (@class.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword) || @class.Modifiers.All(modifier => modifier.Kind() != SyntaxKind.PartialKeyword))
            //    return null;

            //if (MetaLiteral(@class.AttributeLists) == "skip")
            //    return null;

            return @class.Members
              .Select(member => ToMember(member)).Where(member => member != null).ToArray();
        }
        public static Member ToMember(MemberDeclarationSyntax member)
        {
            if (member is FieldDeclarationSyntax)
            {
                var field = (FieldDeclarationSyntax)member;
                if (field.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword))
                    return null;

                var variable = field.Declaration.Variables.First();

                return new Member(variable.Identifier, field.Declaration.Type);
            }
            else if (member is PropertyDeclarationSyntax)
            {
                var property = (PropertyDeclarationSyntax)member;

                if (property.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword))
                    return null;

                return new Member(property.Identifier, property.Type);
            }
            return null;
        }

        public class Member
        {
            public Member(SyntaxToken token, TypeSyntax type)
            {
                this.Token = token;
                this.Type = type;
            }
            public readonly SyntaxToken Token;
            public readonly TypeSyntax Type;
        }
    }


}
