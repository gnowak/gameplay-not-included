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
        
        var asm = Assembly.ReflectionOnlyLoadFrom(System.IO.Path.Combine(managedDir, "Assembly-CSharp-firstpass.dll"));
        
        try {
            foreach (var type in asm.GetTypes()) {
                CheckType(type);
            }
        } catch (ReflectionTypeLoadException ex) {
            foreach (var type in ex.Types) {
                if (type != null) {
                    CheckType(type);
                }
            }
        }
    }
    
    static void CheckType(Type type) {
        if (type.Name.IndexOf("Tame", StringComparison.OrdinalIgnoreCase) >= 0 ||
            type.Name.IndexOf("Wild", StringComparison.OrdinalIgnoreCase) >= 0) {
            Console.WriteLine("Found type firstpass: " + type.FullName);
        }
    }
}
