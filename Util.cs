
using System;
using System.Collections.Generic;
using System.Reflection;

public class Util {

    public static Dictionary<string, MemberInfo> dict = new Dictionary<string, MemberInfo>();

    public static object GetPrivateProperty(Type t, object obj, string propertyName) {
        string key = t.ToString() + ":" + propertyName;
        if (!dict.ContainsKey(key)) {
            dict[key] = t.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(nonPublic: true);
        }
        return ((MethodInfo)dict[key]).Invoke(obj, null);
    }

    public static object GetPrivateField(Type t, object obj, string propertyName) {
        string key = t.ToString() + ":" + propertyName;
        if (!dict.ContainsKey(key)) {
            dict[key] = t.GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
        }
        return ((FieldInfo)dict[key]).GetValue(obj);
    }

    public static void SetPrivateField(Type t, object obj, string propertyName, object val) {
        string key = t.ToString() + ":" + propertyName;
        if (!dict.ContainsKey(key)) {
            dict[key] = t.GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
        }
        ((FieldInfo)dict[key]).SetValue(obj, val);
    }

}
