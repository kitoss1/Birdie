using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Birdie.Editor
{
    [CustomPropertyDrawer(typeof(Data.MinigameDifficultySettings), true)]
    public sealed class SerializeReferenceDrawer : PropertyDrawer
    {
        private static readonly Dictionary<Type, List<Type>> s_typeCache = new Dictionary<Type, List<Type>>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null)
            {
                foreach (SerializedProperty child in GetVisibleChildren(property))
                {
                    height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            List<Type> derivedTypes = GetDerivedTypes(typeof(Data.MinigameDifficultySettings));

            Type currentType = property.managedReferenceValue?.GetType();
            string[] typeNames = new string[derivedTypes.Count + 1];
            typeNames[0] = "(None)";

            int selectedIndex = 0;
            for (int i = 0; i < derivedTypes.Count; i++)
            {
                typeNames[i + 1] = ObjectNames.NicifyVariableName(derivedTypes[i].Name);
                if (currentType == derivedTypes[i])
                {
                    selectedIndex = i + 1;
                }
            }

            Rect dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            int newIndex = EditorGUI.Popup(dropdownRect, label.text, selectedIndex, typeNames);

            if (newIndex != selectedIndex)
            {
                property.managedReferenceValue = newIndex == 0
                    ? null
                    : Activator.CreateInstance(derivedTypes[newIndex - 1]);
            }

            if (property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                float yOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                foreach (SerializedProperty child in GetVisibleChildren(property))
                {
                    float childHeight = EditorGUI.GetPropertyHeight(child, true);
                    Rect childRect = new Rect(position.x, position.y + yOffset, position.width, childHeight);
                    EditorGUI.PropertyField(childRect, child, true);
                    yOffset += childHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static IEnumerable<SerializedProperty> GetVisibleChildren(SerializedProperty property)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            if (!iterator.NextVisible(true))
            {
                yield break;
            }

            do
            {
                if (SerializedProperty.EqualContents(iterator, end))
                {
                    break;
                }

                yield return iterator.Copy();
            }
            while (iterator.NextVisible(false));
        }

        private static List<Type> GetDerivedTypes(Type baseType)
        {
            if (s_typeCache.TryGetValue(baseType, out List<Type> cached))
            {
                return cached;
            }

            List<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                .OrderBy(type => type.Name)
                .ToList();

            s_typeCache[baseType] = types;
            return types;
        }
    }
}
