﻿using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using System;
using System.Text;

namespace Cone
{
    public class ConeTestNamer
    {
        static readonly Regex NormalizeNamePattern = new Regex(@"_|\+", RegexOptions.Compiled);
        static readonly Regex IsFormatStringPattern = new Regex(@"\{(\d(,.+?)?(:.+?)?)\}", RegexOptions.Compiled);

        readonly ParameterFormatter formatter = new ParameterFormatter();

        public string NameFor(MethodBase method) {
            var baseName = GetNameOf(method);
            var parameters = method.GetParameters();
            var displayParameters = Array.ConvertAll(parameters, x => x.Name);
            return string.Format(baseName, displayParameters);
        }

        public string GetNameOf(MethodBase method) {
            var nameAttribute = method.GetCustomAttributes(typeof(DisplayAsAttribute), true);
            if(nameAttribute.Length != 0)
                return ((DisplayAsAttribute)nameAttribute[0]).Name;
            return NormalizeNamePattern.Replace(method.Name, " ");
        }

        public string NameFor(MethodInfo method, object[] parameters) {
            return NameFor(method, parameters, GetNameOf(method));
        }

        public string NameFor(MethodInfo method, object[] parameters, string baseName) {
            if (parameters == null)
                return baseName;
            var displayArguments = DisplayParameters(parameters);
            if(IsFormatString(baseName))
                return string.Format(baseName, displayArguments);
            return string.Format("{0}({1})", baseName, FormatParameters(displayArguments));
        }

        object[] DisplayParameters(object[] arguments) { return Array.ConvertAll(arguments, Format); }

        object Format(object obj) { return formatter.AsWritable(obj); }

        string FormatParameters(object[] arguments) {
            var result = new StringBuilder();
            var sep = "";
            for(var i = 0; i != arguments.Length; ++i, sep = ", ")
                result.Append(sep).Append(arguments[i]);
            return result.ToString();
        }

        bool IsFormatString(string s) {
            return IsFormatStringPattern.IsMatch(s);
        }
    }
}
