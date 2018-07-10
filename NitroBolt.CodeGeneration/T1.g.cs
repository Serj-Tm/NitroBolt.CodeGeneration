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

namespace NitroBolt.CodeGeneration
{
    partial class T1
    {
        public T1(string target, string source)
        {
            Target = target;
            Source = source;
        }

        public T1 With(string target = null, string source = null)
        {
            return new T1(target ?? Target, source ?? Source);
        }
    }

    public static partial class T1Helper
    {
        public static T1 By(this IEnumerable<T1> items, string target = null, string source = null)
        {
            if (target != null)
                return items.FirstOrDefault(_item => _item.Target == target);
            if (source != null)
                return items.FirstOrDefault(_item => _item.Source == source);
            return null;
        }
    }
}