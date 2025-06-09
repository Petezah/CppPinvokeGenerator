using CppAst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppPinvokeGenerator
{
    public class EventHelper
    {
        private TypeMapper _mapper;
        private CppFunction _registerCallbackFunc;
        private CppFunction _unregisterCallbackFunc;
        private string _eventClassName;
        private string _eventDataClassName;

        public delegate (string eventClassName, string eventDataClassName) GetEventClassNamesDelegate(CppClass cppClass);

        public EventHelper(TypeMapper mapper, CppClass cppClass, List<CppFunction> functions, GetEventClassNamesDelegate getEventClassNames)
        {
            _mapper = mapper;
            _registerCallbackFunc = functions.FirstOrDefault(IsRegisterCallbackFunction);
            _unregisterCallbackFunc = functions.FirstOrDefault(IsUnRegisterCallbackFunction);
            if (getEventClassNames != null && cppClass != null)
            {
                (_eventClassName, _eventDataClassName) = getEventClassNames(cppClass);
            }
        }

        public bool CanGenerateEvent
        {
            get
            {
                return _registerCallbackFunc != null 
                    && _unregisterCallbackFunc != null
                    && _eventClassName != null 
                    && _eventDataClassName != null;
            }
        }

        public string EventName => _eventClassName;
        public string EventDataName => _eventDataClassName;
        public string EventRegisterFunc => _mapper.RenameForApi(_registerCallbackFunc.Name, isMethod: true);
        public string EventUnRegisterFunc => _mapper.RenameForApi(_unregisterCallbackFunc.Name, isMethod: true);

        private bool IsRegisterCallbackFunction(CppFunction function)
        {
            var managedName = _mapper.RenameForApi(function.Name, isMethod: true);
            if (managedName.ToLower().Contains("register")
                && function.Parameters.Count == 1
                && _mapper.MapToManagedApiType(function.Parameters[0].Type, isReturnValue: false) == "IntPtr"
                && function.ReturnType.GetDisplayName() != "void")
            {
                return true;
            }
            return false;
        }

        private bool IsUnRegisterCallbackFunction(CppFunction function)
        {
            var managedName = _mapper.RenameForApi(function.Name, isMethod: true);
            if (managedName.ToLower().Contains("unregister")
                && function.Parameters.Count == 1
                && _registerCallbackFunc != null
                && function.Parameters[0].Type == _registerCallbackFunc.ReturnType)
            {
                return true;
            }
            return false;
        }
    }
}
