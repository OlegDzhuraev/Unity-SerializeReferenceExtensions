using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace InsaneOne.SerializeReferenceExtensions.Editor
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        struct TypePopupCache
        {
            public AdvancedTypePopup TypePopup { get; }
            public AdvancedDropdownState State { get; }
            public TypePopupCache(AdvancedTypePopup typePopup, AdvancedDropdownState state)
            {
                TypePopup = typePopup;
                State = state;
            }
        }

        const int MaxTypePopupLineCount = 13;

        static readonly GUIContent NullDisplayName = new(TypeMenuUtility.NullDisplayName);
        static readonly GUIContent IsNotManagedReferenceLabel = new("The property type is not manage reference.");
        static readonly GUIContent TempChildLabel = new();

        readonly Dictionary<string, TypePopupCache> typePopups = new();
        readonly Dictionary<string, GUIContent> typeNameCaches = new();

        SerializedProperty targetProperty;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                // Render label first to avoid label overlap for lists
                var foldoutLabelRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };

                // NOTE: IndentedRect should be disabled as it causes extra indentation.
                //foldoutLabelRect = EditorGUI.IndentedRect(foldoutLabelRect);
                var popupPosition = EditorGUI.PrefixLabel(foldoutLabelRect, label);

#if UNITY_2021_3_OR_NEWER
                // Override the label text with the ToString() of the managed reference.
                var subclassSelectorAttribute = (SubclassSelectorAttribute)attribute;
                if (subclassSelectorAttribute.UseToStringAsLabel && !property.hasMultipleDifferentValues)
                {
                    var managedReferenceValue = property.managedReferenceValue;

                    if (managedReferenceValue != null)
                        label.text = managedReferenceValue.ToString();
                }
#endif

                // Draw the subclass selector popup.
                if (EditorGUI.DropdownButton(popupPosition, GetTypeName(property), FocusType.Keyboard))
                {
                    var popup = GetTypePopup(property);
                    targetProperty = property;
                    popup.TypePopup.Show(popupPosition);
                }

                // Draw the foldout.
                if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
                {
                    var foldoutRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };

#if UNITY_2022_2_OR_NEWER && !UNITY_6000_0_OR_NEWER && !UNITY_2022_3
                    // NOTE: Position x must be adjusted.
                    // FIXME: Is there a more essential solution...?
                    // The most promising is UI Toolkit, but it is currently unable to reproduce all of SubclassSelector features. (Complete provision of contextual menu, e.g.)
                    // 2021.3: No adjustment
                    // 2022.1: No adjustment
                    // 2022.2: Adjustment required
                    // 2022.3: Adjustment required
                    // 2023.1: Adjustment required
                    // 2023.2: Adjustment required
                    // 6000.0: No adjustment
                    foldoutRect.x -= 12;
