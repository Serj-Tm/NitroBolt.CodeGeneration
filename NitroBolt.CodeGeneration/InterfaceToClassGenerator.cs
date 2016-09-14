using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using s = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using NitroBolt.Functional;

namespace NitroBolt.CodeGeneration
{
    public class InterfaceToClassGenerator
    {
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
                    var members = GenerateClasses(@namespace);
                    r = r.AddMembers(s.NamespaceDeclaration(@namespace.Name).AddMembers(members));
                    isAdded = members.Any();
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

        public static MemberDeclarationSyntax[] GenerateClasses(NamespaceDeclarationSyntax @namespace)
        {
            var isInterfaceToClass = @namespace.GetLeadingTrivia().Any(trivia => trivia.ToString().Trim() == "//Meta(interface-to-class)");
            if (!isInterfaceToClass)
                return Array<MemberDeclarationSyntax>.Empty;

            var resultMembers = new List<MemberDeclarationSyntax>();
            foreach (var @interface in @namespace.Members.OfType<InterfaceDeclarationSyntax>())
            {
                resultMembers.Add(ToClass(@interface));
            }
            return resultMembers.ToArray();
        }

        static MemberDeclarationSyntax ToClass(InterfaceDeclarationSyntax @interface)
        {
            var @class = s.ClassDeclaration(@interface.Identifier.Text.Substring(1)).AddModifiers(s.Token(SyntaxKind.PublicKeyword), s.Token(SyntaxKind.PartialKeyword));
            @class = @class.AddBaseListTypes(s.SimpleBaseType(s.IdentifierName(@interface.Identifier)));

            foreach (var property in @interface.Members.OfType<PropertyDeclarationSyntax>())
            {
                @class = @class.AddMembers(s.PropertyDeclaration(property.Type, property.Identifier)
                    .AddModifiers(s.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        s.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(s.Token(SyntaxKind.SemicolonToken)), 
                        s.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).AddModifiers(s.Token(SyntaxKind.PrivateKeyword)).WithSemicolonToken(s.Token(SyntaxKind.SemicolonToken))
                    ));
            }

            var members = @interface.Members.OfType<PropertyDeclarationSyntax>()
                .Select(property => ImmutableGenerator.ToMember(property.Identifier, property.Type, null, null))
                .ToArray();
            //var members = Array<ImmutableGenerator.Member>.Empty;

            @class = @class.AddMembers(ImmutableGenerator.GenerateConstructorAndWithMethod(@class.Identifier.Text,  members).Members.ToArray());

            return @class;
        }
    }
}
