using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.IO;
using NitroBolt.Functional;

namespace NitroBolt.CodeGeneration
{
    class Program
    {
        static void Main(string[] args)
        {
            if (true)
            {
                var xml = @"<!--x--><data><X><!--x-->12</X>s<!--c--><Z x='5'>tt</Z><S>v</S></data>";

                if (true)
                {
                    var data = Sample.XLoading.Load(System.Xml.XmlReader.Create(new StringReader(xml)));
                    Console.WriteLine($"x:{data.X}");
                    Console.WriteLine($"y:{data.Y}");
                    Console.WriteLine($"z:{data.Z}");
                }
                if (true)
                {
                    var data = Sample.XLoading.LoadByDictionary(System.Xml.XmlReader.Create(new StringReader(xml)));
                    Console.WriteLine($"x:{data.X}");
                    Console.WriteLine($"y:{data.Y}");
                    Console.WriteLine($"z:{data.Z}");
                }


                return;
            }
            if (true)
            {
                //foreach (var group in new[] { "A", "A", "B", "C", "C", "C", "A"}.SplitBy(x => x))
                //{
                //    Console.WriteLine($"{group.Key}: {group.Items.JoinToString(", ")}");
                //}
                //return;


                var @class = new DataImmutableGenerator.Class("Test",
                     new[]
                     {
                        new DataImmutableGenerator.Member("X1", "string", true, "x1", "string", null, true, "x1", "string", false),
                        new DataImmutableGenerator.Member("X2", "string", true, "x2", "string", null, true, "x2", "Option<string>", true),
                        new DataImmutableGenerator.Member("Y1", "string", true, "y1", "string", "1.ToString()", true, "y1", "string", false),
                        new DataImmutableGenerator.Member("Y2", "string", true, "y2", "string", "null", true, "y2", "Option<string>", true),
                        new DataImmutableGenerator.Member("IsX1", "bool", true, "isX1", "bool?", "true", true, "isX1", "bool?", false),
                        new DataImmutableGenerator.Member("Public", "string", true, "@public", "string", "\"rr\"", true, "@public", "string", false)
                     }.ToImmutableArray()
                    );
                var code = DataImmutableGenerator.Generate(new[] { @class, new DataImmutableGenerator.Class("X.Y." + @class.Name, @class.Members), new DataImmutableGenerator.Class("X.Y.R", @class.Members) });
                Console.WriteLine(code);
                File.WriteAllText("q.cs", code);

                var ts = new[] { new T1("x", "X"), new T1("y", "Y") };
                var tcode = TransformationGenerator.Generate(ts);
                Console.WriteLine(tcode);
                File.WriteAllText("transformation.cs", code);
            }
            return;
            if (true)
            {
                var code = System.IO.File.ReadAllText(@"P:\Projects\NitroBolt.Projects\NitroBolt.DevConsole\CsGenerator.cs");

                var resultCode = InterfaceToClassGenerator.Generate(code);
            }
            //Console.WriteLine(resultCode);
        }
    }

}
