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
        private static readonly GUIContent k_Low = new GUIContent("Low");
        private static readonly GUIContent k_Med = new GUIContent("Medium");
        private static readonly GUIContent k_High = new GUIContent("High");
        private static readonly GUIContent k_Ultra = new GUIContent("Ultra");

        private static readonly GUIContent k_ShortLow = new GUIContent("L", "Low");
        private static readonly GUIContent k_ShortMed = new GUIContent("M", "Medium");
        private static readonly GUIContent k_ShortHigh = new GUIContent("H", "High");
        private static readonly GUIContent k_ShortUltra = new GUIContent("U", "Ultra");

        public static void IntGUI(this SerializedScalableSetting self, GUIContent label)
        {
            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, 0, EditorGUIUtility.singleLineHeight);
            // Magic Number !!
            rect.x += 3;
            rect.width -= 6;
            // Magic Number !!

            var contentRect = EditorGUI.PrefixLabel(rect, label);

            int[] values = { self.low.intValue, self.med.intValue, self.high.intValue, self.ultra.intValue };
            GUIContent[] labels = {k_ShortLow, k_ShortMed, k_ShortHigh, k_ShortUltra};

            EditorGUI.showMixedValue = self.low.hasMultipleDifferentValues
                || self.med.hasMultipleDifferentValues
                || self.high.hasMultipleDifferentValues
                || self.ultra.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiIntField(contentRect, labels, values);
            if(EditorGUI.EndChangeCheck())
            {
                self.low.intValue = values[0];
                self.med.intValue = values[1];
                self.high.intValue = values[2];
                self.ultra.intValue = values[3];
            }
            EditorGUI.showMixedValue = false;
        }
    }
}
