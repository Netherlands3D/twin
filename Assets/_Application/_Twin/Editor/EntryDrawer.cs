using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D._Application._Twin.Editor
{
    [CustomPropertyDrawer(typeof(AssetLibrary.Entry))]
    public class EntryDrawer : PropertyDrawer
    {
        private class EntryProps
        {
            private readonly SerializedProperty prop;

            public SerializedProperty Id => prop.FindPropertyRelative("id");
            public SerializedProperty Type => prop.FindPropertyRelative("type");
            public AssetLibrary.EntryType TypeValue => (AssetLibrary.EntryType)Type.enumValueIndex;
            public SerializedProperty Title => prop.FindPropertyRelative("title");
            public SerializedProperty Description => prop.FindPropertyRelative("description");
            public SerializedProperty Prefab => prop.FindPropertyRelative("prefab");
            public SerializedProperty Url => prop.FindPropertyRelative("url");
            public SerializedProperty Children => prop.FindPropertyRelative("children");

            public bool IsImportable => TypeValue is AssetLibrary.EntryType.Prefab or AssetLibrary.EntryType.Url;

            public BaseField<string> CreateTitleField(Foldout root)
            {
                var titleField = new TextField("Title")
                {
                    bindingPath = Title.propertyPath
                };
                titleField.RegisterValueChangedCallback(
                    evt =>
                    {
                        if (string.IsNullOrEmpty(evt.newValue))
                        {
                            root.text = "Entry";
                            return;
                        }
                    
                        root.text = evt.newValue;
                    }
                );

                return titleField;
            }

            public BaseField<Enum> CreateTypeField(EventCallback<ChangeEvent<Enum>> onChanged)
            {
                var typeField = new EnumField("Type", (AssetLibrary.EntryType)Type.enumValueIndex)
                {
                    bindingPath = Type.propertyPath
                };
                
                typeField.RegisterValueChangedCallback(onChanged);
                
                return typeField;
            }

            public VisualElement CreateIdFieldWithButton()
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;

                var idText = new TextField("Id")
                {
                    bindingPath = Id.propertyPath,
                    style = { flexGrow = 1 }
                };

                var genBtn = new Button(() =>
                {
                    Id.stringValue = Guid.NewGuid().ToString();
                    Id.serializedObject.ApplyModifiedProperties();
                    idText.SetValueWithoutNotify(Id.stringValue);
                })
                { text = "Generate" };

                row.Add(idText);
                row.Add(genBtn);

                return row;
            }

            public VisualElement CreateDescriptionField() => new PropertyField(Description, "Description");
            public VisualElement CreatePrefabField() => new PropertyField(Prefab, "Prefab");
            public VisualElement CreateUrlField() => new PropertyField(Url, "Url");
            public VisualElement CreateChildrenField() => new PropertyField(Children, "Children");
            
            public EntryProps(SerializedProperty prop)
            {
                this.prop = prop;
            }

            public void Load()
            {
                var assetLibrary = prop.serializedObject.targetObject as AssetLibrary;
                if (!assetLibrary)
                {
                    Debug.LogError(prop.type + " is not an AssetLibrary");
                    return;
                }

                assetLibrary.Load(Id.stringValue);
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty prop)
        {
            var root = new Foldout();
            var entry = new EntryProps(prop);

            var prefabField = entry.CreatePrefabField();
            var urlField = entry.CreateUrlField();
            var childrenField = entry.CreateChildrenField();

            root.text = string.IsNullOrEmpty(entry.Title.stringValue) ? "Entry" : entry.Title.stringValue;
            root.Add(entry.CreateTypeField(_ => RefreshVisibility(urlField, prefabField, childrenField, entry)));
            root.Add(entry.CreateIdFieldWithButton());
            root.Add(entry.CreateTitleField(root));
            root.Add(entry.CreateDescriptionField());
            root.Add(CreateSeparator());
            root.Add(urlField);
            root.Add(prefabField);
            root.Add(childrenField);
            root.Bind(prop.serializedObject);

            if (entry.IsImportable && Application.isPlaying)
            {
                var actionBtn = new Button(entry.Load) { text = "Import" };
                root.Add(actionBtn);
            }

            // Initial refresh of the UI state
            RefreshVisibility(urlField, prefabField, childrenField, entry);

            return root;
        }

        private void RefreshVisibility(VisualElement url, VisualElement prefab, VisualElement children, EntryProps entry)
        {
            url.style.display = entry.TypeValue == AssetLibrary.EntryType.Url ? DisplayStyle.Flex : DisplayStyle.None;
            prefab.style.display = entry.TypeValue == AssetLibrary.EntryType.Prefab ? DisplayStyle.Flex : DisplayStyle.None;
            children.style.display = entry.TypeValue is AssetLibrary.EntryType.Folder or AssetLibrary.EntryType.DataSet 
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement CreateSeparator()
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.marginTop = 4;
            separator.style.marginBottom = 4;
            separator.style.backgroundColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
            
            return separator;
        }
    }
}