using System;

namespace InsaneOne.SerializeReferenceExtensions.Editor
{
    public interface ITypeCompatibilityPolicy
    {
        bool IsCompatible(Type baseType, Type candiateType);
    }
}