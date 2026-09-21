using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WhoBuysThisResearch
{
    internal static class ReflectionUtil
    {
        internal const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    Type type = assemblies[i].GetType(fullName, false);
                    if (type != null) return type;
                }
                catch { }
            }
            return null;
        }

        internal static bool TryRead(object owner, string name, out object value)
        {
            value = null;
            if (owner == null) return false;
            for (Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                try
                {
                    FieldInfo field = type.GetField(name, AnyInstance | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        value = field.GetValue(owner);
                        return true;
                    }
                    PropertyInfo prop = type.GetProperty(name, AnyInstance | BindingFlags.DeclaredOnly);
                    if (prop != null && prop.GetIndexParameters().Length == 0)
                    {
                        value = prop.GetValue(owner, null);
                        return true;
                    }
                }
                catch { return false; }
            }
            return false;
        }

        internal static bool TryReadStatic(Type type, string name, out object value)
        {
            value = null;
            if (type == null) return false;
            try
            {
                FieldInfo field = type.GetField(name, AnyStatic);
                if (field != null)
                {
                    value = field.GetValue(null);
                    return true;
                }
                PropertyInfo prop = type.GetProperty(name, AnyStatic);
                if (prop != null && prop.GetIndexParameters().Length == 0)
                {
                    value = prop.GetValue(null, null);
                    return true;
                }
            }
            catch { }
            return false;
        }

        internal static object FindActiveUnityInstance(Type type)
        {
            if (type == null || !typeof(UnityEngine.Object).IsAssignableFrom(type)) return null;
            UnityEngine.Object[] objects;
            try { objects = Resources.FindObjectsOfTypeAll(type); }
            catch { return null; }
            for (int i = 0; i < objects.Length; i++)
            {
                Component component = objects[i] as Component;
                if (component != null && component.gameObject.activeInHierarchy) return component;
                GameObject gameObject = objects[i] as GameObject;
                if (gameObject != null && gameObject.activeInHierarchy) return gameObject;
            }
            return objects.Length > 0 ? objects[0] : null;
        }

        internal static IEnumerable EnumerateMember(object owner, string name)
        {
            object value;
            if (!TryRead(owner, name, out value)) return null;
            return value as IEnumerable;
        }

        internal static IEnumerable AsEnumerable(object value)
        {
            return value as IEnumerable;
        }

        internal static string ReadString(object owner, string name)
        {
            object value;
            if (!TryRead(owner, name, out value) || value == null) return null;
            return value as string ?? Convert.ToString(value);
        }

        internal static int ReadInt(object owner, string name, int fallback)
        {
            object value;
            if (!TryRead(owner, name, out value) || value == null) return fallback;
            try { return Convert.ToInt32(value); }
            catch { return fallback; }
        }

        internal static bool ReadBool(object owner, string name, bool fallback)
        {
            object value;
            if (!TryRead(owner, name, out value) || value == null) return fallback;
            try { return Convert.ToBoolean(value); }
            catch { return fallback; }
        }

        internal static object Invoke(object owner, string name, object[] args)
        {
            if (owner == null) return null;
            MethodInfo method = FindMethod(owner.GetType(), name, args == null ? 0 : args.Length, false);
            if (method == null) return null;
            try { return method.Invoke(owner, args); }
            catch { return null; }
        }

        internal static MethodInfo FindMethod(Type type, string name, int parameterCount, bool isStatic)
        {
            if (type == null) return null;
            BindingFlags flags = isStatic ? AnyStatic : AnyInstance;
            MethodInfo[] methods = type.GetMethods(flags);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == name && methods[i].GetParameters().Length == parameterCount) return methods[i];
            }
            return null;
        }

        internal static MethodInfo FindMethod(Type type, string name, Type[] parameterTypes, bool isStatic)
        {
            if (type == null) return null;
            BindingFlags flags = isStatic ? AnyStatic : AnyInstance;
            MethodInfo[] methods = type.GetMethods(flags);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != name) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != parameterTypes.Length) continue;
                bool matches = true;
                for (int p = 0; p < parameters.Length; p++)
                {
                    if (parameterTypes[p] != null && parameters[p].ParameterType != parameterTypes[p])
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches) return method;
            }
            return null;
        }

        internal static string SafeToString(object value)
        {
            if (value == null) return "<null>";
            try { return value.ToString(); }
            catch { return "<to-string-failed>"; }
        }

        internal static List<string> ReadStringList(object owner, string name)
        {
            List<string> result = new List<string>();
            IEnumerable values = EnumerateMember(owner, name);
            if (values == null) return result;
            foreach (object value in values)
            {
                if (value != null) result.Add(Convert.ToString(value));
            }
            return result;
        }
    }
}
