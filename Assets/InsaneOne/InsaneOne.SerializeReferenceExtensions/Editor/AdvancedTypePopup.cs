using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace InsaneOne.SerializeReferenceExtensions.Editor
{
    public class AdvancedTypePopupItem : AdvancedDropdownItem
    {
        public Type Type { get; }

        public AdvancedTypePopupItem(Type type, string name) : base(name)
        {
            Type = type;
        }
    }

    /// <summary> A type popup with a fuzzy finder. </summary>
    public class AdvancedTypePopup : AdvancedDropdown
    {
        const int MaxNamespaceNestCount = 16;

        public static void AddTo(AdvancedDropdownItem root, IEnumerable<Type> types)
        {
            var itemCount = 0;

            var nullItem = new AdvancedTypePopupItem(null, TypeMenuUtility.NullDisplayName)
            {
                id = itemCount++,
            };
            root.AddChild(nullItem);

            var typeArray = types.OrderByType().ToArray();

            // Single namespace if the root has one namespace and the nest is unbranched.
            var isSingleNamespace = true;
            var namespaces = new string[MaxNamespaceNestCount];
            foreach (var type in typeArray)
            {
                var splittedTypePath = TypeMenuUtility.GetSplittedTypePath(type);
                if (splittedTypePath.Length <= 1)
                    continue;

                // If they explicitly want sub category, let them do.
                if (TypeMenuUtility.GetAttribute(type) != null)
                {
                    isSingleNamespace = false;
                    break;
                }

                for (var k = 0; splittedTypePath.Length - 1 > k; k++)
                {
                    var ns = namespaces[k];
                    if (ns == null)
                        namespaces[k] = splittedTypePath[k];

                    else if (ns != splittedTypePath[k])
                    {
                        isSingleNamespace = false;
                        break;
                    }
                }

                if (!isSingleNamespace)
                    break;
            }

            // Add type items.
            foreach (var type in typeArray)
            {
                var splittedTypePath = TypeMenuUtility.GetSplittedTypePath(type);
                if (splittedTypePath.Length == 0)
                    continue;

                var parent = root;

                // Add namespace items.
                if (!isSingleNamespace)
                {
                    for (var k = 0; (splittedTypePath.Length - 1) > k; k++)
                    {
                        var foundItem = GetItem(parent, splittedTypePath[k]);
                        if (foundItem != null)
                        {
                            parent = foundItem;
                        }
                        else
                        {
                            var newItem = new AdvancedDropdownItem(splittedTypePath[k])
                            {
                                id = itemCount++
                            };
                            parent.AddChild(newItem);
                            parent = newItem;
                        }
                    }
                }

                // Add type item.
                var item = new AdvancedTypePopupItem(type, ObjectNames.NicifyVariableName(splittedTypePath[^1]))
                {
                    id = itemCount++,
                };
                parent.AddChild(item);
            }
        }

        static AdvancedDropdownItem GetItem(AdvancedDropdownItem parent, string name)
        {
            foreach (var item in parent.children)
            {
                if (item.name == name)
                    return item;
            }

            return null;
        }

        static readonly float HeaderHeight = EditorGUIUtility.singleLineHeight * 2f;

        Type[] types;

        public event Action<AdvancedTypePopupItem> OnItemSelected;

        public AdvancedTypePopup(IEnumerable<Type> types, int maxLineCount, AdvancedDropdownState state) : base(state)
        {
            SetTypes(types);
            minimumSize = new Vector2(minimumSize.x, EditorGUIUtility.singleLineHeight * maxLineCount + HeaderHeight);
        }

        public void SetTypes(IEnumerable<Type> types)
        {
            this.types = types.ToArray();
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select Type");
            AddTo(root, types);
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            if (item is AdvancedTypePopupItem typePopupItem)
                OnItemSelected?.Invoke(typePopupItem);
        }
    }
}