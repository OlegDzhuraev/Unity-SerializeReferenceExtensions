using System;
using System.Collections.Generic;
using System.Linq;

namespace InsaneOne.SerializeReferenceExtensions.Editor
{
    public sealed class TypeCandiateService
    {
        readonly ITypeCandiateProvider typeCandiateProvider;
        readonly IIntrinsicTypePolicy intrinsicTypePolicy;
        readonly ITypeCompatibilityPolicy typeCompatibilityPolicy;

        readonly Dictionary<Type, Type[]> typeCache = new();

        public TypeCandiateService(ITypeCandiateProvider typeCandiateProvider, IIntrinsicTypePolicy intrinsicTypePolicy, ITypeCompatibilityPolicy typeCompatibilityPolicy)
        {
            this.typeCandiateProvider = typeCandiateProvider ?? throw new ArgumentNullException(nameof(typeCandiateProvider));
            this.intrinsicTypePolicy = intrinsicTypePolicy ?? throw new ArgumentNullException(nameof(intrinsicTypePolicy));
            this.typeCompatibilityPolicy = typeCompatibilityPolicy ?? throw new ArgumentNullException(nameof(typeCompatibilityPolicy));
        }

        public IReadOnlyList<Type> GetDisplayableTypes(Type baseType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            if (typeCache.TryGetValue(baseType, out var cachedTypes))
                return cachedTypes;

            var candiateTypes = typeCandiateProvider.GetTypeCandidates(baseType);
            var result = candiateTypes
                .Where(intrinsicTypePolicy.IsAllowed)
                .Where(t => typeCompatibilityPolicy.IsCompatible(baseType, t))
                .Distinct()
                .ToArray();

            typeCache.Add(baseType, result);
            return result;
        }
    }
}