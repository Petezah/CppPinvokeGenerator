using System.Collections.Generic;
using System.Linq;
using CppAst;

namespace CppPinvokeGenerator
{
    public class CppClassContainer
    {
        public CppClassContainer(CppClass cppClass, params CppClass[] baseCppClasses)
        {
            var funcs = cppClass
                .Constructors
                .Concat(cppClass.Functions)
                .Concat(baseCppClasses.SelectMany(c => c.Constructors))
                .Concat(baseCppClasses.SelectMany(c => c.Functions));
            if (cppClass.TemplateKind == CppTemplateKind.TemplateSpecializedClass
                && cppClass.SpecializedTemplate != null)
            {
                funcs = funcs.Concat(cppClass.SpecializedTemplate.Functions);
            }

            Functions = funcs.ToList();
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
