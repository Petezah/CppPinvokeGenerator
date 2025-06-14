﻿using System;
using System.IO;
using System.Text;

namespace CppPinvokeGenerator.Templates
{
    public class TemplateManager
    {
        private static readonly string UnsafeClassString = "unsafe ";

        private StringBuilder cHeader = new StringBuilder();
        private string csGlobalClass = "GlobalFunctions";
        private string csClassUnsafeOrSafe = UnsafeClassString; // Preserve original default behavior

        public string GlobalFunctionsClassName => csGlobalClass;

        public TemplateManager AddToCHeader(string content)
        {
            cHeader.AppendLine(content);
            return this;
        }

        public TemplateManager SetGlobalFunctionsClassName(string className)
        {
            csGlobalClass = className;
            return this;
        }

        public TemplateManager SetClassesUnsafe(bool isUnsafe)
        {
            csClassUnsafeOrSafe = isUnsafe ? UnsafeClassString : string.Empty;
            return this;
        }

        public string CHeader() 
            => GetEmbeddedResource("CHeader.txt") + cHeader;

        public string CTypeHeader(string typename)
            => GetEmbeddedResource("CTypeHeader.txt")
                .Replace("%TYPENAME%", typename.Trim('\n', '\r'));

        public string CSharpHeader(string @namespace, string content) 
            => GetEmbeddedResource("CSharpHeader.txt")
                .Replace("%NAMESPACE%", @namespace)
                .Replace("%CONTENT%", content.Trim('\n', '\r'));

        public string CSharpClass(string className, string nativeClassName, string dllImportsContent, string apiContent, string nativeLibraryPath, string extraContent = "")
            => GetEmbeddedResource("CSharpClass.txt")
                .Replace("%CLASS_UNSAFE_SAFE%", csClassUnsafeOrSafe)
                .Replace("%CLASS_NAME%", className)
                .Replace("%CCLASS_NAME%", nativeClassName)
                .Replace("%DLLIMPORTS%", dllImportsContent.Trim('\n', '\r'))
                .Replace("%API%", apiContent.Trim('\n', '\r'))
                .Replace("%NATIVE_LIBRARY_PATH%", nativeLibraryPath)
                .Replace("%EXTRA%", extraContent);

        public string CSharpGlobalClass(string dllImportsContent, string apiContent, string nativeLibraryPath, string extraContent = "")
            => GetEmbeddedResource("CSharpGlobalClass.txt")
                .Replace("%CLASS_UNSAFE_SAFE%", csClassUnsafeOrSafe)
                .Replace("%CLASS_NAME%", csGlobalClass)
                .Replace("%DLLIMPORTS%", dllImportsContent.Trim('\n', '\r'))
                .Replace("%API%", apiContent.Trim('\n', '\r'))
                .Replace("%NATIVE_LIBRARY_PATH%", nativeLibraryPath)
                .Replace("%EXTRA%", extraContent);

        public string CSharpEnum(string enumName, string enumItems)
            => GetEmbeddedResource("CSharpEnum.txt")
                .Replace("%ENUM_NAME%", enumName)
                .Replace("%ITEMS%", enumItems.Trim('\n', '\r'));

        public string CSharpEvent(string eventName, string eventDataName, string eventRegisterFunc, string eventUnregisterFunc)
            => GetEmbeddedResource("CSharpEvent.txt")
                .Replace("%EVENTNAME%", eventName)
                .Replace("%EVENTDATANAME%", eventDataName)
                .Replace("%EVENTREGISTERFUNC%", eventRegisterFunc)
                .Replace("%EVENTUNREGISTERFUNC%", eventUnregisterFunc);

        private string GetEmbeddedResource(string file)
        {
            using (Stream stream = typeof(TemplateManager).Assembly.GetManifestResourceStream("CppPinvokeGenerator.Templates." + file))
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    ms.Position = 0;
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
    }
}
