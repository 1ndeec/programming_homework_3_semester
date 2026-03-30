// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Generator;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;

/// <summary>
/// Provides reflection-based utilities for generating a compilable-ish C# stub of a type.
/// </summary>
public static class Reflector
{
    /// <summary>
    /// Generates a C# source file named &lt;TypeName&gt;.cs that contains a minimal stub of the given type:
    /// header (visibility/modifiers/kind), base types/interfaces, declared fields, and declared methods.
    /// </summary>
    /// <param name="someClass">The type to reflect and print as a C# stub.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="someClass"/> is null.</exception>
    public static void PrintStructure(Type someClass)
    {
        ArgumentNullException.ThrowIfNull(someClass);

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System.Linq;");

        sb.AppendLine();

        var ns = someClass.Namespace;
        var indent = string.Empty;

        if (!string.IsNullOrWhiteSpace(ns))
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
            indent = "    ";
        }

        sb.AppendLine(indent + BuildTypeHeader(someClass));
        sb.AppendLine(indent + "{");

        var memberIndent = indent + "    ";

        foreach (var f in someClass.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (f.IsSpecialName)
            {
                continue;
            }

            sb.AppendLine(memberIndent + BuildFieldLine(f));
        }

        if (someClass.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Any(f => !f.IsSpecialName))
        {
            sb.AppendLine();
        }

        foreach (var m in someClass.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (m.IsSpecialName)
            {
                continue;
            }

            sb.AppendLine(memberIndent + BuildMethodSignature(m));
            sb.AppendLine(memberIndent + "{");

            WriteMethodBodyPlaceholder(sb, m, memberIndent + "    ");

            sb.AppendLine(memberIndent + "}");
            sb.AppendLine();
        }

        sb.AppendLine(indent + "}");

        if (!string.IsNullOrWhiteSpace(ns))
        {
            sb.AppendLine("}");
        }

