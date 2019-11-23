using NitroBolt.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.CodeGeneration
{
    public static class ImmutableGenerating
    {
        [CommandLine("--immutable-class")]
        public static void Execute()
        {
            var code = File.ReadAllText(@"p:\Projects\NitroBolt.CodeGeneration\NitroBolt.CodeGeneration.Sample\ImmutableR.cs");
            var g_code = ImmutableGenerator.Generate(code);
            File.WriteAllText("q.cs", g_code);
        }


    }
}
