using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CppAst;

namespace CppPinvokeGenerator
{
    public static class CppAstExtensions
    {
        public static bool IsVoid(this CppType type) => type.GetDisplayName() == "void";

        public static bool IsBool(this CppType type) => type.GetDisplayName() == "bool";

        public static bool IsOperator(this CppFunction func) => func.Name.StartsWith("operator"); // TODO: regex?

        public static bool IsStatic(this CppFunction func) => func.StorageQualifier == CppStorageQualifier.Static;

        /// <summary>
        /// If a type defined under another type, then print the full name, e.g. Class1::Class2
        /// </summary>
        public static string GetFullTypeName(this CppType cppType)
        {
            bool isRef = false;
            if (cppType is CppReferenceType refType)
            {
                isRef = true;
                cppType = refType.ElementType;
            }

            string templateArgs = string.Empty;
            if (cppType is CppClass cppClass && cppClass.TemplateKind == CppTemplateKind.TemplateSpecializedClass)
            {
                templateArgs = GetClassTemplateArgs(cppClass);
            }

            string name = cppType.GetDisplayName();
            while (cppType.Parent is CppType parentType)
            {
                name = parentType.GetDisplayName() + "::" + name;
                cppType = parentType;
            }
            return name + templateArgs + (isRef ? "&" : "");
        }

        public static string GetClassTemplateArgs(this CppClass cppClass)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('<');
            for (int i = 0; i < cppClass.TemplateSpecializedArguments.Count; i++)
            {
                var ta = cppClass.TemplateSpecializedArguments[i];
                if (i != 0)
                {
                    sb.Append(", ");
                }
                sb.Append(ta.ArgAsType.GetFullTypeName());
            }
            sb.Append('>');
            return sb.ToString();
        }

        public static string GetFlatTypeName(this CppType cppType)
        {
            string name = cppType.GetDisplayName();
            if (cppType is CppClass cppClass && cppClass.TemplateKind == CppTemplateKind.TemplateSpecializedClass)
            {
                foreach (var arg in cppClass.TemplateSpecializedArguments)
                {
                    name += arg.ArgAsType.GetDisplayName();
                }
            }
            return name;
        }

        public static List<CppClass> GetAllClassesRecursively(this CppCompilation compilation)
        {
            var cppClasses = new List<CppClass>();
            foreach (var cppClass in compilation.Classes)
                VisitClass(cppClasses, cppClass);
            return cppClasses;
        }

        public static List<CppClass> GetAllClassesRecursively(this CppNamespace compilation)
        {
            var cppClasses = new List<CppClass>();
            foreach (var cppClass in compilation.Classes)
                VisitClass(cppClasses, cppClass);
            return cppClasses;
        }

        private static void VisitClass(List<CppClass> cppClasses, CppClass cppClass)
        {
            cppClasses.Add(cppClass);
            foreach (var subClass in cppClass.Classes)
                VisitClass(cppClasses, subClass);
        }

        public static IEnumerable<CppClass> GetBaseClasses(this CppClass cppClass)
        {
            foreach (var baseType in cppClass.BaseTypes)
            {
                if (baseType.Type is CppClass baseClass)
                {
                    yield return baseClass;
                    var bases = baseClass.GetBaseClasses();
                    foreach (var c in bases)
                    {
                        yield return c;
                    }
                }
            }
        }

        public static bool DumpErrorsIfAny(this CppCompilation compilation)
        {
            if (!compilation.Diagnostics.HasErrors)
                return false;

            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            foreach (var dgn in compilation.Diagnostics.Messages)
                if (dgn.Type == CppLogMessageType.Error)
                    Console.WriteLine($"{dgn.Text} at {dgn.Location}");

            Console.ForegroundColor = color;

            return true;
        }

        public static IEnumerable<T> OnlyUnique<T>(this IEnumerable<T> source) where T : CppType
        {
            return source.Distinct(new CppTypeEqualityComparer<T>());
        }

        /// <summary>
        /// E.g.: "void Foo(int age, char* name)" ==> "ic" (first letters of parameters' types)
        /// </summary>
        public static string ParametersMask(this CppFunction function)
        {
            var sb = new StringBuilder();
            var parameters = function.Parameters.ToList();
            if (parameters.Count < 1)
                sb.Append("0");
            else
                foreach (var p in parameters)
                {
                    var type = p.Type.GetDisplayName()
                        .Replace("const ", "")
                        .Replace("*", "")
                        .Replace("&", "")
                        .Trim();

                    sb.Append(type[0]);
                }

            return sb.ToString();
        }

        public static bool IsCopyConstructor(this CppFunction function)
        {
            if (!function.IsConstructor || function.Parameters.Count != 1)
                return false;

            if (function.Parameters[0].Type is CppReferenceType refType
                && refType.ElementType is CppQualifiedType qualType)
            {
                return qualType.Qualifier == CppTypeQualifier.Const;
            }
            return false;
        }
    }

    internal class CppTypeEqualityComparer<T> : IEqualityComparer<T> where T : CppType
    {
        public bool Equals(T x, T y) => x.GetFullTypeName().Equals(y.GetFullTypeName());

        public int GetHashCode(T c) => c.GetFullTypeName().GetHashCode();
    }
}