#endif

                    property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);
                }

                // Draw property if expanded.
                if (property.isExpanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        // Check if a custom property drawer exists for this type.
                        var customDrawer = GetCustomPropertyDrawer(property);
                        if (customDrawer != null)
                        {
                            // Draw the property with custom property drawer.
                            var indentedRect = position;
                            var foldoutDifference = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            indentedRect.height = customDrawer.GetPropertyHeight(property, label);
                            indentedRect.y += foldoutDifference;
                            customDrawer.OnGUI(indentedRect, property, label);
                        }
                        else
                        {
                            // Draw the properties of the child elements.
                            // NOTE: In the following code, since the foldout layout isn't working properly, I'll iterate through the properties of the child elements myself.
                            // EditorGUI.PropertyField(position, property, GUIContent.none, true);

                            var childPosition = position;
                            childPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            foreach (var childProperty in property.GetChildProperties())
                            {
                                var height = EditorGUI.GetPropertyHeight(childProperty, new GUIContent(childProperty.displayName, childProperty.tooltip), true);
                                childPosition.height = height;
                                EditorGUI.PropertyField(childPosition, childProperty, true);

                                childPosition.y += height + EditorGUIUtility.standardVerticalSpacing;
                            }
                        }
                    }
                }
            }
            else
            {
                EditorGUI.LabelField(position, label, IsNotManagedReferenceLabel);
            }

            EditorGUI.EndProperty();
        }

        PropertyDrawer GetCustomPropertyDrawer(SerializedProperty property)
        {
            var propertyType = ManagedReferenceUtility.GetType(property.managedReferenceFullTypename);
            if (propertyType != null && PropertyDrawerCache.TryGetPropertyDrawer(propertyType, out var drawer))
                return drawer;

            return null;
        }

        TypePopupCache GetTypePopup(SerializedProperty property)
        {
            // Cache this string. This property internally call Assembly.GetName, which result in a large allocation.
            var managedReferenceFieldTypename = property.managedReferenceFieldTypename;

            if (!typePopups.TryGetValue(managedReferenceFieldTypename, out var result))
            {
                var state = new AdvancedDropdownState();

                var baseType = ManagedReferenceUtility.GetType(managedReferenceFieldTypename);
                var types = TypeSearchService.TypeCandiateService.GetDisplayableTypes(baseType);
                var popup = new AdvancedTypePopup(
                    types,
                    MaxTypePopupLineCount,
                    state
                );
                popup.OnItemSelected += item =>
                {
                    var type = item.Type;

                    // Apply changes to individual serialized objects.
                    foreach (var targetObject in targetProperty.serializedObject.targetObjects)
                    {
                        var individualObject = new SerializedObject(targetObject);
                        var individualProperty = individualObject.FindProperty(targetProperty.propertyPath);
                        var obj = individualProperty.SetManagedReference(type);
                        individualProperty.isExpanded = (obj != null);

                        individualObject.ApplyModifiedProperties();
                        individualObject.Update();
                    }
                };

                result = new TypePopupCache(popup, state);
                typePopups.Add(managedReferenceFieldTypename, result);
            }
            return result;
        }

        GUIContent GetTypeName(SerializedProperty property)
        {
            var managedReferenceFullTypename = property.managedReferenceFullTypename;

            if (string.IsNullOrEmpty(managedReferenceFullTypename))
                return NullDisplayName;

            if (typeNameCaches.TryGetValue(managedReferenceFullTypename, out var cachedTypeName))
                return cachedTypeName;

            var type = ManagedReferenceUtility.GetType(managedReferenceFullTypename);
            string typeName = null;

            var typeMenu = TypeMenuUtility.GetAttribute(type);
            if (typeMenu != null)
            {
                typeName = typeMenu.GetTypeNameWithoutPath();
                if (!string.IsNullOrWhiteSpace(typeName))
                    typeName = ObjectNames.NicifyVariableName(typeName);

            }

            if (string.IsNullOrWhiteSpace(typeName))
                typeName = ObjectNames.NicifyVariableName(type.Name);

            GUIContent result = new(typeName);
            typeNameCaches.Add(managedReferenceFullTypename, result);
            return result;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded || string.IsNullOrEmpty(property.managedReferenceFullTypename))
                return EditorGUIUtility.singleLineHeight;

            var height = EditorGUIUtility.singleLineHeight;
            height += EditorGUIUtility.standardVerticalSpacing;

            var customDrawer = GetCustomPropertyDrawer(property);
            if (customDrawer != null)
            {
                height += customDrawer.GetPropertyHeight(property, label);
                return height;
            }

            height += GetChildrenHeight(property);

            return height;
        }

        static float GetChildrenHeight(SerializedProperty property)
        {
            var height = 0f;
            var first = true;

            foreach (var child in property.GetChildProperties())
            {
                if (!first)
                    height += EditorGUIUtility.standardVerticalSpacing;

                first = false;

                TempChildLabel.text = child.displayName;
                TempChildLabel.tooltip = child.tooltip;

                height += EditorGUI.GetPropertyHeight(child, TempChildLabel, true);
            }

            return height;
        }
    }
}
