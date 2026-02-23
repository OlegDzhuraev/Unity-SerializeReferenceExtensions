using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace InsaneOne.SerializeReferenceExtensions.Editor
{
    public static class TypeMenuUtility
    {
        public const string NullDisplayName = "<null>";

        public static AddTypeMenuAttribute GetAttribute(Type type)
        {
            return Attribute.GetCustomAttribute(type, typeof(AddTypeMenuAttribute)) as AddTypeMenuAttribute;
        }

        public static string[] GetSplittedTypePath(Type type)
        {
            var typeMenu = GetAttribute(type);

            if (typeMenu != null)
                return typeMenu.GetSplittedMenuName();

            Debug.Assert(type.FullName != null, "type.FullName != null");

            var splitIndex = type.FullName.LastIndexOf('.');
            return splitIndex >= 0 ? new[] { type.FullName[..splitIndex], type.FullName[(splitIndex + 1)..] } : new[] { type.Name };
        }

        public static IEnumerable<Type> OrderByType(this IEnumerable<Type> source)
        {
            return source.OrderBy(type =>
            {
                if (type == null)
                    return -999;

                return GetAttribute(type)?.Order ?? 0;
            }).ThenBy(type =>
            {
                if (type == null)
                    return null;

                return GetAttribute(type)?.MenuName ?? type.Name;
            });
        }

    }
}