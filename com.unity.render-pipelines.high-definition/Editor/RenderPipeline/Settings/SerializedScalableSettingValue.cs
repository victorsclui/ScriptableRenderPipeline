using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    public class SerializedScalableSettingValue
    {
        public SerializedProperty level;
        public SerializedProperty useOverride;
        public SerializedProperty @override;

        public SerializedScalableSettingValue(SerializedProperty property)
        {
            level = property.FindPropertyRelative("m_Level");
            useOverride = property.FindPropertyRelative("m_UseOverride");
            @override = property.FindPropertyRelative("m_Override");
        }
    }

    public static class SerializedScalableSettingValueUI
    {
        public interface IValueFormatter
        {
            string GetValue(ScalableSetting.Level level);
        }

        public struct NoopFormatter : IValueFormatter
        {
            public string GetValue(ScalableSetting.Level level) => string.Empty;
        }

        public struct FromScalableSetting<T>: IValueFormatter
        {
            private ScalableSetting<T> m_Value;
            private HDRenderPipelineAsset m_Source;

            public FromScalableSetting(
                ScalableSetting<T> value,
                HDRenderPipelineAsset source)
            {
                m_Value = value;
                m_Source = source;
            }

            public string GetValue(ScalableSetting.Level level) => m_Value != null && m_Source != null ? $"({m_Value[level]} from {m_Source.name})" : string.Empty;
        }

        private static readonly GUIContent k_Level = new GUIContent("Level");
        private static readonly GUIContent k_UseOverride = new GUIContent("Use Override");
        private static readonly GUIContent k_Override = new GUIContent("Override");

        private static readonly GUIContent[] k_LevelOptions =
        {
            new GUIContent("Low"),
            new GUIContent("Medium"),
            new GUIContent("High"),
            new GUIContent("Ultra"),
            new GUIContent("Override"),
        };

        static Rect DoGUI(SerializedScalableSettingValue self, GUIContent label)
        {
            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, 0, EditorGUIUtility.singleLineHeight);

            var contentRect = EditorGUI.PrefixLabel(rect, label);

            // Render the enum popup
            const int k_EnumWidth = 70;
            // Magic number??
            const int k_EnumOffset = 30;
            var enumRect = new Rect(contentRect);
            enumRect.x -= k_EnumOffset;
            enumRect.width = k_EnumWidth + k_EnumOffset;
            var enumValue = self.useOverride.boolValue ? k_LevelOptions.Length - 1 : self.level.intValue;

            var newEnumValues = EditorGUI.Popup(enumRect, GUIContent.none, enumValue, k_LevelOptions);
            if (newEnumValues != enumValue)
            {
                self.useOverride.boolValue = newEnumValues == k_LevelOptions.Length - 1;
                if (!self.useOverride.boolValue)
                    self.level.intValue = newEnumValues;
            }

            // Return the rect fo user can render the field there
            var fieldRect = new Rect(contentRect);
            fieldRect.x = enumRect.x + enumRect.width + 2 - k_EnumOffset;
            fieldRect.width = contentRect.width - (fieldRect.x - enumRect.x) + k_EnumOffset;

            return fieldRect;
        }

        public static void IntGUI<T>(this SerializedScalableSettingValue self, GUIContent label, T @default)
            where T: struct, IValueFormatter
        {
            var fieldRect = DoGUI(self, label);
            if (self.useOverride.boolValue)
                self.@override.intValue = EditorGUI.IntField(fieldRect, self.@override.intValue);
            else
                EditorGUI.LabelField(fieldRect, @default.GetValue((ScalableSetting.Level)self.level.intValue));
        }

        public static void IntGUI(this SerializedScalableSettingValue self, GUIContent label)
        {
            IntGUI(self, label, new NoopFormatter());
        }
    }
}
