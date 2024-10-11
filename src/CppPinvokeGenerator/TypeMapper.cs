using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CppAst;
using Microsoft.Extensions.Logging;

namespace CppPinvokeGenerator
{
    public class TypeMapper
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger<TypeMapper>();

        private readonly CppCompilation _cppCompilation;
        private readonly HashSet<string> _registeredTypes = new HashSet<string>();
        private readonly HashSet<string> _registeredEnums = new HashSet<string>();
        private readonly HashSet<string> _unsupportedTypes = new HashSet<string>();
        private readonly HashSet<string> _unsupportedMethods = new HashSet<string>();

        public struct ReturnTypeParamAddition
        {
            public struct Param
            {
                public string Name;
                public string Type;
                public string PInvokeType;
            }
            public Param[] Params;
            public string NativeProlog;
            public string NativeEpilog;
            public string ManagedProlog;
            public string ManagedEpilog;
            public bool HasManagedCode => !string.IsNullOrEmpty(ManagedProlog) || !string.IsNullOrEmpty(ManagedEpilog);
        };
        private readonly Dictionary<string, ReturnTypeParamAddition> _returnTypeParams = new Dictionary<string, ReturnTypeParamAddition>();

        private readonly Dictionary<string, string> _mappings = new Dictionary<string, string>
            {
                // stdint.h types:
                { "uint8_t",           nameof(Byte) },
                { "uint16_t",          nameof(UInt16) },
                { "uint32_t",          nameof(UInt32) },
                { "uint64_t",          nameof(UInt64) },
                { "usize_t" ,          nameof(UIntPtr) + "/*usize_t*/" }, // .NET really needs native integers
                { "uintptr_t" ,        nameof(UIntPtr) }, // should we use nuint here too?
                { "int8_t",            nameof(SByte) },
                { "int16_t",           nameof(Int16) },
                { "int32_t",           nameof(Int32) },
                { "int64_t",           nameof(Int64) },
                { "size_t" ,           nameof(IntPtr) + "/*size_t*/"},
                { "intptr_t" ,         nameof(IntPtr)},

                // standard types:
                { "bool",               nameof(Byte) + "/*bool*/" }, //bool is not blittable
                { "char",               nameof(SByte) },
                { "unsigned char",      nameof(Byte) },
                { "signed char",        nameof(SByte) },
                { "short",              nameof(Int16) },
                { "short int",          nameof(Int16) },
                { "signed short",       nameof(Int16) },
                { "signed short int",   nameof(Int16) },
                { "unsigned short",     nameof(UInt16) },
                { "unsigned short int", nameof(UInt16) },
                { "int",                nameof(Int32) },
                { "signed",             nameof(Int32) },
                { "signed int",         nameof(Int32) },
                { "unsigned",           nameof(UInt32) },
                { "unsigned int",       nameof(UInt32) },
                { "long",               nameof(Int64) },
                { "long int",           nameof(Int64) },
                { "signed long",        nameof(Int64) },
                { "signed long int",    nameof(Int64) },
                { "unsigned long",      nameof(UInt64) },
                { "unsigned long int",  nameof(UInt64) },
                { "float",              nameof(Single) },
                { "double",             nameof(Double) },
                // TODO: long double, wchar_t ?

                { "void", "void" },
                { "void*", nameof(IntPtr) },
            };

        private readonly string[] _illiegalVariableNames =
            {
                // will be prefixed with @
                "var",
                "namespace",
                "ref",
                "in",
                "out",
                "class",
                "base",
            };

        public CppCompilation CppCompilation => _cppCompilation;

        public TypeMapper(CppCompilation cppCompilation)
        {
            _cppCompilation = cppCompilation;
        }

        internal IEnumerable<CppClassContainer> GetAllClasses()
        {
            var globalFunctions = new List<CppFunction>();
            var allClasses = new List<CppClass>();

            globalFunctions.AddRange(_cppCompilation.Functions);
            allClasses.AddRange(_cppCompilation.GetAllClassesRecursively());

            foreach (CppNamespace ns in _cppCompilation.Namespaces)
            {
                globalFunctions.AddRange(ns.Functions);
                allClasses.AddRange(ns.GetAllClassesRecursively());
            }

            allClasses = allClasses.OnlyUnique().ToList();

            foreach (var cppClass in allClasses)
            {
                if (IsSupported(cppClass.GetDisplayName()))
                {
                    RegisterClass(CleanType(cppClass.GetDisplayName()));
                    yield return new CppClassContainer(cppClass);
                }
            }

            // "Global" class for global functions
            yield return new CppClassContainer(globalFunctions);
        }

        internal IEnumerable<CppEnumContainer> GetAllEnums()
        {
            var allEnums = new List<CppEnum>();

            allEnums.AddRange(_cppCompilation.Enums);

            foreach (var ns in _cppCompilation.Namespaces)
            {
                allEnums.AddRange(ns.Enums);
            }

            allEnums = allEnums.OnlyUnique().ToList();

            foreach (var cppEnum in allEnums)
            {
                if (IsSupported(cppEnum.GetDisplayName()))
                {
                    RegisterEnum(CleanType(cppEnum.GetDisplayName()));
                    yield return new CppEnumContainer(cppEnum);
                }
            }
        }

        public void RegisterClass(string className)
        {
            Logger.LogDebug($"RegisterClass({className})");
            _registeredTypes.Add(className);
        }

