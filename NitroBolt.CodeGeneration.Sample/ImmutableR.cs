using System;
using System.Collections.Generic;
using System.Text;

namespace NitroBolt.CodeGeneration.Sample
{
    public partial class ImmutableR
    {
        public readonly int X = 2;
        public int Y { get; private set; } = 1; 
    }
}
