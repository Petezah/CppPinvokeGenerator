// This file is auto-generated (EgorBo/CppPinvokeGenerator). Do not edit.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace TestAPI
{
    public partial class StringVector : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region Handle
        /// <summary>
        /// Pointer to the underlying native object
        /// </summary>
        public IntPtr Handle => DangerousGetHandle();

        /// <summary>
        /// Create StringVector from a native pointer
        /// if letGcDeleteNativeObject is false GC won't release the underlying object (use ONLY if you know it will be handled by some other native object)
        /// </summary>
        public StringVector(IntPtr handle, bool letGcDeleteNativeObject) : base(letGcDeleteNativeObject) => SetHandle(handle);
        #endregion

        #region API
        public StringVector() : base(ownsHandle: true) => SetHandle(StringVector_StringVector_0());

        public StringVector(StringVector src) : base(ownsHandle: true) => SetHandle(StringVector_StringVector_S((src == null ? IntPtr.Zero : src.Handle)));

        public void Add(string item) => StringVector_Add_s(Handle, item);

        public string Get(Int64 index) { int buffer_len = 255; var buffer = new System.Text.StringBuilder(buffer_len); StringVector_Get_s(Handle, (IntPtr)index, buffer, buffer_len); return buffer.ToString(); }

        public Int64 Size() => (Int64)(StringVector_Size_0(Handle));
        #endregion

        #region DllImports
        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr StringVector_StringVector_0();

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr StringVector_StringVector_S(IntPtr src);

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void StringVector_Add_s(IntPtr target, string item);

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void StringVector_Get_s(IntPtr target, IntPtr/*size_t*/ index, System.Text.StringBuilder buffer, Int32 buffer_len);

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ StringVector_Size_0(IntPtr target);
        #endregion

        #region ReleaseHandle
        protected override bool ReleaseHandle()
        {
            StringVector__delete(Handle);
            return true;
        }

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void StringVector__delete(IntPtr target);
        #endregion
    }
}
