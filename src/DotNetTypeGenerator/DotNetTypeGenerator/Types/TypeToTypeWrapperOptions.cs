﻿using System.Reflection;

namespace DotNetTypeGenerator.Types;

public class TypeToTypeWrapperOptions
{
    public List<ParameterConversionRule> ConversionRules { get; set; } = new List<ParameterConversionRule>();
    public string TypeName { get; set; } = "GeneratedType";
    public string NamespaceName { get; set; } = "GeneratedNamespace";
    public List<string> IncludeMethods { get; set; } = new List<string>();
    public List<string> IncludeProperties { get; set; } = new List<string>();
    public Func<TypeToTypeWrapperOptions, Type, MethodInfo, bool> ExcludeMethod { get; set; } = (options, originalType, method) => false;

    public Func<TypeToTypeWrapperOptions, Type, MethodInfo, bool> IncludeMethod { get; set; } = (options, originalType, method) =>
    {
        if (method.IsAbstract) return false;

        if (!method.IsPublic) return false;

        if (method.IsStatic) return false;

        if (method.IsGenericMethodDefinition) return false;

        if (method.IsConstructor) return false;

        if (method.IsSpecialName) return false;

        if (method.DeclaringType != originalType) return false;

        return true;
    };

    public Func<TypeToTypeWrapperOptions, Type, MethodInfo, string> MethodNameGenerator { get; set; } = (options, originalType, methodInfo) => methodInfo.Name;
    public Func<TypeToTypeWrapperOptions, Type, List<Attribute>>? TypeAttributesGenerator { get; set; } = null;
    public Func<TypeToTypeWrapperOptions, Type, string> TypeNameGenerator { get; set; } = (options, originalType) => options.TypeName;
    public Func<TypeToTypeWrapperOptions, Type, string> NamespaceNameGenerator { get; set; } = (options, originalType) => options.NamespaceName;
    public Func<TypeToTypeWrapperOptions, Type, object> Factory { get; set; } = (options, originalType) => Activator.CreateInstance(originalType);
    public Action<TypeToTypeWrapperOptions, Type, object>? OnConstructor { get; set; } = null;
    public Func<TypeToTypeWrapperOptions, Type, string>? OnConstructorCustomCodeGenerator { get; set; } = null;
    public Action<TypeToTypeWrapperOptions, Type, object, MethodInfo>? OnBeforeMethod { get; set; } = null;
    public Func<TypeToTypeWrapperOptions, Type, MethodInfo, string>? OnBeforeMethodCustomCodeGenerator { get; set; } = null;
    public Action<TypeToTypeWrapperOptions, Type, object, MethodInfo>? OnAfterMethod { get; set; } = null;
    public Func<TypeToTypeWrapperOptions, Type, MethodInfo, string>? OnAfterMethodCustomCodeGenerator { get; set; } = null;
    public Func<TypeToTypeWrapperOptions, Type, string>? CustomCodeGenerator { get; set; } = null;
    public List<AdditionalParameter> AdditionalConstructorParameters { get; set; } = new List<AdditionalParameter>();
    public List<string> AdditionalNamespaces { get; set; } = new();
    public List<Assembly> AdditionalReferences { get; set; } = new();
    public bool IsSourceCodeIncluded { get; set; } = true;
    public bool IsSourceCodePrettified { get; set; } = true;
    public Type? Inherits { get; set; } = null;
    public List<Type>? Implements { get; set; } = null;
    public AssemblyGenerator AssemblyGenerator { get; set; } = new AssemblyGenerator();
}

public class AdditionalParameter
{
    public Type Type { get; set; }
    public string Name { get; set; }

    public AdditionalParameter(Type type, string name)
    {
        Type = type;
        Name = name;
    }
}
