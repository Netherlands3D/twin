#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;

namespace Netherlands3D.FirstPersonViewer.Temp
{

    // Custom property drawer for the dropdown
    [CustomPropertyDrawer(typeof(ReorderableList<>), useForChildren: true)]
    public class ReorderableListDrawer : PropertyDrawer
    {
        private const string fieldName = "list";
        private const float padding = 2f;
        private const int arraySizePadding = 50;
        private ReorderableList list;
        private SerializedProperty listProperty;
        private bool unfold = true;
        private System.Type genericType;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Find the SerializedProperty for your reorderable list
            listProperty = property.FindPropertyRelative(fieldName);

            // Initialize the ReorderableList
            if (list == null)
            {
                Initialize(property);
            }

            // Update the serialized object
            property.serializedObject.Update();
            // Draw the array size
            EditorGUI.PropertyField(new Rect(position.x + position.width - arraySizePadding, position.y, arraySizePadding, EditorGUIUtility.singleLineHeight),
                                    property.FindPropertyRelative(fieldName).FindPropertyRelative("Array.size"), GUIContent.none);

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            // Check if mouse is hovering over the foldout
            if (Event.current.type == EventType.Repaint && position.Contains(Event.current.mousePosition))
            {
                // Highlight foldout when hovered over
                EditorGUI.DrawRect(new Rect(position.x, position.y, position.width - arraySizePadding, EditorGUIUtility.singleLineHeight), Color.grey);
            }

            unfold = EditorGUI.Foldout(position, unfold, property.name, true);
            if (unfold)
            {
                // Draw the reorderable list
                list.DoLayoutList();
            }
            // Apply changes to the serialized object
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void Initialize(SerializedProperty property)
        {
            list = new ReorderableList(property.serializedObject, listProperty, true, true, true, true);

            // Add callback functions
            list.drawHeaderCallback += DrawHeader;
            list.drawElementCallback += DrawElement;
            list.onAddDropdownCallback += OnAddDropdown;
            list.elementHeightCallback += GetListElementHeight;

            genericType = GetListType(fieldInfo.FieldType, fieldName);
        }

        static Type GetListType(Type type, string fieldName)
        {
            //Type parentType = _property.serializedObject.targetObject.GetType();
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Type fieldType = fieldInfo.FieldType;

            // Check if the field type is a generic list
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // Return the generic argument type
                return fieldType.GetGenericArguments()[0];
            }

            return null;
        }

        private float GetListElementHeight(int index)
        {
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            // Here you can calculate the height dynamically based on the content of the element
            // For example, you could check the type of the element and return different heights for different types
            // For simplicity, let's return a fixed height
            return EditorGUI.GetPropertyHeight(element, GUIContent.none, true) + padding * 2;
        }

        // Callback to draw the header of the reorderable list
        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, listProperty.name);
        }

        // Callback to draw each element of the reorderable list
        private void DrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            SerializedProperty elementProperty = list.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, elementProperty, true);
        }

        // Callback when the "Add" button is clicked, providing a dropdown menu
        private void OnAddDropdown(Rect buttonRect, ReorderableList list)
        {
            // Create the dropdown menu
            GenericMenu menu = new GenericMenu();

            // Add options to the dropdown menu (example options)
            List<System.Type> derivedTypes = GetDerivedTypes(genericType);

            foreach(Type type in derivedTypes)
            {
                if (type == null) continue;

                object item = Activator.CreateInstance(type);
                menu.AddItem(new GUIContent(type.Name), false, OnAddButtonClick, item);
            }

            // Show the dropdown menu
            menu.DropDown(buttonRect);
        }

        // Callback when an option in the dropdown menu is clicked
        private void OnAddButtonClick(object target)
        {
            // Ensure that the listProperty is not null
            if (listProperty != null)
            {
                // Record the current array size
                int arraySize = listProperty.arraySize;

                // Insert a new element at the end of the array
                listProperty.InsertArrayElementAtIndex(arraySize);

                // Get the newly inserted element
                SerializedProperty newElement = listProperty.GetArrayElementAtIndex(arraySize);

                // Assign the value of the new element
                newElement.boxedValue = target;

                // Apply modifications to the serialized object
                listProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        // Get all the classes derived from the specified base type
        private List<Type> GetDerivedTypes(Type baseType)
        {
            List<Type> derivedTypes = new List<Type>();
            foreach (var type in Assembly.GetAssembly(baseType).GetTypes())
            {
                if (type.IsSubclassOf(baseType) && !type.IsAbstract)
                {
                    derivedTypes.Add(type);
                }
            }
            return derivedTypes;
        }

    }
}
#endif