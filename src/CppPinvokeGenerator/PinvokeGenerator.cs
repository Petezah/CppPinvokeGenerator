using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using CppAst;
using CppPinvokeGenerator.Templates;
using Microsoft.Extensions.Logging;

namespace CppPinvokeGenerator
{
    public class PinvokeGenerator
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger<TypeMapper>();

        /// <param name="dllImportPath">will be used as the first argument in [DllImport]. Can be a path to some constant</param>
        public static void Generate(TypeMapper mapper, TemplateManager templateManager, string @namespace, string dllImportPath, string outCFile, string outCsFile)
        {
            Generate(mapper, templateManager, @namespace, CallingConvention.Cdecl, false, dllImportPath, outCFile, outCsFile);
        }

        /// <param name="dllImportPath">will be used as the first argument in [DllImport]. Can be a path to some constant</param>
        public static void Generate(TypeMapper mapper, TemplateManager templateManager, string @namespace, CallingConvention callingConvention, bool writeProperties, string dllImportPath, string outCFile, string outCsFile)
        {
            var csFileSb = new StringBuilder();
            var cFileSb = new StringBuilder();

            foreach (CppClassContainer cppClass in mapper.GetAllClasses())
            {
                // Header for C types:
                cFileSb.AppendLine();
                cFileSb.AppendLine();
                cFileSb.Append("/************* ");
                cFileSb.Append(cppClass.IsGlobal ? "Global functions:" : cppClass.Class.GetFullTypeName());
                cFileSb.Append(" *************/");
                cFileSb.AppendLine();
                cFileSb.AppendLine();

                var csDllImportsSb = new StringBuilder();
                var csApiSb = new StringBuilder();
                var allFunctions = new List<CppFunction>();

                // filter out functions we are not going to bind:
                foreach (var function in cppClass.Functions)
                {
                    if (function.Visibility == CppVisibility.Private)
                        continue;

                    if (mapper.IsMethodMarkedAsUnsupported(function) ||
                        !mapper.IsSupported(function.ReturnType.GetDisplayName()) ||
                        !function.Parameters.All(p => mapper.IsSupported(p.Type.GetDisplayName())) ||
                        function.IsOperator() ||
                        function.IsCopyConstructor())
                    {
                        cFileSb.AppendLine($"//NOT_BOUND:".PadRight(32) + function);
                        continue;
                    }

                    allFunctions.Add(function);
                }

                var propertyGenerator = new PropertyGenerator();
                propertyGenerator.RegisterCandidates(mapper, allFunctions);

                foreach (CppFunction function in allFunctions)
                {
                    // Type_MethodName_argsMask
                    string flatFunctionName = $"{cppClass.Name}_{function.Name}_{function.ParametersMask()}";

                    var cfunctionWriter = new FunctionWriter();
                    var dllImportWriter = new FunctionWriter();
                    var apiFunctionWriter = new FunctionWriter();

                    var callingConventionStr = callingConvention == CallingConvention.Winapi ? string.Empty : $", CallingConvention = CallingConvention.{callingConvention}";
                    dllImportWriter
                        .Attribute($"[DllImport({dllImportPath}{callingConventionStr})]")
                        .Private()
                        .Static()
                        .Extern();

                    apiFunctionWriter
                        .SummaryComment(function.Comment?.ChildrenToString());

                    var propertyInfo = propertyGenerator.AsPropertyCandidate(function);
                    if (propertyInfo == null || !writeProperties)
                        apiFunctionWriter.Public();
                    else
                    {
                        apiFunctionWriter.Private();
                        if (!propertyInfo.WrittenToApi)
                        {
                            csApiSb.AppendLine(propertyInfo.GenerateProperty().Tabify(2));
                        }
                    }

                    if (function.IsStatic() || cppClass.IsGlobal)
                        apiFunctionWriter.Static();

                    var returnTypeInfo = mapper.GetReturnTypeParamAddition(function.ReturnType.GetFullTypeName());
                    if (function.IsConstructor)
                    {
                        cfunctionWriter.ReturnType(cppClass.Class.GetFullTypeName() + "*", "EXPORTS", 32);
                        dllImportWriter.ReturnType(nameof(IntPtr));
                    }
                    else
                    {
                        var cReturnType = function.ReturnType.GetFullTypeName();
                        cReturnType = returnTypeInfo.HasValue ? "void" : cReturnType; // in the case of a parameter adjustment, the return value is coming back in OUT parameters
                        cfunctionWriter.ReturnType(cReturnType, "EXPORTS", 32);
                        dllImportWriter.ReturnType(returnTypeInfo.HasValue ? "void" : mapper.NativeToPinvokeType(function.ReturnType)); // DLL import must match the C function
                        apiFunctionWriter.ReturnType(mapper.MapToManagedApiType(function.ReturnType));
                    }

                    cfunctionWriter.MethodName(flatFunctionName);
                    dllImportWriter.MethodName(flatFunctionName);
                    apiFunctionWriter.MethodName(mapper.RenameForApi(function.Name, isMethod: !function.IsConstructor));

                    if (!function.IsConstructor &&
                        !function.IsStatic() &&
                        !cppClass.IsGlobal)
                    {
                        // all instance methods will have "this" as the first argument
                        cfunctionWriter.Parameter(cppClass.Class.GetFullTypeName() + "*", "target");
                        dllImportWriter.Parameter(nameof(IntPtr), "target");
                    }

                    foreach (var parameter in function.Parameters)
                    {
                        var (nativeType, newNativeName, prolog) = mapper.GetNativeParamMarshallingCode(parameter.Type.GetDisplayName(), parameter.Name);
                        cfunctionWriter.BodyAppendProlog(prolog);
                        cfunctionWriter.Parameter(
                            nativeType,
                            parameter.Name);

                        dllImportWriter.Parameter(
                            mapper.NativeToPinvokeType(parameter.Type),
                            mapper.EscapeVariableName(parameter.Name));

                        apiFunctionWriter.Parameter(
                            mapper.MapToManagedApiType(parameter.Type),
                            mapper.EscapeVariableName(parameter.Name));
                    }
                    if (returnTypeInfo.HasValue)
                    {
                        // C function needs to handle the new parameters;
                        // DLL importer needs to handle the new parameters
                        // API signature stays the same; only its body gets adjusted (later)
                        foreach (var param in returnTypeInfo.Value.Params)
                        {
                            cfunctionWriter.Parameter(param.Type, param.Name);

                            dllImportWriter.Parameter(
                                param.PInvokeType ?? mapper.NativeToPinvokeType(param.Type),
                                mapper.EscapeVariableName(param.Name));
                        }
                        cfunctionWriter.BodyAppendProlog(returnTypeInfo.Value.NativeProlog);
                        cfunctionWriter.BodyAppendEpilog(returnTypeInfo.Value.NativeEpilog);
                        apiFunctionWriter.BodyAppendProlog(returnTypeInfo.Value.ManagedProlog);
                        apiFunctionWriter.BodyAppendEpilog(returnTypeInfo.Value.ManagedEpilog);
                    }

                    cfunctionWriter.BodyStart(); // append "return" if needed

                    if (cppClass.IsGlobal)
                    {
                        // GlobalMethod
                        cfunctionWriter.BodyCallMethod(function.Name);
                    }
                    else if(!function.IsConstructor && !function.IsStatic())
                    {
                        // target->InstanceMethod
                        cfunctionWriter.BodyCallMethod($"target->{function.Name}");
                    }
                    else if (function.IsStatic())
                    {
                        // Class1::StaticMethod
                        cfunctionWriter.BodyCallMethod(cppClass.Class.GetFullTypeName() + "::" + function.Name);
                    }
                    else
                    {
                        // new Class1
                        cfunctionWriter.BodyCallMethod($"new {cppClass.Class.GetFullTypeName()}");
                    }

                    if (function.IsConstructor)
                    {
                        apiFunctionWriter
                            .BaseCtor("ownsHandle: true")
                            .StartExpressionBody()
                            .BodyCallMethod("SetHandle");
                    }
                    else if (returnTypeInfo.HasValue && returnTypeInfo.Value.HasManagedCode)
                        apiFunctionWriter.BodyStart();
                    else
                        apiFunctionWriter.StartExpressionBody();

                    if (mapper.IsKnownNativeType(function.ReturnType))
                    {
                        // call "ctor(IntPtr, bool)"
                        apiFunctionWriter.BodyCallMethod("new " + mapper.MapToManagedApiType(function.ReturnType));
                    }

                    // some API functions need special casts, e.g. IntPtr/*size_t*/ (nint) to long
                    if (mapper.NeedsCastForApi(function.ReturnType, out string returnTypeApiCast))
                        apiFunctionWriter.BodyCallMethod(returnTypeApiCast);

                    apiFunctionWriter.BodyCallMethod(flatFunctionName);

                    // all API functions pass "Handle" property as the first argument to DllImport methods
                    if (!function.IsConstructor && !cppClass.IsGlobal && !function.IsStatic())
                        apiFunctionWriter.PassParameter("Handle");

                    foreach (var parameter in function.Parameters)
                    {
                        var (nativeType, newNativeName, prolog) = mapper.GetNativeParamMarshallingCode(parameter.Type.GetDisplayName(), parameter.Name);
                        cfunctionWriter.PassParameter(string.IsNullOrEmpty(newNativeName) ? parameter.Name : newNativeName);

                        string dllImportType = mapper.NativeToPinvokeType(parameter.Type);
                        string escapedName = mapper.EscapeVariableName(parameter.Name);

                        if (parameter.Type.IsBool()) // bool to byte 
                            escapedName = $"(Byte)({escapedName} ? 1 : 0)";

                        // cast to DllImport's type if needed (TODO: wrap with checked {})
                        else if (dllImportType.Contains("/*"))
                            escapedName = $"({dllImportType.DropComments()}){escapedName}";

                        // if the parameter is a C# class-wrapper - pass its Handle
                        else if (mapper.IsKnownNativeType(parameter.Type))
                            escapedName = $"({escapedName} == null ? IntPtr.Zero : {escapedName}.Handle)";
                        
                        apiFunctionWriter.PassParameter(escapedName);
                    }
                    if (returnTypeInfo.HasValue)
                    {
                        // Adjust API body to handle additional parameters
                        foreach (var param in returnTypeInfo.Value.Params)
                        {
                            string escapedName = mapper.EscapeVariableName(param.Name);
                            apiFunctionWriter.PassParameter(escapedName);
                        }
                    }

                    if (function.ReturnType.IsBool())
                        csApiSb.AppendLine(apiFunctionWriter.Build(" > 0").Tabify(2)); // byte to bool
                    else if (mapper.IsKnownNativeType(function.ReturnType))
                    {
                        // pass "false" to "ownsHandle" argument
                        apiFunctionWriter.EndLastCall(true).PassParameter("false");
                        csApiSb.AppendLine(apiFunctionWriter.Build().Tabify(2));
                    }
                    else
                        csApiSb.AppendLine(apiFunctionWriter.Build().Tabify(2));

                    csApiSb.AppendLine();

                    csDllImportsSb.AppendLine(dllImportWriter.BuildWithoutBody().Tabify(2));
                    csDllImportsSb.AppendLine();

                    cFileSb.AppendLine(cfunctionWriter.Build());
                }

                if (cppClass.IsGlobal)
                    csFileSb.Append(templateManager.CSharpGlobalClass(csDllImportsSb.ToString(), csApiSb.ToString(), dllImportPath));
                else if (!cppClass.IsGlobal)
                {
                    csFileSb.Append(templateManager.CSharpClass(mapper.RenameForApi(cppClass.Name, false), cppClass.Name, csDllImportsSb.ToString(), csApiSb.ToString(), dllImportPath));

                    // Append "delete" method:
                    // EXPORTS(void) %class%_delete(%class%* target) { if (target) delete target; }
                    var cfunctionWriter = new FunctionWriter();
                    cfunctionWriter.ReturnType("void", "EXPORTS", 32)
                        .MethodName(cppClass.Name + "__delete")
                        .Parameter(cppClass.Class.GetFullTypeName() + "*", "target")
                        .BodyStart()
                        .Body("delete target");
                    cFileSb
                        .AppendLine(cfunctionWriter.Build());
                }
            }

            File.WriteAllText(outCFile, templateManager.CHeader() + cFileSb);
            File.WriteAllText(outCsFile, templateManager.CSharpHeader(@namespace, csFileSb.ToString()));
        }
    }
}