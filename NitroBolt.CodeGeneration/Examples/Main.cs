using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.CodeGenerator
{
  public partial class Array1
  {
    public readonly string[] Items;
  }
  public partial class ImmutableArray1
  {
    public readonly ImmutableArray<string> Items;
  }

  public partial class NullMember1
  {
    [Meta("null")]
    public readonly string Title;
  }

  public partial class NullMember2
  {
    [Meta("not-null")]
    public readonly string Title;
  }

  public partial class ValueTypeMember1
  {
    public readonly int Index;
    public readonly int Index2 = 2;
  }

  [Meta("skip")]
  public partial class Skip1
  {
    public readonly int Index;
  }

  public class MetaAttribute:Attribute
  {
    public MetaAttribute(string value)
    {
    }
  }
}
