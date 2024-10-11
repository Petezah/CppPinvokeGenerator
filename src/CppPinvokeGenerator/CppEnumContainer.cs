using CppAst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppPinvokeGenerator
{
    public class CppEnumContainer
    {
        public CppEnumContainer(CppEnum cppEnum)
        {
            Items = cppEnum.Items.ToList();
            Enum = cppEnum;
        }

        public CppEnum Enum { get; }

        public string Name => Enum?.GetDisplayName();

        public List<CppEnumItem> Items { get; }

        public override string ToString() => Name;
    }
}
