#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

namespace TappyTale.WaveDirector.Editor
{
    internal static class WaveDirectorTypeUtil
    {
        internal static Type FindTypeByName(string typeName)
        {
            return FindType(t => t != null && string.Equals(t.Name, typeName, StringComparison.Ordinal));
        }

        internal static Type FindTypeByFullName(string fullName)
        {
            return FindType(t => t != null && string.Equals(t.FullName, fullName, StringComparison.Ordinal));
        }

        private static Type FindType(Func<Type, bool> predicate)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm == null || asm.IsDynamic) continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }
                catch { continue; }

                if (types == null) continue;

                foreach (var t in types)
                {
                    if (predicate(t)) return t;
                }
            }

            return null;
        }
    }
}
#endif
