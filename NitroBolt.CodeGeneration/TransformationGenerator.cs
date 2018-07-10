using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using s = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NitroBolt.CodeGeneration
{
    [Obsolete("Not finished", false)]
    public static class TransformationGenerator
    {
        public static string Generate(IEnumerable<T1> ts)
        {
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



                var args = new List<ArgumentSyntax>();

                foreach (var t in ts)
                {
                    var arg = s.Argument(s.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, s.IdentifierName("source"), s.IdentifierName(t.Source)))
                        .WithNameColon(s.NameColon(t.Target));
                    args.Add(arg);
                }


                var @class = s.ClassDeclaration("Transformation")
                    .AddMembers
                    (
                      s.MethodDeclaration(s.IdentifierName("T"), s.Identifier("Transform"))
                        .AddModifiers(s.Token(SyntaxKind.PublicKeyword), s.Token(SyntaxKind.StaticKeyword))
                        .AddParameterListParameters(s.Parameter(s.Identifier("source")).WithType(s.IdentifierName("S")))
                        .WithBody(s.Block(s.ReturnStatement(s.ObjectCreationExpression(s.IdentifierName("T")).AddArgumentListArguments(args.ToArray()))))
                    );

                r = r.AddMembers(@class);

                //resultMembers.AddRange(InterfaceToClassGenerator.GenerateClasses(@namespace));

                //var @namespace = "ToDo";


                return r.NormalizeWhitespace().ToString();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                return null;
            }
        }
    }
}
