﻿using System.Collections.Generic;
using System.Linq;
using Custom.Decompiler.ILAst;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

public static class CecilExtensions
{
    public static IEnumerable<MethodDefinition> MethodsWithBody(this TypeDefinition type)
    {
        return type.Methods.Where(x => x.Body != null);
    }

    public static IEnumerable<PropertyDefinition> ConcreteProperties(this TypeDefinition type)
    {
        return type.Properties.Where(x => (x.GetMethod == null || !x.GetMethod.IsAbstract) && (x.SetMethod == null || !x.SetMethod.IsAbstract));
    }

    public static bool HasInterface(this TypeReference type, string interfaceFullName)
    {
        if (type == null)
            return false;

        if (type.IsGenericParameter)
        {
            var genericType = (GenericParameter)type;
            return genericType.Constraints.Any(t => t.HasInterface(interfaceFullName));
        }

        var resolved = type.Resolve();

        if (resolved == null)
            return false;

        return (resolved.Interfaces != null && resolved.Interfaces.Any(i => i.FullName != null && i.FullName.Equals(interfaceFullName)))
                || resolved.BaseType.HasInterface(interfaceFullName);
    }

    public static void InsertBefore(this ILProcessor processor, Instruction target, params Instruction[] instructions)
    {
        foreach (var instruction in instructions)
            processor.InsertBefore(target, instruction);
    }

    public static void InsertAfter(this ILProcessor processor, Instruction target, params Instruction[] instructions)
    {
        foreach (var instruction in instructions.Reverse())
            processor.InsertAfter(target, instruction);
    }

    public static IEnumerable<Instruction> WithinRange(this Collection<Instruction> instructions, ILRange range)
    {
        return instructions.Where(i => i.Offset >= range.From && i.Offset <= range.To);
    }

    public static Instruction AtOffset(this Collection<Instruction> instructions, int offset)
    {
        return instructions.First(i => i.Offset == offset);
    }

    public static Instruction BeforeOffset(this Collection<Instruction> instructions, int offset)
    {
        return instructions.Last(i => i.Offset < offset);
    }

    public static IEnumerable<ILRange> GetILRanges(this ILExpression expression)
    {
        return expression.ILRanges.Concat(expression.Original.SelectMany(exp => GetILRanges(exp)));
    }

    public static int FirstILOffset(this ILNode node)
    {
        return node.GetSelfAndChildrenRecursive<ILExpression>()
            .SelectMany(exp => exp.GetILRanges())
            .Min(ilr => ilr.From);
    }

    public static int LastILOffset(this ILNode node)
    {
        return node.GetSelfAndChildrenRecursive<ILExpression>()
            .SelectMany(exp => exp.GetILRanges())
            .DefaultIfEmpty(new ILRange { To = -1 })
            .Max(ilr => ilr.To);
    }

    public static void ReplaceCollection<T>(this Collection<T> collection, IEnumerable<T> source)
    {
        var items = source.ToList();
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }

    public static bool IsAsyncStateMachine(this ICustomAttributeProvider value)
    {
        return value.CustomAttributes.Any(a => a.AttributeType.Name == "AsyncStateMachineAttribute");
    }

    public static bool IsIAsyncStateMachine(this TypeDefinition typeDefinition)
    {
        return typeDefinition.Interfaces.Any(x => x.Name == "IAsyncStateMachine");
    }
}