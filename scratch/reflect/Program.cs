using System;
using System.Reflection;

class Program {
    static string managedDir = @"C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed";
    
    static Assembly ResolveAssembly(object sender, ResolveEventArgs args) {
        string name = new AssemblyName(args.Name).Name;
        string path = System.IO.Path.Combine(managedDir, name + ".dll");
        if (System.IO.File.Exists(path))
            return Assembly.ReflectionOnlyLoadFrom(path);
        return null;
    }
    
    static void Main() {
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveAssembly;
        
        var asm = Assembly.ReflectionOnlyLoadFrom(System.IO.Path.Combine(managedDir, "Assembly-CSharp.dll"));
        
        string[] typeNames = new string[] { "DragTool", "DigTool", "DeconstructTool", "CancelTool", "PrioritizeTool", "BuildTool", "Diggable", "Deconstructable", "Prioritizable" };
        
        foreach (var typeName in typeNames) {
            var t = asm.GetType(typeName);
            if (t != null) {
                Console.WriteLine("=== " + typeName + " ===");
                Console.WriteLine("  Base: " + (t.BaseType != null ? t.BaseType.Name : "null"));
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                try {
                    foreach (var m in t.GetMethods(flags)) {
                        string pars = "";
                        foreach (var p in m.GetParameters()) {
                            if (pars.Length > 0) pars += ", ";
                            pars += p.ParameterType.Name + " " + p.Name;
                        }
                        Console.WriteLine("  " + m.ReturnType.Name + " " + m.Name + "(" + pars + ")");
                    }
                } catch (Exception ex) {
                    Console.WriteLine("  Error: " + ex.GetType().Name + " - " + ex.Message);
                }
                Console.WriteLine();
            } else {
                Console.WriteLine("Type not found: " + typeName);
            }
        }
    }
}
