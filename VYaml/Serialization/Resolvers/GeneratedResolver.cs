#nullable enable
using System;
using System.Reflection;
using VYaml.Annotations;

namespace VYaml.Serialization
{
    public class GeneratedResolver : IYamlFormatterResolver
    {
        static class Check<T>
        {
            internal static bool Registered;
        }

        static class Cache<T>
        {
            internal static IYamlFormatter<T>? Formatter;

            static Cache()
            {
                if (Check<T>.Registered) return;

                TryInvokeRegisterYamlFormatter<T>();
            }
        }

        static bool TryInvokeRegisterYamlFormatter<T>()
        {
            var type = typeof(T);
            if (type.GetCustomAttribute<YamlObjectAttribute>() == null) return false;

            if (type.IsInterface)
            {
                var generatedFormatterType = type.GetNestedType($"{type.Name}GeneratedFormatter");
                if (generatedFormatterType == null) return false;

                var formatterInstance = (IYamlFormatter<T>)Activator.CreateInstance(generatedFormatterType)!;
                Register(formatterInstance);

                return true;
            }

            var m = type.GetMethod("__RegisterVYamlFormatter",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);

            if (m == null) return false;

            m.Invoke(null, null); // Cache<T>.formatter will set from method
            return true;
        }

        [Preserve]
        public static void Register<T>(IYamlFormatter<T> formatter)
        {
            Check<T>.Registered = true; // avoid to call Cache() constructor called.
            Cache<T>.Formatter = formatter;
        }

        public static readonly GeneratedResolver Instance = new();

        public IYamlFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;
    }
}
