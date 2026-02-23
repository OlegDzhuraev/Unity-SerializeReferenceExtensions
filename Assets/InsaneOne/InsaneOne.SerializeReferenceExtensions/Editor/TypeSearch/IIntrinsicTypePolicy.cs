using System;

namespace InsaneOne.SerializeReferenceExtensions.Editor
{
    public interface IIntrinsicTypePolicy
    {
        bool IsAllowed(Type candiateType);
    }
}