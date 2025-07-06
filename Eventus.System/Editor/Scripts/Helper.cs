using System;
using System.Collections.Generic;
using System.Linq;
using Eventus.Runtime;

namespace Eventus.Editor
{
    public static class Helper
    {
        public static bool IsMarkedAs(Channel channel, Type attributeType)
        {
            var memberInfo = typeof(Channel).GetMember(channel.ToString());
            return memberInfo.Length > 0 && Attribute.IsDefined(memberInfo[0], attributeType);
        }

        private static readonly HashSet<string> csharpKeywords = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while"
        };

        public static string FormatChannelName(string rawName, string currentCategory)
        {
            if (currentCategory == Global.DEFAULT_CATEGORY)
            {
                var parts = rawName.Split('_');
                if (parts.Length > 1)
                    // Ex: "[Player] Health"
                    return $"[{parts[0]}] {string.Join("_", parts.Skip(1))}";
            }
            else
            {
                var prefix = currentCategory + "_";
                if (rawName.StartsWith(prefix)) return rawName[prefix.Length..];
            }

            return rawName;
        }

        public static bool IsValidEnumName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (!char.IsLetter(name[0]) && name[0] != '_') return false;

            for (var i = 1; i < name.Length; i++)
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                    return false;

            return !csharpKeywords.Contains(name);
        }
    }
}