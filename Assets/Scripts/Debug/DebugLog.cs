using System;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace LocalCalendar.AppDebug
{
    public static class GeneralDebug
    {
        public static void DumpObj(object obj)
        {
            if (obj == null) Debug.Log("null");

            var type = obj.GetType();
            var fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            var sb = new StringBuilder();
            sb.AppendLine(type.Name);

            foreach (var f in fields)
            {
                sb.AppendLine($"  {f.Name}: {f.GetValue(obj)}");
            }

            Debug.Log(sb.ToString());
        }
    }
}
