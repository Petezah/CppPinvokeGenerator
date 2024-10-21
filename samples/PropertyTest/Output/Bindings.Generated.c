// This file is auto-generated (EgorBo/CppPinvokeGenerator). Do not edit.

#if (defined WIN32 || defined _WIN32)
#define EXPORTS(returntype) extern "C" __declspec(dllexport) returntype __cdecl
#else
#define EXPORTS(returntype) extern "C" __attribute__((visibility("default"))) returntype
#endif

#include "TestAPI.h"


/************* StringVector *************/

//NOT_BOUND:                    public StringVector(StringVector && src)
//NOT_BOUND:                    public StringVector(StringVectorBase const& src)
//NOT_BOUND:                    public StringVector(StringVectorBase && src)
//NOT_BOUND:                    public StringVector& operator=(StringVector const&)
EXPORTS(StringVector*)          StringVector_StringVector_0() { return new StringVector(); }
EXPORTS(StringVector*)          StringVector_StringVector_S(StringVector src) { return new StringVector(src); }
EXPORTS(void)                   StringVector_Add_s(StringVector* target, const char* item) { auto demo_string_param = item; target->Add(demo_string_param); }
EXPORTS(void)                   StringVector_Get_s(StringVector* target, size_t index, char* buffer, int buffer_len) { auto native_str = target->Get(index); strncpy(buffer, native_str.c_str(), buffer_len);  }
EXPORTS(size_t)                 StringVector_Size_0(StringVector* target) { return target->Size(); }
EXPORTS(void)                   StringVector__delete(StringVector* target) { delete target; }


/************* UserGroup *************/

EXPORTS(UserGroup*)             UserGroup_UserGroup_0() { return new UserGroup(); }
EXPORTS(StringVector const&)    UserGroup_GetUsers_0(UserGroup* target) { return target->GetUsers(); }
EXPORTS(void)                   UserGroup_SetUsers_S(UserGroup* target, StringVector users) { target->SetUsers(users); }
EXPORTS(void)                   UserGroup_GetGroupName_0(UserGroup* target, char* buffer, int buffer_len) { auto native_str = target->GetGroupName(); strncpy(buffer, native_str.c_str(), buffer_len);  }
EXPORTS(void)                   UserGroup_SetGroupName_s(UserGroup* target, const char* groupName) { auto demo_string_param = groupName; target->SetGroupName(demo_string_param); }
EXPORTS(bool)                   UserGroup_GetGroupIsActive_0(UserGroup* target) { return target->GetGroupIsActive(); }
EXPORTS(void)                   UserGroup_SetGroupIsActive_b(UserGroup* target, bool active) { target->SetGroupIsActive(active); }
EXPORTS(void)                   UserGroup__delete(UserGroup* target) { delete target; }


/************* Global functions: *************/
