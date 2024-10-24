# CppPinvokeGenerator

This is a fork of [EgorBo/CppPinvokeGenerator](https://github.com/EgorBo/CppPinvokeGenerator):

A simple pinvoke generator based on [xoofx/CppAst](https://github.com/xoofx/CppAst) to generate C# for C/C++ with a few more features to help with binding string types, and for handling more complex type mapping.

Let's say we have a C++ class:
```c++
class Calculator {
public:
     int add(int x, int y);
}
```

Since it's a class and `add` is an instance method, we need to make it DllImport-friendly:

```c++
EXPORTS(Calculator*) Calculator_Calculator() { return new Calculator(); }
EXPORTS(int) Calculator_add(Calculator* target, int x, int y);
```

Now we can easily bind it to C#:

```csharp
public partial class Calculator : SafeHandle
{
    public IntPtr Handle { get; private set; }
    
    // API:
    public Calculator() => this.Handle = Calculator_Calculator();
    public int Add(int x, int y) => Calculator_add(Handle, x, y);
    
    // DllImports
    [DllImport("mylib")] private static extern int Calculator_add(IntPtr handle, int x, int y);
    [DllImport("mylib")] private static extern IntPtr Calculator_Calculator();
}
```

So the generator is able to generate C# classes and the C glue.
As an example - see [samples/SimdJson](https://github.com/EgorBo/CppPinvokeGenerator/tree/master/samples/SimdJson).

# Nuget
```
dotnet add package CppPinvokeGenerator
```
