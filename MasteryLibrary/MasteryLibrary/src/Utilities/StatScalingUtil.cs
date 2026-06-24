using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MasteryLibrary.src.Utilities
{
    public static class StatScalingUtil
    {
        private static readonly ConcurrentDictionary<string, Func<StatConfiguration, int, float>> ScalingMethods =
            new ConcurrentDictionary<string, Func<StatConfiguration, int, float>>();

        static StatScalingUtil()
        {
            ScalingMethods["Linear"] = (config, level) =>
                GetSafeParam<float>(config, "BaseValue") * level;

            ScalingMethods["Exponential"] = (config, level) =>
            {
                float baseVal = GetSafeParam<float>(config, "BaseValue");
                float growth = GetSafeParam<float>(config, "GrowthFactor", 1.0f);
                return baseVal * (float)Math.Pow(growth, level);
            };

            ScalingMethods["Array"] = (config, level) =>
            {
                if (level <= 0) return 0;

                var values = GetSafeParam<float[]>(config, "Values");
                if (values == null || values.Length == 0) return 0;

                int index = Math.Clamp(level - 1, 0, values.Length - 1);
                return values[index];
            };
        }

        public static float GetScaledValue(StatConfiguration statConfig, int level)
        {
            if (statConfig == null) return 0;

            if (ScalingMethods.TryGetValue(statConfig.ScalingMethod, out var scalingFunc))
            {
                try { return scalingFunc(statConfig, level); }
                catch { return 0; }
            }

            return ScalingMethods["Linear"](statConfig, level);
        }

        public static void RegisterScalingMethod(string methodName, Func<StatConfiguration, int, float> scalingFunction)
        {
            if (!string.IsNullOrWhiteSpace(methodName) && scalingFunction != null)
            {
                ScalingMethods[methodName] = scalingFunction;
            }
        }

        private static T GetSafeParam<T>(StatConfiguration config, string key, T defaultValue = default)
        {
            if (config?.ScalingParams == null || !config.ScalingParams.TryGetValue(key, out object value))
                return defaultValue;

            try
            {
                if (typeof(T) == typeof(float))
                    return (T)(object)Convert.ToSingle(value);

                if (typeof(T) == typeof(float[]) && value is Newtonsoft.Json.Linq.JArray jArray)
                    return (T)(object)jArray.ToObject<float[]>();

                return (T)value;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}