        public void RegisterEnum(string enumName)
        {
            Logger.LogDebug($"RegisterEnum({enumName})");
            _registeredEnums.Add(enumName);
        }

        public void RegisterMapping(string nativeType, string managedType)
        {
            Logger.LogDebug($"RegisterMapping({nativeType}, {managedType})");
            _mappings[nativeType] = managedType;
        }

        public void RegisterUnsupportedTypes(params string[] types)
        {
            foreach (string type in types)
            {
                Logger.LogDebug($"RegisterUnsupportedType(t);");
                _registeredTypes.Remove(type);
                _unsupportedTypes.Add(CleanType(type));
            }
        }

        public void RegisterUnsupportedMethod(string className, string methodName)
        {
            if (string.IsNullOrEmpty(className))
                _unsupportedMethods.Add(methodName);
            else
                _unsupportedMethods.Add(className + "." + methodName);
        }

        public void RegisterReturnTypeParamAddition(string nativeType, ReturnTypeParamAddition additionInfo)
        {
            Logger.LogDebug($"RegisterMapping({nativeType}, {additionInfo})");
            _returnTypeParams[nativeType] = additionInfo;
        }

        internal bool IsMethodMarkedAsUnsupported(CppFunction function)
        {
            if (function.Parent is CppClass cppClass)
                return _unsupportedMethods.Contains(cppClass.GetDisplayName() + "." + function.Name);
            return _unsupportedMethods.Contains(function.Name);
        }

        internal string NativeToPinvokeType(CppType nativeType)
        {
            string type = nativeType.GetDisplayName();
            return NativeToPinvokeType(type);
        }

        internal string NativeToPinvokeType(string type)
        {
            bool isPtr = type.Trim().EndsWith("*");
            type = CleanType(type);

            if (_mappings.TryGetValue(type, out string managedType))
                return managedType + (isPtr ? "*" : "");

            if (isPtr && _mappings.TryGetValue(type + "*", out string managedTypePtr))
                return managedTypePtr;

            if (_registeredTypes.Contains(type))
                return "IntPtr";

            if (_registeredEnums.Contains(type))
                return type;

            Logger.LogWarning($"No C# equivalent for {type}");
            return type + (isPtr ? "*" : "");
        }

        internal bool IsSupported(string type)
        {
            // skip rvalues
            if (type.Contains("&&"))
                return false;

            return !_unsupportedTypes.Contains(CleanType(type));
        }

        internal static string CleanType(string type, bool keepPointer = false)
        {
            type = type
                .Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries).Last()
                .Replace("const ", "")
                .Replace(" const", "")
                .Replace("&", "");

            if (!keepPointer)
                type = type.Replace("*", "");
            return type.Trim();
        }

        internal string EscapeVariableName(string name)
        {
            if (_illiegalVariableNames.Contains(name))
                return "@" + name;
            return name;
        }

        // (name, isMethod)
        public event Func<string, bool, string> RenamingForApi;

        internal string RenameForApi(string name, bool isMethod)
        {
            if (RenamingForApi != null)
                name = RenamingForApi(name, isMethod);

            var map = new Dictionary<string, string> {
                    { "JSON",  "Json" },
                    { "XML",   "Xml" },
                    { "minify", "Minify" },
                    { "index", "Index" },
                };

            foreach (var item in map)
                name = name.Replace(item.Key, item.Value);

            return name.ToCamelCase();
        }

        // (nativeType, parameterName) => (nativeTypeOut, newParameterName, body)
        public event Func<string, string, (string, string, string)> NativeParamMarshallingCode;

        internal (string, string, string) GetNativeParamMarshallingCode(string nativeType, string paramterName)
        {
            bool isPtr = nativeType.Trim().EndsWith("*");
            nativeType = CleanType(nativeType);
            if (NativeParamMarshallingCode != null)
                return NativeParamMarshallingCode(nativeType + (isPtr ? "*" : ""), paramterName);
            return (nativeType + (isPtr ? "*" : ""), string.Empty, string.Empty);
        }

        internal bool IsKnownNativeType(CppType nativeTtype)
        {
            var type = nativeTtype.GetDisplayName();
            if (_registeredTypes.Any(rt => rt == CleanType(type)))
                return true;
            return false;
        }

        internal string MapToManagedApiType(CppType nativeType)
        {
            string type = nativeType.GetDisplayName();
            if (IsKnownNativeType(nativeType))
                return RenameForApi(CleanType(type), false);

            type = NativeToPinvokeType(nativeType);
            if (type.Contains("/*usize_t*/")) return nameof(UInt64);
            if (type.Contains("/*size_t*/")) return nameof(Int64);
            if (type.Contains("/*bool*/")) return nameof(Boolean);

            return type;
        }

        internal bool NeedsCastForApi(CppType nativeType, out string cast)
        {
            string managedType = NativeToPinvokeType(nativeType);
            if (!managedType.Contains("/*") || nativeType.IsBool())
            {
                cast = null;
                return false;
            }

            cast = $"({MapToManagedApiType(nativeType)})";
            return true;
        }

        internal ReturnTypeParamAddition? GetReturnTypeParamAddition(string returnType)
        {
            if (_returnTypeParams.TryGetValue(returnType, out var info))
            {
                return info;
            }
            return null;
        }
    }
}
