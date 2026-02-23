using System;
using System.Collections.Generic;

namespace InsaneOne.SerializeReferenceExtensions.Editor
{
    public interface ITypeCandiateProvider
    {
        IEnumerable<Type> GetTypeCandidates(Type baseType);
    }
}