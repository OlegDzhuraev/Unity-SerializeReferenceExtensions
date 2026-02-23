using System;

namespace InsaneOne.SerializeReferenceExtensions.Editor
{
    public sealed class DefaultTypeCompatibilityPolicy : ITypeCompatibilityPolicy
    {
        public static readonly DefaultTypeCompatibilityPolicy Instance = new();

        public bool IsCompatible(Type baseType, Type candiateType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            if (candiateType == null)
                throw new ArgumentNullException(nameof(candiateType));

            if (baseType.IsGenericTypeDefinition || baseType.ContainsGenericParameters)
                return false;

            return baseType.IsAssignableFrom(candiateType);
        }
    }
}