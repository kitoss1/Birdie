# C# Coding Style

The general rule we follow is "use Visual Studio defaults" with modifications from General Puzzle Studio code practices.

## Editor Support

An [EditorConfig](https://editorconfig.org "EditorConfig homepage") file (`.editorconfig`) has been provided at the root of the runtime repository, enabling C# auto-formatting conforming to the guidelines.

For non-C# files (xml, bat, sh, etc.), our current best guidance is consistency. When editing files, keep new code and changes consistent with the style in the files. For new files, it should conform to the style for that kind of files. If there is a completely new type of file, anything that is reasonably broadly accepted is fine.

## Analyzer Rules

Code style rules are defined in `Assets/Default.ruleset` which includes:
- **StyleCop analyzers** for consistent code style
- **Microsoft (Roslyn) analyzers** for code quality
- **Unity analyzers** for Unity-specific best practices
- **AsyncFixer** for async/await pattern enforcement

## General Puzzle Studio Code Practices

Following [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) **MUST** be followed by all projects, with these modifications:
- Use `m_` prefix for member variables instead of the suggested `_` prefix
- XML format comments for public variables are not to be used

For efficient code practices, see: [C# Performance Guidelines](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/performance/)

## Code Organization & Limits

To improve code readability and maintainability, all new code written **MUST**:

### File and Class Structure
- **Classes maximum 500 lines of code**
- **Methods maximum 50 lines of code**
- **Every class/interface defined in their own file**
- **Use properties instead of public member variables** in classes
- **Not use the `partial` keyword** anywhere in code
- **Not use the `#region` directive** anywhere in code
- **Not have public nested classes/enums**

### Interface Usage
- **Be accessed through interface** when accessed outside of the namespace
- This promotes loose coupling and better testability

### Naming Requirements
- **Use naming that explains the functionality** of the class/method/variable properly
- Names must be: **Specific**, **Understandable**, **Meaningful**, **Searchable**, **Consistent**

## Code Quality Principles

All new code written **SHOULD**:

- **Follow [SOLID principles](https://en.wikipedia.org/wiki/SOLID)**
- **Have no need for comments explaining how the code works** (self-documenting)
- **Have no code blocks copied around** but extracted into separate methods
- **Be easy to understand by any programmer**

## Namespaces

### Namespaces should be properly organized
`using` statements go at the top of the file and should be sorted alphabetically, except for the `System` namespace which should be on top of all others.
```
using System;
using System.Collections.Generic;
```

### All code goes into namespaces
Format is `Birdie.<MainFeature>.<Feature>`. Sub-namespaces are allowed.

We move and add new code into correct namespaces.
```
namespace Birdie.Meta.Popups
{
    public class SomeClass
    {
    }
}

namespace Birdie.Core.LevelData
{
    public class LevelGoalsData
    {
    }
}
```

## Braces

### We use [Allman style](http://en.wikipedia.org/wiki/Indent_style#Allman_style) braces
```
while (x == y)
{
    DoSomething();
    DoSomethingElse();
}
```

### Use braces in single-statement control blocks
```
// do
if (x == y)
{
    DoSomething();
}

// don't
if (x == y)
    DoSomething();
```

### Nested `using` statements _can_ be written without braces or indentation
```
using (var stream = new FileStream(...))
using (var reader = new StreamReader(stream))
{
    // do something
}
```

## Fields

### Respect file's current style
If a file happens to differ in style from these guidelines (e.g. private members are named `_member` rather than `m_member`), the existing style in that file takes precedence. This does not apply to files that do not have a consistent style to begin with.

### Use prefixed camelcase
```
// use m_ for instance fields for internal or less visibility
internal int m_foo;
protected int m_bar;
private int m_baz;
[SerializeField] private Camera m_fooCamera;

// use s_ for static fields
private static int s_foo;

```

### Use properties instead of public fields
```
// do - use properties
public int Foo { get; set; }

// don't - avoid public fields
public int Foo;
```

If public fields are absolutely necessary (e.g., serialization), use PascalCasing:
```
[SerializeField] public int ExceptionCase; // Only when required
```

### Use `readonly` where possible
```
private readonly int m_foo;
```

### Use the `static` keyword first and `readonly` after
```
private static readonly int s_foo;
```

### Use PascalCasing for constants
Exceptions can be made for interop code where the constant name should match exactly.
```
public const int FooBar;
```

### Fields are declared at the top
```
// do
public class SomeClass
{
    private int m_foo;
    private int m_bar;

    public void DoSomething()
    {
    }
}

// don't
public class SomeClass
{
    private int m_foo;

    public void DoSomething()
    {
    }

    private int m_bar;
}
```

## Use PascalCasing for function names
```
public void DoSomething()
{
}

// including local ones
public void DoSomething()
{
    void DoSomethingLocal()
    {
    }
}
```

## Avoid using 'this' unless necessary
```
private void Update()
{
    // do
    transform.position = Vector3.zero;

    // don't
    this.transform.position = Vector3.zero;
}
```

## Always specify visibility
```
// do
private int m_foo;

private void DoSomething()
{
}

// don't
int m_foo;

void DoSomething()
{
}
```

## Visibility modifier always goes first
```
// do
public abstract class SomeClass
{
    public abstract void DoSomething();
}

// don't
abstract public class SomeClass
{
    abstract public void DoSomething();
}
```

## Use four spaces for indentation instead of tabs
```
// do
while (x == y)
{
    // do something
}

// don't
while (x == y)
{
[\t]// do something
}
```

## Avoid more than one empty line at any time
```
// do
public SomeClass()
{
    private int m_foo;

    public SomeClass()
    {
    }
}

// don't
public SomeClass()
{
    private int m_foo;


    public SomeClass()
    {
    }
}
```

## Use 'var' on the left side of assignment only if explicit type is on the right side
```
// do
var stream = new FileStream(...);

// don't
var stream = OpenStandardInput();
```

## Use language keywords instead of BCL types
```
// do
int foo = 0;
string bar = "Hello";
float baz = .0f;
int.Parse("42");

// don't
Int32 foo = 0;
String bar = "Hello";
Single baz = .0f;
Int32.Parse("42");
```

## Avoid hardcoded class, method, field or variable names when possible
```
// do
string errorMessage = $"Error on {nameof(SomeClass)}.{nameof(SomeMethod)}";

// don't
string errorMessage = "Error on SomeClass.SomeMethod";
```

## All internal and private types should be static or sealed unless derivation from them is required
```
internal static class SomeClass
{
}

internal sealed class SomeClass
{
}
```

## Comments go on separate lines
```
// do
// this is a properly placed comment
int x = 0;

// don't
int foo = 0; // this is a wrongly placed comment
```

## Async and coroutine methods should be suffixed
```
private async Task DoSomethingAsync()
{
}

private IEnumerator DoSomethingCoroutine()
{
}
```

## Event fields contain a past tense verb and event handlers start with 'On'
 ```
// Event name, no prefix
public event EventHandler<ShapeEventArgs> ShapeChanged;

// Event Handler, with On- prefix
void OnShapeChanged(ShapeEventArgs e) { }
```

## When you add the tag [SerializeField] to a var it should go to the same line and not above
```
// do
    [SerializeField] private int m_foo;

// don't
	[SerializeField]
	private int m_foo;

```