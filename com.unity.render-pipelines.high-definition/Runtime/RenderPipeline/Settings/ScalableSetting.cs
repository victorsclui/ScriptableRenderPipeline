using System;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.HighDefinition
{
    public static class ScalableSetting
    {
        public enum Level
        {
            Low,
            Medium,
            High,
            Ultra
        }
    }

    [Serializable]
    public class ScalableSetting<T>
    {
        [SerializeField]
        private T m_Low;
        [SerializeField]
        private T m_Med;
        [SerializeField]
        private T m_High;
        [SerializeField]
        private T m_Ultra;

        public T this[ScalableSetting.Level index]
        {
            get
            {
                switch (index)
                {
                    case ScalableSetting.Level.Low: return m_Low;
                    case ScalableSetting.Level.Medium: return m_Med;
                    case ScalableSetting.Level.High: return m_High;
                    case ScalableSetting.Level.Ultra: return m_Ultra;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
            set
            {
                switch (index)
                {
                    case ScalableSetting.Level.Low: m_Low = value; break;
                    case ScalableSetting.Level.Medium: m_Med = value; break;
                    case ScalableSetting.Level.High: m_High = value; break;
                    case ScalableSetting.Level.Ultra: m_Ultra = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        public T low
        {
            get => m_Low;
            set => m_Low = value;
        }

        public T med
        {
            get => m_Med;
            set => m_Med = value;
        }

        public T high
        {
            get => m_High;
            set => m_High = value;
        }

        public T ultra
        {
            get => m_Ultra;
            set => m_Ultra = value;
        }
    }

    [Serializable] public class IntScalableSetting: ScalableSetting<int> {}
    [Serializable] public class UintScalableSetting: ScalableSetting<uint> {}
    [Serializable] public class FloatScalableSetting: ScalableSetting<float> {}
    [Serializable] public class BoolScalableSetting: ScalableSetting<bool> {}

    [Serializable]
    public class ScalableSettingValue<T>
    {
        [SerializeField] private ScalableSetting.Level m_Level;
        [SerializeField] private bool m_UseOverride;
        [SerializeField] private T m_Override;

        public T @override
        {
            get => m_Override;
            set => m_Override = value;
        }

        public bool useOverride
        {
            get => m_UseOverride;
            set => m_UseOverride = value;
        }

        public ScalableSetting.Level level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public T Value(ScalableSetting<T> source) => m_UseOverride ? m_Override : source[m_Level];

        public void CopyTo(ScalableSettingValue<T> dst)
        {
            Assert.IsNotNull(dst);

            dst.m_Level = m_Level;
            dst.m_UseOverride = m_UseOverride;
            dst.m_Override = m_Override;
        }
    }

    [Serializable] public class IntScalableSettingValue: ScalableSettingValue<int> {}
    [Serializable] public class UintScalableSettingValue: ScalableSettingValue<uint> {}
    [Serializable] public class FloatScalableSettingValue: ScalableSettingValue<float> {}
    [Serializable] public class BoolScalableSettingValue: ScalableSettingValue<bool> {}
}
