// This file is auto-generated (EgorBo/CppPinvokeGenerator). Do not edit.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace TestAPI
{
    public partial class UserGroup : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region Handle
        /// <summary>
        /// Pointer to the underlying native object
        /// </summary>
        public IntPtr Handle => DangerousGetHandle();

        /// <summary>
        /// Create UserGroup from a native pointer
        /// if letGcDeleteNativeObject is false GC won't release the underlying object (use ONLY if you know it will be handled by some other native object)
        /// </summary>
        public UserGroup(IntPtr handle, bool letGcDeleteNativeObject) : base(letGcDeleteNativeObject) => SetHandle(handle);
        #endregion

        #region API
        public UserGroup() : base(ownsHandle: true) => SetHandle(UserGroup_UserGroup_0());

        public StringVector Users
        {
            get => GetUsers();
            set => SetUsers(value);
        }
        
        private StringVector GetUsers() => new StringVector(UserGroup_GetUsers_0(Handle), false);

        private void SetUsers(StringVector users) => UserGroup_SetUsers_S(Handle, (users == null ? IntPtr.Zero : users.Handle));

        public string GroupName
        {
            get => GetGroupName();
            set => SetGroupName(value);
        }
        
        private string GetGroupName() { int buffer_len = 255; var buffer = new System.Text.StringBuilder(buffer_len); UserGroup_GetGroupName_0(Handle, buffer, buffer_len); return buffer.ToString(); }

        private void SetGroupName(string groupName) => UserGroup_SetGroupName_s(Handle, groupName);

        public Boolean GroupIsActive
        {
            get => GetGroupIsActive();
            set => SetGroupIsActive(value);
        }
        
        private Boolean GetGroupIsActive() => UserGroup_GetGroupIsActive_0(Handle) > 0;

        private void SetGroupIsActive(Boolean active) => UserGroup_SetGroupIsActive_b(Handle, (Byte)(active ? 1 : 0));
        #endregion

        #region DllImports
        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr UserGroup_UserGroup_0();

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr UserGroup_GetUsers_0(IntPtr target);

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void UserGroup_SetUsers_S(IntPtr target, IntPtr users);

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void UserGroup_GetGroupName_0(IntPtr target, System.Text.StringBuilder buffer, Int32 buffer_len);

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void UserGroup_SetGroupName_s(IntPtr target, string groupName);

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ UserGroup_GetGroupIsActive_0(IntPtr target);

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void UserGroup_SetGroupIsActive_b(IntPtr target, Byte/*bool*/ active);
        #endregion

        #region ReleaseHandle
        protected override bool ReleaseHandle()
        {
            UserGroup__delete(Handle);
            return true;
        }

        [DllImport(TestAPIN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void UserGroup__delete(IntPtr target);
        #endregion
    }
}