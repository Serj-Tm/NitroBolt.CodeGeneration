using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using NitroBolt.Functional;
using NitroBolt.Immutable;

namespace NitroBolt.CodeGenerator
{
    partial class Array1
    {
        public Array1(string[] items)
        {
            Items = items ?? Array<string>.Empty;
        }

        public Array1 With(string[] items = null)
        {
            return new Array1(items ?? Items);
        }
    }

    partial class ImmutableArray1
    {
        public ImmutableArray1(ImmutableArray<string>? items = null)
        {
            Items = items.OrEmpty();
        }

        public ImmutableArray1 With(ImmutableArray<string>? items = null)
        {
            return new ImmutableArray1(items ?? Items);
        }
    }

    partial class NullMember1
    {
        public NullMember1(string title = null)
        {
            Title = title;
        }

        public NullMember1 With(Option<string> title = null)
        {
            return new NullMember1(title.Else(Title));
        }
    }

    public static partial class NullMember1Helper
    {
        public static NullMember1 By(this IEnumerable<NullMember1> items, Option<string> title = null)
        {
            if (title != null)
                return items.FirstOrDefault(_item => _item.Title == title.Value);
            return null;
        }
    }

    partial class NullMember2
    {
        public NullMember2(string title)
        {
            Title = title;
        }

        public NullMember2 With(string title = null)
        {
            return new NullMember2(title ?? Title);
        }
    }

    public static partial class NullMember2Helper
    {
        public static NullMember2 By(this IEnumerable<NullMember2> items, string title = null)
        {
            if (title != null)
                return items.FirstOrDefault(_item => _item.Title == title);
            return null;
        }
    }

    partial class ValueTypeMember1
    {
        public ValueTypeMember1(int index, int ? index2 = null)
        {
            Index = index;
            Index2 = index2 ?? 2;
        }

        public ValueTypeMember1 With(int ? index = null, int ? index2 = null)
        {
            return new ValueTypeMember1(index ?? Index, index2 ?? Index2);
        }
    }

    public static partial class ValueTypeMember1Helper
    {
        public static ValueTypeMember1 By(this IEnumerable<ValueTypeMember1> items, int ? index = null, int ? index2 = null)
        {
            if (index != null)
                return items.FirstOrDefault(_item => _item.Index == index);
            if (index2 != null)
                return items.FirstOrDefault(_item => _item.Index2 == index2);
            return null;
        }
    }
}