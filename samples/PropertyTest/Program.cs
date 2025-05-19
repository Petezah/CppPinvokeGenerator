using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using CppAst;
using CppPinvokeGenerator;
using CppPinvokeGenerator.Templates;

namespace PropertyTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // This sample demonstrates the TypeMapper.RegisterReturnTypeParamAddition,
            // TypeMapper.NativeParamMarshallingCode, and TemplateManager.SetClassesUnsafe APIs,
            // as well as the Generate function which takes declaration type and
            // can generate multiple cs files

            string projectFolder = "../../../";
            string outputFolder = $"{projectFolder}Output/";
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            Console.WriteLine("Parsing TestAPI.h...");

            var options = new CppParserOptions();
            // TODO: test on macOS
            options.ConfigureForWindowsMsvc(CppTargetCpu.X86_64);
            options.Defines.Add("_ALLOW_COMPILER_AND_STL_VERSION_MISMATCH"); // Workaround to prevent VS's STL code from breaking the parser, since we simply use the libclang that comes with CppAst
            options.AdditionalArguments.Add("-std=c++17");

            CppCompilation compilation = CppParser.ParseFile(Path.Combine(projectFolder, @"TestAPI.h"), options);

            if (compilation.DumpErrorsIfAny())
            {
                Console.ReadKey();
                return;
            }

            var mapper = new TypeMapper(compilation);

            mapper.RegisterUnsupportedTypes(
                "StringVectorBase");
    
            mapper.RegisterMapping("string_view", "string");

            mapper.NativeParamMarshallingCode += (nativeType, parameterName) =>
            {
                switch (nativeType)
                {
                    case "string":
                    case "string_view":
                        // The newParamName/prolog is trivial here, but demonstrates how to use the API
                        return ("const char*", "demo_string_param", $"auto demo_string_param = {parameterName}; ");

                    default:
                        return (nativeType, string.Empty, string.Empty);
                }
            };

            var stringParamAddition = new TypeMapper.ReturnTypeParamAddition
            {
                Params = new[]
                {
                    new TypeMapper.ReturnTypeParamAddition.Param { Type = "char*", Name = "buffer", PInvokeType = "System.Text.StringBuilder" },
                    new TypeMapper.ReturnTypeParamAddition.Param { Type = "int", Name = "buffer_len" }
                },
                NativeProlog = "auto native_str = ",
                NativeEpilog = " strncpy(buffer, native_str.c_str(), buffer_len); ",
                ManagedProlog = "int buffer_len = 255; var buffer = new System.Text.StringBuilder(buffer_len); ",
                ManagedEpilog = " return buffer.ToString();"
            };
            mapper.RegisterReturnTypeParamAddition("string", stringParamAddition);
            mapper.RegisterReturnTypeParamAddition("string&", stringParamAddition);

            mapper.RenamingForNativeType += nativeReturnType =>
            {
                return ExpandNativeNameForTemplateTypes(nativeReturnType,
                    new[]
                    {
                        "TrivialTemplate"
                    });
            };

            mapper.RenamingForApi += (nativeName, isMethod) =>
            {
                if (!isMethod)
                    return ExpandNativeNameForTemplateTypes(nativeName,
                        new[]
                        {
                            "TrivialTemplate"
                        });
                return nativeName;
            };
            mapper.RegisterPointerOnlyTypes(
                "TrivialTemplate");

            mapper.RegisterFullTypeNameTypes(
                "TrivialTemplate");

            var templateManager = new TemplateManager();

            // Add additional stuff we want to see in the bindings.c
            templateManager
                .AddToCHeader(@"#include ""TestAPI.h""")
                .SetClassesUnsafe(false)
                .SetGlobalFunctionsClassName("TestAPIN");

            PinvokeGenerator.Generate(mapper,
                templateManager,
                @namespace: "TestAPI",
                CallingConvention.Cdecl,
                writeProperties: true,
                dllImportPath: @"TestAPIN.NativeLib",
                outCFile: Path.Combine(outputFolder, "Bindings.Generated.c"),
                outCsFilePath: outputFolder,
                makeCsFilePerClass: true,
                ".Generated");

            Console.WriteLine("Done. See Output folder.");
        }

        private static string ExpandNativeNameForTemplateTypes(string nativeType, string[] templateTypes, params string[] trimmedStrings)
        {
            foreach (var templateType in templateTypes)
            {
                if (nativeType.StartsWith(templateType))
                {
                    var newTypeName = ExpandNativeTemplateTypeName(nativeType);
                    foreach (var trimmedString in trimmedStrings)
                    {
                        newTypeName = newTypeName.Replace(trimmedString, "");
                    }
                    return newTypeName;
                }
            }
            return nativeType;
        }

        private static (string, string[]) SplitNativeTemplateTypeAndParams(string nativeType)
        {
            var lbIdx = nativeType.IndexOf("<");
            var rbIdx = nativeType.LastIndexOf(">");
            if (lbIdx >= 0 && rbIdx >= 0)
            {
                var templateParams = nativeType.Substring(lbIdx + 1, rbIdx - lbIdx - 1).Split(",");
                return (nativeType.Substring(0, lbIdx), templateParams);
            }
            return (nativeType, []);
        }

        private static string ExpandNativeTemplateTypeName(string nativeType)
        {
            var (baseType, templateParams) = SplitNativeTemplateTypeAndParams(nativeType);
            if (templateParams.Length > 0)
            {
                return ExpandTemplateArguments(baseType, templateParams);
            }
            return nativeType.Trim().ToCamelCase();
        }

        private static string ExpandTemplateArguments(string baseTypeName, string[] templateParams)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var param in templateParams)
            {
                sb.Append(ExpandNativeTemplateTypeName(param));
            }
            sb.Append(baseTypeName.ToCamelCase());
            return sb.ToString();
        }
    }
}
