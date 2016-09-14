using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.CodeGeneration
{
    class Program
    {
        static void Main(string[] args)
        {
            var code = System.IO.File.ReadAllText(@"P:\Projects\NitroBolt.Projects\NitroBolt.DevConsole\CsGenerator.cs");

            var resultCode = InterfaceToClassGenerator.Generate(code);
            //Console.WriteLine(resultCode);
        }
    }
}
