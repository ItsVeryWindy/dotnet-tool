// See https://aka.ms/new-console-template for more information
using ToolLibrary;
using ToolLibrary.Abstractions;

var assemblyPath = args[0];
var nugetPath = args[1];

Console.WriteLine($"Hello, World! {assemblyPath} {nugetPath}");

var context = new CustomAssemblyLoadContext(assemblyPath, nugetPath);

//Debugger.Launch();

using var scope = context.EnterContextualReflection();

var toolLibraryAssembly = context.LoadFromAssemblyPath(typeof(Class1).Assembly.Location);

var class1 = (IClass1)toolLibraryAssembly.CreateInstance(typeof(Class1).FullName!)!;

var names = class1.Do(context.Assembly);

if (names is null)
    return;

foreach (var name in names)
{
    Console.WriteLine($"Health Check Name: {name}");
}
