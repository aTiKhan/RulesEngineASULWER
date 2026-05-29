// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.Json;

namespace RulesEngine.HelperFunctions
{
    public static class Utils
    {
        private static readonly ConcurrentDictionary<string, Type> _typeCache = new();

        public static object GetTypedObject(dynamic input)
        {
            if (input is ExpandoObject)
            {
                Type type = CreateAbstractClassType(input);
                return CreateObject(type, input);
            }
            else
            {
                return input;
            }
        }

        public static Type CreateAbstractClassType(dynamic input)
        {
            List<DynamicProperty> props = new List<DynamicProperty>();

            try
            {
                if (input is JsonElement jsonElement)
                    input = jsonElement.ToExpandoObject();

                if (!(input is ExpandoObject))
                    return input.GetType();

                foreach (var expando in (IDictionary<string, object>)input)
                {
                    Type t;
                    if (expando.Value is IList list)
                    {
                        if (list.Count == 0)
                        {
                            t = typeof(List<Dictionary<string, ImplicitObject>>);
                        }
                        else
                        {
                            var internalType = CreateAbstractClassType(list[0]);
                            t = typeof(List<>).MakeGenericType(internalType);
                        }
                    }
                    else
                    {
                        t = CreateAbstractClassType(expando.Value);
                    }
                    props.Add(new DynamicProperty(expando.Key, t));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating abstract class type: {ex.Message}", ex);
            }

            var cacheKey = GetTypeCacheKey(props);
            return _typeCache.GetOrAdd(cacheKey, _ => DynamicClassFactory.CreateType(props));
        }

        public static object CreateObject(Type type, dynamic input)
        {
            if (input is JsonElement inputElement)
            {
                return CreateObject(type, inputElement.ToExpandoObject());
            }

            if (!(input is ExpandoObject))
            {
                return Convert.ChangeType(input, type);
            }

            object obj = Activator.CreateInstance(type);

            var typeProps = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).ToDictionary(c => c.Name);

            foreach (var expando in (IDictionary<string, object>)input)
            {
                if (typeProps.ContainsKey(expando.Key) && expando.Value != null && !(expando.Value is DBNull))
                {
                    object val;
                    var propInfo = typeProps[expando.Key];
                    if (expando.Value is ExpandoObject)
                    {
                        var propType = propInfo.PropertyType;
                        val = CreateObject(propType, expando.Value);
                    }
                    else if (expando.Value is IList)
                    {
                        var internalType = propInfo.PropertyType.GenericTypeArguments.FirstOrDefault() ?? typeof(object);
                        var temp = (IList)expando.Value;
                        var newList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(internalType));
                        for (int i = 0; i < temp.Count; i++)
                        {
                            var child = CreateObject(internalType, temp[i]);
                            newList.Add(child);
                        }
                        val = newList;
                    }
                    else if (expando.Value is JsonElement expandoElement)
                    {
                        val = CreateObject(propInfo.PropertyType, expandoElement);
                    }
                    else
                    {
                        val = expando.Value;
                    }
                    propInfo.SetValue(obj, val, null);
                }
            }

            return obj;
        }

        private static string GetTypeCacheKey(List<DynamicProperty> props) => string.Join("|", props.Select((p, i) => $"{i}:{p.Name}:{p.Type.FullName}"));
    }
}