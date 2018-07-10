using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NitroBolt.Functional;

namespace NitroBolt.CodeGeneration.Sample
{
    public static class XLoading
    {
        public static XSample Load(XmlReader reader)
        {
            int? x = null;
            string y = null;
            string z = null;

            for (; reader.Read();)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    reader.Read();
                    break;
                }
            }

            for (;;)
            {
                if (reader.NodeType == XmlNodeType.EndElement || reader.NodeType == XmlNodeType.None)
                    break;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var tag = reader.Name;
                        reader.Read();
                        switch (tag)
                        {
                            case "X":
                                x = reader.ReadContentAsInt();
                                break;
                            case "Y":
                                y = reader.ReadContentAsString();
                                break;
                            case "Z":
                                z = reader.ReadContentAsString();
                                break;
                        }
                        reader.ReadOuterXml();
                        //var xml = reader.ReadOuterXml();
                        //Console.WriteLine(xml);
                        break;
                    default:
                        Console.WriteLine($"{reader.NodeType}:{reader.Value}");
                        reader.Read();
                        break;
                }
            }

            return new XSample(x: x, y: y, z: z);
        }
        public static XSample LoadByDictionary(XmlReader reader)
        {
            var converterIndex = new Dictionary<string, Func<string, object>>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"X", s => Convert.ToInt32(s) },
                {"Y", s => s },
                {"Z", s => s },
            };

            var index = new Dictionary<string, object>();

            for (; reader.Read();)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    reader.Read();
                    break;
                }
            }

            for (;;)
            {
                if (reader.NodeType == XmlNodeType.EndElement || reader.NodeType == XmlNodeType.None)
                    break;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var tag = reader.Name;
                        reader.Read();

                        var converter = converterIndex.Find(tag);
                        if (converter != null)
                            index[tag] = converter(reader.ReadContentAsString());

                        reader.ReadOuterXml();
                        //var xml = reader.ReadOuterXml();
                        //Console.WriteLine(xml);
                        break;
                    default:
                        Console.WriteLine($"{reader.NodeType}:{reader.Value}");
                        reader.Read();
                        break;
                }
            }

            return new XSample(x: (int?)index.Find("X"), y: (string)index.Find("Y"), z: (string)index.Find("Z"));
        }
    }
    public partial class XSample
    {
        public readonly int X = 1;
        public readonly string Y = null;
        public readonly string Z = "z";
    }
}
