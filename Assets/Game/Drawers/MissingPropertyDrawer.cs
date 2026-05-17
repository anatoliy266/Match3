#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class Req : PropertyAttribute { }

[CustomPropertyDrawer(typeof(Req))]
public class ReqDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Проверяем, задано ли значение (не равно default)
        bool isMissing = IsValueMissing(property);

        // Если значение не задано — красим фон красным
        Color defaultBg = GUI.backgroundColor;
        if (isMissing)
            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f); // мягкий красный

        // Отрисовываем стандартное поле
        EditorGUI.PropertyField(position, property, label, true);

        // Возвращаем исходный цвет фона
        GUI.backgroundColor = defaultBg;
    }

    private bool IsValueMissing(SerializedProperty property)
    {
        // Массив считается незаполненным, если пуст
        if (property.isArray && property.arraySize == 0)
            return true;

        switch (property.propertyType)
        {
            case SerializedPropertyType.String:
                return string.IsNullOrEmpty(property.stringValue);

            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue == null;

            case SerializedPropertyType.Integer:
                return property.intValue == 0;

            case SerializedPropertyType.Float:
                return Mathf.Approximately(property.floatValue, 0f);

            case SerializedPropertyType.Boolean:
                return property.boolValue == false;

            // Для структур (Vector3, Color и т.п.) — сравниваем с default
            default:
                // boxedValue работает для любых типов, кроме массивов и объектов
                if (property.propertyType == SerializedPropertyType.Generic)
                {
                    // Для вложенных объектов не проверяем через boxedValue,
                    // но можно было бы пройти по всем дочерним полям — здесь пропускаем
                    return false;
                }
                var value = property.boxedValue;
                if (value == null)
                    return false; // не должно случиться для value-типов
                var type = value.GetType();
                if (type.IsValueType)
                {
                    var defaultValue = System.Activator.CreateInstance(type);
                    return value.Equals(defaultValue);
                }
                return false;
        }
    }
}
#endif