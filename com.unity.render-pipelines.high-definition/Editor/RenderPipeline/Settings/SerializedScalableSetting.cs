using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    public class SerializedScalableSetting
    {
        public SerializedProperty low;
        public SerializedProperty med;
        public SerializedProperty high;
        public SerializedProperty ultra;

        public SerializedScalableSetting(SerializedProperty property)
        {
            low = property.FindPropertyRelative("m_Low");
            med = property.FindPropertyRelative("m_Med");
            high = property.FindPropertyRelative("m_High");
            ultra = property.FindPropertyRelative("m_Ultra");
        }
    }

    public static class SerializedScalableSettingUI
    {
        private static readonly GUIContent k_ShortLow = new GUIContent("L", "Low");
        private static readonly GUIContent k_ShortMed = new GUIContent("M", "Medium");
        private static readonly GUIContent k_ShortHigh = new GUIContent("H", "High");
        private static readonly GUIContent k_ShortUltra = new GUIContent("U", "Ultra");

        private static readonly GUIContent k_Low = new GUIContent("Low", "Low");
        private static readonly GUIContent k_Med = new GUIContent("Medium", "Medium");
        private static readonly GUIContent k_High = new GUIContent("High", "High");
        private static readonly GUIContent k_Ultra = new GUIContent("Ultra", "Ultra");

        public static void ValueGUI<T>(this SerializedScalableSetting self, GUIContent label)
        {
            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, 0, EditorGUIUtility.singleLineHeight);
            // Magic Number !!
            rect.x += 3;
            rect.width -= 6;
            // Magic Number !!

            var contentRect = EditorGUI.PrefixLabel(rect, label);
            EditorGUI.showMixedValue = self.low.hasMultipleDifferentValues
                                       || self.med.hasMultipleDifferentValues
                                       || self.high.hasMultipleDifferentValues
                                       || self.ultra.hasMultipleDifferentValues;

            if (typeof(T) == typeof(bool))
            {
                GUIContent[] labels = {k_Low, k_Med, k_High, k_Ultra};
                bool[] values =
                {
                    self.low.boolValue,
                    self.med.boolValue,
                    self.high.boolValue,
                    self.ultra.boolValue
                };
                EditorGUI.BeginChangeCheck();
                MultiField(contentRect, labels, values);
                if(EditorGUI.EndChangeCheck())
                {
                    self.low.boolValue = values[0];
                    self.med.boolValue = values[1];
                    self.high.boolValue = values[2];
                    self.ultra.boolValue = values[3];
                }
            }
            else if (typeof(T) == typeof(int))
            {
                GUIContent[] labels = {k_ShortLow, k_ShortMed, k_ShortHigh, k_ShortUltra};
                int[] values =
                {
                    self.low.intValue,
                    self.med.intValue,
                    self.high.intValue,
                    self.ultra.intValue
                };
                EditorGUI.BeginChangeCheck();
                MultiField(contentRect, labels, values);
                if(EditorGUI.EndChangeCheck())
                {
                    self.low.intValue = values[0];
                    self.med.intValue = values[1];
                    self.high.intValue = values[2];
                    self.ultra.intValue = values[3];
                }
            }

            EditorGUI.showMixedValue = false;
        }

        internal static void MultiField<T>(Rect position, GUIContent[] subLabels, T[] values)
        {
            var length = values.Length;
            var num = (position.width - (float) (length - 1) * 4f) / (float) length;
            var position1 = new Rect(position)
            {
                width = num
            };
            var labelWidth = EditorGUIUtility.labelWidth;
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            for (var index = 0; index < values.Length; ++index)
            {
                EditorGUIUtility.labelWidth = CalcPrefixLabelWidth(subLabels[index], (GUIStyle) null);
                if (typeof(T) == typeof(int))
                    values[index] = (T)(object)EditorGUI.IntField(position1, subLabels[index], (int)(object)values[index]);
                else if (typeof(T) == typeof(bool))
                    values[index] = (T)(object)EditorGUI.Toggle(position1, subLabels[index], (bool)(object)values[index]);
                position1.x += num + 4f;
            }
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;
        }

        internal static float CalcPrefixLabelWidth(GUIContent label, GUIStyle style = null)
        {
            if (style == null)
                style = EditorStyles.label;
            return style.CalcSize(label).x;
        }
    }
}
