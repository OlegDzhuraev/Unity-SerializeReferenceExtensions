using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace InsaneOne.SerializeReferenceExtensions.Editor
{
    public static class PropertyDrawerCache
    {
        static readonly Dictionary<Type, PropertyDrawer> Caches = new();

        public static bool TryGetPropertyDrawer(Type type, out PropertyDrawer drawer)
        {
            if (!Caches.TryGetValue(type, out drawer))
            {
                var drawerType = GetCustomPropertyDrawerType(type);
                drawer = drawerType != null ? (PropertyDrawer)Activator.CreateInstance(drawerType) : null;
                Caches.Add(type, drawer);
            }

            return drawer != null;
        }

        static Type GetCustomPropertyDrawerType(Type type)
        {
            var interfaceTypes = type.GetInterfaces();

            var types = TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>();
            foreach (var drawerType in types)
            {
                var customPropertyDrawerAttributes = drawerType.GetCustomAttributes(typeof(CustomPropertyDrawer), true);
                foreach (CustomPropertyDrawer customPropertyDrawer in customPropertyDrawerAttributes)
                {
                    var field = customPropertyDrawer.GetType().GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null)
                        continue;

                    if (field.GetValue(customPropertyDrawer) is not Type fieldType)
                        continue;

                    if (fieldType == type)
                        return drawerType;

                    // If the property drawer also allows for being applied to child classes, check if they match
                    var useForChildrenField = customPropertyDrawer.GetType().GetField("m_UseForChildren", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (useForChildrenField == null)
                        continue;

                    var useForChildrenValue = useForChildrenField.GetValue(customPropertyDrawer);
                    if (useForChildrenValue is bool && (bool)useForChildrenValue)
                    {
                        // Check interfaces
                        if (Array.Exists(interfaceTypes, interfaceType => interfaceType == fieldType))
                            return drawerType;

                        // Check derived types
                        var baseType = type.BaseType;
                        while (baseType != null)
                        {
                            if (baseType == fieldType)
                                return drawerType;

                            baseType = baseType.BaseType;
                        }
                    }
                }
            }
            return null;
        }
    }
}