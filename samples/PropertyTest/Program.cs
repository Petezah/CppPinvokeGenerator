using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
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
    }
}