        var fileName = GetCleanTypeName(someClass) + ".cs";
        File.WriteAllText(fileName, sb.ToString());
    }

    /// <summary>
    /// Writes differences between two types to the specified output stream.
    /// </summary>
    /// <param name="a">The first type to compare.</param>
    /// <param name="b">The second type to compare.</param>
    /// <param name="output">The stream to which the differences are written.</param>
    public static void DiffClasses(Type a, Type b, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(output);

        output.WriteLine($"=== Diff: {a.FullName} VS {b.FullName} ===");
        output.WriteLine();

        WriteFieldsDiff(a, b, output);
        output.WriteLine();
        WriteMethodsDiff(a, b, output);
    }

    private static string BuildTypeHeader(Type t)
    {
        var vis = GetTypeVisibility(t);

        var kind = t.IsInterface ? "interface" : t.IsEnum ? "enum" : (t.IsValueType && !t.IsPrimitive) ? "struct" : "class";

        var isClass = t.IsClass;

        var mods = new StringBuilder();
        mods.Append(vis);

        if (isClass)
        {
            if (t.IsSealed && t.IsAbstract)
            {
                mods.Append(" static");
            }
            else if (t.IsAbstract)
            {
                mods.Append(" abstract");
            }
            else if (t.IsSealed && !t.IsAbstract)
            {
                mods.Append(" sealed");
            }
        }

        var name = GetCleanTypeName(t);
        var bases = BuildBaseList(t);

        return $"{mods} {kind} {name}{bases}";
    }

    private static string BuildBaseList(Type t)
    {
        if (t.IsEnum)
        {
            return string.Empty;
        }

        var parts = t.GetInterfaces().Select(SimpleTypeName).ToList();

        if (t.BaseType != null && t.BaseType != typeof(object) && !t.IsInterface && !t.IsValueType)
        {
            parts.Insert(0, SimpleTypeName(t.BaseType));
        }

        return parts.Count == 0 ? string.Empty : " : " + string.Join(", ", parts);
    }

    private static string BuildFieldLine(FieldInfo f)
    {
        var vis = GetMemberVisibility(f);
        var mods = string.Empty;

        if (f.IsStatic)
        {
            mods += " static";
        }

        if (f.IsInitOnly)
        {
            mods += " readonly";
        }

        var typeName = SimpleTypeName(f.FieldType);
        return $"{vis}{mods} {typeName} {f.Name};";
    }

    private static string BuildMethodSignature(MethodInfo m)
    {
        var vis = GetMemberVisibility(m);
        var mods = string.Empty;

        if (m.IsStatic)
        {
            mods += " static";
        }

        var returnType = SimpleTypeName(m.ReturnType);

        var parameters = string.Join(", ", m.GetParameters().Select(p =>
        {
            var pt = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
            var refMod = p.IsOut ? "out " : (p.ParameterType.IsByRef ? "ref " : string.Empty);
            if (pt == null)
            {
                pt = typeof(object);
            }

            return $"{refMod}{SimpleTypeName(pt)} {p.Name ?? "arg"}";
        }));

        return $"{vis}{mods} {returnType} {m.Name}({parameters})";
    }

    private static void WriteMethodBodyPlaceholder(StringBuilder sb, MethodInfo m, string indent)
    {
        if (m.ReturnType == typeof(void))
        {
            return;
        }

        if (m.ReturnType.IsValueType)
        {
            sb.AppendLine(indent + "return default;");
            return;
        }

        sb.AppendLine(indent + "return null!;");
    }

    private static string GetTypeVisibility(Type t)
    {
        if (!t.IsNested)
        {
            return t.IsPublic ? "public" : "internal";
        }

        return t switch
        {
            { IsNestedPublic: true } => "public",
            { IsNestedFamily: true } => "protected",
            { IsNestedFamORAssem: true } => "protected internal",
            { IsNestedAssembly: true } => "internal",
            { IsNestedPrivate: true } => "private",
            _ => "internal"
        };
    }

    private static string GetMemberVisibility(FieldInfo f)
    {
        return f switch
        {
            { IsPublic: true } => "public",
            { IsFamily: true } => "protected",
            { IsFamilyOrAssembly: true } => "protected internal",
            { IsAssembly: true } => "internal",
            _ => "private"
        };
    }

    private static string GetMemberVisibility(MethodInfo m)
    {
        return m switch
        {
            { IsPublic: true } => "public",
            { IsFamily: true } => "protected",
            { IsFamilyOrAssembly: true } => "protected internal",
            { IsAssembly: true } => "internal",
            _ => "private"
        };
    }

    private static string GetCleanTypeName(Type t)
    {
        var name = t.Name;
        var tick = name.IndexOf('`');
        if (tick >= 0)
        {
            var arity = int.Parse(name[(tick + 1) ..]);
            name = name[..tick] + "<" + string.Join(", ", Enumerable.Range(1, arity).Select(i => $"T{i}")) + ">";
        }

        return name;
    }

    private static string SimpleTypeName(Type t)
    {
        if (t == typeof(void))
        {
            return "void";
        }

        if (t.IsByRef)
        {
            t = t.GetElementType() !;
        }

        if (t.IsGenericType)
        {
            var defName = t.Name;
            var tick = defName.IndexOf('`');
            if (tick >= 0)
            {
                defName = defName[..tick];
            }

            var args = string.Join(", ", t.GetGenericArguments().Select(SimpleTypeName));
            return $"{defName}<{args}>";
        }

        if (t.IsArray)
        {
            return SimpleTypeName(t.GetElementType() !) + "[]";
        }

        return t.Name;
    }

    private static void WriteFieldsDiff(Type a, Type b, TextWriter output)
    {
        var aFields = a.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(f => !f.IsSpecialName)
            .Select(FieldSignature)
            .ToArray();

        var bFields = b.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(f => !f.IsSpecialName)
            .Select(FieldSignature)
            .ToArray();

        output.WriteLine("--- FIELDS ---");

        output.WriteLine("Only in A:");
        foreach (var x in aFields.Except(bFields))
        {
            output.WriteLine("  " + x);
        }

        output.WriteLine("Only in B:");
        foreach (var x in bFields.Except(aFields))
        {
            output.WriteLine("  " + x);
        }

        output.WriteLine("In both:");
        foreach (var x in aFields.Intersect(bFields))
        {
            output.WriteLine("  " + x);
        }
    }

    private static void WriteMethodsDiff(Type a, Type b, TextWriter output)
    {
        var aMethods = a.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(MethodSignature)
            .ToArray();

        var bMethods = b.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(MethodSignature)
            .ToArray();

        output.WriteLine("--- METHODS ---");

        output.WriteLine("Only in A:");
        foreach (var x in aMethods.Except(bMethods))
        {
            output.WriteLine("  " + x);
        }

        output.WriteLine("Only in B:");
        foreach (var x in bMethods.Except(aMethods))
        {
            output.WriteLine("  " + x);
        }

        output.WriteLine("In both:");
        foreach (var x in aMethods.Intersect(bMethods))
        {
            output.WriteLine("  " + x);
        }
    }

    private static string FieldSignature(FieldInfo f)
    {
        var vis = GetMemberVisibility(f);
        var st = f.IsStatic ? " static" : string.Empty;
        return $"{vis}{st} {f.FieldType.Name} {f.Name}";
    }

    private static string MethodSignature(MethodInfo m)
    {
        var vis = GetMemberVisibility(m);
        var st = m.IsStatic ? " static" : string.Empty;

        var pars = string.Join(", ", m.GetParameters().Select(p =>
        {
            var pt = p.ParameterType.IsByRef
                ? (p.ParameterType.GetElementType() ?? typeof(object))
                : p.ParameterType;

            var refMod = p.IsOut ? "out " : (p.ParameterType.IsByRef ? "ref " : string.Empty);
            return refMod + pt.Name;
        }));

        return $"{vis}{st} {m.ReturnType.Name} {m.Name}({pars})";
    }
}