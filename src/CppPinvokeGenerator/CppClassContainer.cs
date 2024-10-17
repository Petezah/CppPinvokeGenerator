using System.Collections.Generic;
using System.Linq;
using CppAst;

namespace CppPinvokeGenerator
{
    public class CppClassContainer
    {
        public CppClassContainer(CppClass cppClass, params CppClass[] baseCppClasses)
        {
            Functions = cppClass
                .Constructors
                .Concat(cppClass.Functions)
                .Concat(baseCppClasses.SelectMany(c => c.Functions))
                .ToList();
            Class = cppClass;
        }

        public CppClassContainer(IEnumerable<CppFunction> functions)
        {
            Functions = functions.ToList();
        }

        public bool IsGlobal => Class == null;

        public CppClass Class { get; }

        public string Name => Class?.GetDisplayName();

        public string CHeaderTypeName => IsGlobal ? "Global functions:" : Class.GetFullTypeName();

        public List<CppFunction> Functions { get; }

        public override string ToString() => Name;
    }
}
