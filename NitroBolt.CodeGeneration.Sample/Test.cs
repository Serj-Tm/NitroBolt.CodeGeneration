using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using NitroBolt.Functional;
using NitroBolt.Immutable;

namespace Testing
{
    partial class Test
    {
        public readonly string X1;
        public readonly string X2;
        public readonly string Y1;
        public readonly string Y2;
        public readonly bool IsX1;
        public readonly string Public;
        public Test( string  x1,  string  x2,  string  y1 = null,  string  y2 = null,  bool  ? isX1 = null,  string  @public = null)
        {
            X1 = x1;
            X2 = x2;
            Y1 = y1 ?? 1.ToString();
            Y2 = y2 ?? null;
            IsX1 = isX1 ?? true;
            Public = @public ?? "rr";
        }

        public Test With(string x1 = null, Option<string> x2 = null, string y1 = null, Option<string> y2 = null, bool ? isX1 = null, string @public = null)
        {
            return new Test(x1 ?? X1, x2.Else(X2), y1 ?? Y1, y2.Else(Y2), isX1 ?? IsX1, @public ?? Public);
        }
    }
}