using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using NitroBolt.Functional;
using NitroBolt.Immutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NitroBolt.Functional;

namespace NitroBolt.CodeGeneration.Sample
{
    partial class XSample
    {
        public XSample(int ? x = null, string y = null, string z = null)
        {
            X = x ?? 1;
            Y = y ?? null;
            Z = z ?? "z";
        }

        public XSample With(int ? x = null, string y = null, string z = null)
        {
            return new XSample(x ?? X, y ?? Y, z ?? Z);
        }
    }

    public static partial class XSampleHelper
    {
        public static XSample By(this IEnumerable<XSample> items, int ? x = null, string y = null, string z = null)
        {
            if (x != null)
                return items.FirstOrDefault(_item => _item.X == x);
            if (y != null)
                return items.FirstOrDefault(_item => _item.Y == y);
            if (z != null)
                return items.FirstOrDefault(_item => _item.Z == z);
            return null;
        }
    }
}