using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace TagRides.Shared.Utilities
{
    public static class ReflectionUtilities
    {
        /// <summary>
        /// Sets all public properties of <paramref name="target"/> equal to those of <paramref name="source"/>
        /// 
        /// Throws an Exception if either is null, or they are not the same type
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        public static void UpdateToMatch(this object target, object source, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            if (target == null || source == null || target.GetType() != source.GetType())
                throw new Exception();

            PropertyInfo[] properties = target.GetType().GetProperties(bindingFlags);

            foreach (var property in properties)
            {
                if (property.CanWrite)
                    property.SetValue(target, property.GetValue(source));
            }
        }

        public static void SetAllToDefault(this object obj, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(bindingFlags);

            foreach (var property in properties)
            {
                if (property.CanWrite)
                    property.SetValue(obj, default);
            }
        }
    }
}
