using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TinyPass
{
    static class ReflectionHelper
    {

        public static readonly Type ObjectType = typeof(object);
        public const BindingFlags PropertyBindingFlags = (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        public const BindingFlags FieldBindingFlags = (BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        public const string __PREFIX = "@";

        public static Dictionary<string, Tuple<object, Type>> ObjectToDictionary<T>(T obj) where T : class
        {
            var getters = ReflectionHelper.GetPropertyGetters<T>();
            var dictionary = new Dictionary<string, Tuple<object, Type>>(getters.Count, FastStringComparer.Instance);
            var gettersEnumerator = getters.GetEnumerator();
            while (gettersEnumerator.MoveNext())
            {
                var kv = gettersEnumerator.Current;
                dictionary.Add(kv.Key, kv.Value(obj));
            }
            return dictionary;
        }

        public static Dictionary<string, Tuple<object, Type>> ObjectToDictionary_Parameterized<T>(T obj) where T : class
        {
            var getters = ReflectionHelper.GetPropertyGetters<T>(ReflectionHelper.__PREFIX);

            var dictionary = new Dictionary<string, Tuple<object, Type>>(getters.Count, FastStringComparer.Instance);
            var gettersEnumerator = getters.GetEnumerator();
            while (gettersEnumerator.MoveNext())
            {
                var kv = gettersEnumerator.Current;
                dictionary.Add(kv.Key, kv.Value(obj));
            }
            return dictionary;
        }

        public static Dictionary<string, Func<T, Tuple<object, Type>>> GetPropertyGetters<T>(string prefix = "") where T : class
        {
            Type type = typeof(T);
            var source = type.ContainsGenericParameters ? ZeroLengthArray<PropertyInfo>.Value : type.GetProperties(PropertyBindingFlags);
            var dictionary = new Dictionary<string, Func<T, Tuple<object, Type>>>(source.Length, FastStringComparer.Instance);

            var pInstance = Expression.Parameter(type);
            var parameterExpressionArray = new[] { pInstance };
            foreach (PropertyInfo p in source)
            {
                if (p.GetIndexParameters().Length != 0) continue;
                MethodInfo getMethod = p.GetGetMethod(nonPublic: true);
                if (getMethod != null)
                {
                    UnaryExpression body = Expression.Convert(Expression.Call(pInstance, getMethod), ObjectType);
                    var valueTupleBody = Expression.New(typeof(Tuple<object, Type>).GetConstructor(new Type[] { }), new Expression[] { body, Expression.Constant(p.PropertyType) });
                    dictionary[prefix + p.Name] = Expression.Lambda<Func<T, Tuple<object, Type>>>(valueTupleBody, parameterExpressionArray).Compile();
                }
            }

            return dictionary;
        }

        public static Dictionary<string, Action<object, object>> GetPropertySetters(object ClassObject)
        {
            var dictionary = new Dictionary<string, Action<object, object>>(FastStringComparer.Instance);
            if (ClassObject != null)
            {
                Type ClassType = ClassObject.GetType();
                var source_properites = ClassType.ContainsGenericParameters ? ZeroLengthArray<PropertyInfo>.Value : ClassType.GetProperties(PropertyBindingFlags);
                var source_fields = ClassType.ContainsGenericParameters ? ZeroLengthArray<FieldInfo>.Value : ClassType.GetFields(FieldBindingFlags);

                var pInstance = Expression.Parameter(ObjectType, "instance");
                var pValue = Expression.Parameter(ObjectType, "value");

                foreach (PropertyInfo p in source_properites)
                {
                    if (p.GetIndexParameters().Length != 0) continue;
                    if (p.CanWrite)
                    {
                        dictionary[p.Name] = Expression.Lambda<Action<object, object>>(
                            Expression.Assign(Expression.Property(Expression.Convert(pInstance, ClassType), p.Name), Expression.Convert(pValue, p.PropertyType)),
                            new[] { pInstance, pValue }
                         ).Compile();
                    }
                    else
                    {
                        // 因為 Anonymous Type PropertyInfo 是無法寫入變數內容.要改用 FieldInfo (<ID>i__Field)
                        MethodInfo SetValueMethodInfo = null;
                        var FieldInfo = ClassType.GetField($"<{p.Name}>i__Field", FieldBindingFlags);
                        if (FieldInfo != null)
                            SetValueMethodInfo = FieldInfo.GetType().GetMethod("SetValue", new Type[] { typeof(object), typeof(object) });
                        else if ((FieldInfo = ClassType.GetField($"<{p.Name}>i__Field", FieldBindingFlags)) != null)
                            SetValueMethodInfo = FieldInfo.GetType().GetMethod("SetValue", new Type[] { typeof(object), typeof(object) });
                        if (SetValueMethodInfo != null)
                        {
                            var valueCast = Expression.Convert(pValue, p.PropertyType);
                            dictionary[p.Name] = Expression.Lambda<Action<object, object>>(
                                // Like : FieldInfo.SetValue(pInstance, CAST(pValue, PropertyType));
                                Expression.Call(Expression.Constant(FieldInfo), SetValueMethodInfo, new Expression[] { pInstance, valueCast }),
                                new[] { pInstance, pValue }
                             ).Compile();
                        }
                    }
                }
                foreach (FieldInfo f in source_fields)
                {
                    string Name = f.Name;
                    if (!Name.StartsWith("<"))  //<ID>k__BackingField   <ID>i__Field
                    {
                        dictionary[f.Name] = Expression.Lambda<Action<object, object>>(
                            Expression.Assign(Expression.Field(Expression.Convert(pInstance, ClassType), f.Name), Expression.Convert(pValue, f.FieldType)),
                             new[] { pInstance, pValue }
                            ).Compile();
                    }
                }
            }
            return dictionary;
        }

        public static Dictionary<string, Action<T, object>> GetPropertySetters<T>() where T : class
        {
            Type ClassType = typeof(T);
            var source_properites = ClassType.ContainsGenericParameters ? ZeroLengthArray<PropertyInfo>.Value : ClassType.GetProperties(PropertyBindingFlags);
            var source_fields = ClassType.ContainsGenericParameters ? ZeroLengthArray<FieldInfo>.Value : ClassType.GetFields(FieldBindingFlags);
            var dictionary = new Dictionary<string, Action<T, object>>(source_properites.Length, FastStringComparer.Instance);
            var valueCast_container = new UnaryExpression[1];

            var pInstance = Expression.Parameter(ClassType, "instance");
            var pValue = Expression.Parameter(ObjectType, "value");

            foreach (PropertyInfo p in source_properites)
            {
                if (p.GetIndexParameters().Length != 0) continue;
                if (p.CanWrite)
                {
                    dictionary[p.Name] = Expression.Lambda<Action<T, object>>(
                        Expression.Assign(Expression.Property(Expression.Convert(pInstance, ClassType), p.Name), Expression.Convert(pValue, p.PropertyType)),
                        new[] { pInstance, pValue }
                     ).Compile();
                }
                else
                {
                    // 因為 Anonymous Type PropertyInfo 是無法寫入變數內容.要改用 FieldInfo (<ID>i__Field)
                    MethodInfo SetValueMethodInfo = null;
                    var FieldInfo = ClassType.GetField($"<{p.Name}>i__Field", FieldBindingFlags);
                    if (FieldInfo != null)
                        SetValueMethodInfo = FieldInfo.GetType().GetMethod("SetValue", new Type[] { typeof(object), typeof(object) });
                    else if ((FieldInfo = ClassType.GetField($"<{p.Name}>k__BackingField", FieldBindingFlags)) != null)
                        SetValueMethodInfo = FieldInfo.GetType().GetMethod("SetValue", new Type[] { typeof(object), typeof(object) });
                    if (SetValueMethodInfo != null)
                    {
                        var valueCast = Expression.Convert(pValue, p.PropertyType);
                        dictionary[p.Name] = Expression.Lambda<Action<T, object>>(
                            // Like : FieldInfo.SetValue(pInstance, CAST(pValue, PropertyType));
                            Expression.Call(Expression.Constant(FieldInfo), SetValueMethodInfo, new Expression[] { pInstance, pValue }),
                            new[] { pInstance, pValue }
                         ).Compile();
                    }
                }
            }
            foreach (FieldInfo f in source_fields)
            {
                string Name = f.Name;
                if (!Name.StartsWith("<"))  //<ID>k__BackingField   <ID>i__Field
                {
                    dictionary[f.Name] = Expression.Lambda<Action<T, object>>(
                        Expression.Assign(Expression.Field(pInstance, f.Name), Expression.Convert(pValue, f.FieldType)),
                        new[] { pInstance, pValue }
                   ).Compile();
                }
            }
            return dictionary;
        }

        private sealed class FastStringComparer : System.Collections.Generic.IEqualityComparer<string>
        {
            FastStringComparer() { }
            public bool Equals(string x, string y) => string.Equals(x, y);
            public int GetHashCode(string obj) => obj.GetHashCode();
            public static readonly FastStringComparer Instance = new FastStringComparer();
        }
        private static class ZeroLengthArray<T> { public static readonly T[] Value = new T[0]; }

    }
}
