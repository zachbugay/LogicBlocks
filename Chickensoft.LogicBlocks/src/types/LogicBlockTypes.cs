namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Chickensoft.LogicBlocks.Types;

/// <summary>
/// LogicBlock type hierarchy lookup system — find and cache types by their
/// base type or ancestor without reflection, using the LogicBlocks Type
/// Generator output.
/// <br />
/// Be sure to call <see cref="Initialize"/> with the generated type registry
/// before using the other methods in this class.
/// </summary>
public static class LogicBlockTypes {
  private static readonly Dictionary<Type, HashSet<Type>> _typesByBaseType =
    new();
  private static readonly Dictionary<Type, HashSet<Type>> _typesByAncestor =
    new();
  private static readonly Dictionary<Type, JsonTypeInfo> _jsonTypeInfosByType =
    new();
  private static readonly Dictionary<Type, List<LogicProp>> _propertiesByType =
    new();

  /// <summary>
  /// This caches the types shown in the generated registry by their base type
  /// for more performant type-hierarchy lookups later.
  /// </summary>
  /// <param name="registry">Type registry generated by the
  /// LogicBlocksTypeGenerator.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Initialize(ITypeRegistry registry) =>
    ComputeTypesByBaseType(registry);

  /// <summary>
  /// Gets all the derived types of the given type. Does not work for
  /// interfaces. Caches the derived types for speedy lookups later.
  /// </summary>
  /// <param name="type">Ancestor type.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static HashSet<Type> GetDescendants(Type type) {
    CacheDescendants(type);
    return _typesByAncestor[type];
  }

  /// <summary>
  /// Gets all the properties of the given metatype, including the properties
  /// inherited from any base metatypes that the type extends.
  /// </summary>
  /// <param name="registry">Type registry generated by the
  /// LogicBlocksTypeGenerator.</param>
  /// <param name="type"></param>
  /// <returns>All properties of the metatype, including the properties
  /// inherited from any base metatypes that the type extends.</returns>
  public static IEnumerable<LogicProp> GetProperties(
    ITypeRegistry registry, Type type
  ) {
    if (_propertiesByType.TryGetValue(type, out var properties)) {
      return properties;
    }

    GetMetatype(registry, type); // Validate type is metatype.

    properties = new List<LogicProp>();
    var currentType = type;

    do {
      if (registry.Metatypes.TryGetValue(currentType, out var metatype)) {
        foreach (var property in metatype.Properties) {
          properties.Add(property);
        }
      }

      currentType = currentType.BaseType;
    } while (currentType != null);

    return properties;
  }

  /// <summary>
  /// Creates (or retrieves from cache) a JsonTypeInfo instance for the given
  /// type, using the provided type registry and serializer options. If the type
  /// is not a metatype tagged with the <see cref="LogicModelAttribute"/>, this
  /// method will throw an exception.
  /// </summary>
  /// <param name="registry">Type registry generated by the
  /// LogicBlocksTypeGenerator.</param>
  /// <param name="options">Json serializer options.</param>
  /// <param name="type">System type.</param>
  /// <returns>Json type info for the provided type's metatype.</returns>
  public static JsonTypeInfo CreateJsonTypeInfo(
    ITypeRegistry registry,
    JsonSerializerOptions options,
    Type type
  ) {
    if (_jsonTypeInfosByType.TryGetValue(type, out var jsonTypeInfo)) {
      return jsonTypeInfo;
    }

    var metatype = GetMetatype(registry, type);

    // We can safely disregard AOT warnings since we know the types will not
    // be trimmed since they are registered in the generated type registry by
    // the logic blocks generator, in addition to the fact that we are
    // providing type information for all serializable, non-primitive types.
    // #pragma warning disable IL2026, IL3050
    jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, options);

    // Allow the json serializer to construct an instance of the type.
    jsonTypeInfo.CreateObject = registry.VisibleInstantiableTypes[type];

    foreach (var property in metatype.Properties) {
      // Look at the property metadata generated by the logic blocks generator
      // and construct the relevant JsonPropertyInfo for each property.
      var jsonProp = jsonTypeInfo.CreateJsonPropertyInfo(
        property.Type, property.Name
      );
      jsonProp.IsRequired = false;
      jsonProp.Get = property.Getter;
      jsonProp.Set = property.Setter;

      jsonTypeInfo.Properties.Add(jsonProp);
    }

    // Cache type info so we don't construct it more than once.
    _jsonTypeInfosByType[type] = jsonTypeInfo;

    return jsonTypeInfo;
    // #pragma warning restore IL2026, IL3050
  }


  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static void CacheDescendants(Type type) {
    if (_typesByAncestor.ContainsKey(type)) { return; }

    _typesByAncestor.Add(type, FindDescendants(type));
  }

  private static HashSet<Type> FindDescendants(Type type) {
    var descendants = new HashSet<Type>();
    var queue = new Queue<Type>();
    queue.Enqueue(type);

    while (queue.Count > 0) {
      var current = queue.Dequeue();
      descendants.Add(current);

      if (_typesByBaseType.TryGetValue(current, out var children)) {
        foreach (var child in children) {
          queue.Enqueue(child);
        }
      }
    }

    descendants.Remove(type);

    return descendants;
  }

  private static void ComputeTypesByBaseType(ITypeRegistry registry) {
    foreach (var type in registry.VisibleTypes) {
      if (type.BaseType is not Type baseType) { continue; }

      if (_typesByBaseType.TryGetValue(baseType, out var existingSet)) {
        existingSet.Add(type);
      }
      else {
        var set = new HashSet<Type>();
        set.Add(type);
        _typesByBaseType.Add(baseType, set);
      }
    }
  }

  private static IMetatype GetMetatype(ITypeRegistry registry, Type type) {
    if (!registry.Metatypes.TryGetValue(type, out var metatype)) {
      throw new InvalidOperationException(
        $"Type {type} is not a metatype. Metatypes must be tagged with the " +
        "[LogicModel] attribute to allow the LogicBlocks generator to " +
        "generate information about the properties of the type."
      );
    }

    return metatype;
  }
}
