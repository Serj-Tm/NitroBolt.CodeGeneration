using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using NitroBolt.Functional;
using NitroBolt.Immutable;
using System;
using System.Collections.Generic;
using System.Text;

namespace NitroBolt.CodeGeneration.Sample
{
    partial class ImmutableR
    {
        public ImmutableR(int ? x = null, int ? y = null)
        {
            X = x ?? X;
            Y = y ?? Y;
        }

        public ImmutableR With(int ? x = null, int ? y = null)
        {
            return new ImmutableR(x ?? X, y ?? Y);
        }
    }
}