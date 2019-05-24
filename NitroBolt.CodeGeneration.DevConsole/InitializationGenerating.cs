using NitroBolt.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.CodeGeneration
{
    class InitializationGenerating
    {
        [CommandLine("--class-initialization")]
        public static void Execute()
        {
            var filepath = @"P:\Projects\Pmk.Guard\Pmk.Guard.Viaduct\IssaDb.cs";

            var code = System.IO.File.ReadAllText(filepath);

            var result = InitializationGenerator.Generate(code);
            Console.WriteLine(result);
            System.IO.File.WriteAllText("q.cs", result);
        }
    }
}
