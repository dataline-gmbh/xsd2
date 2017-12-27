using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace Xsd2
{
    internal static class CodeDomExtensions
    {
        private static readonly HashSet<string> _attributesWithNames = new HashSet<string>()
        {
            "System.Xml.Serialization.XmlAttributeAttribute",
            "System.Xml.Serialization.XmlElementAttribute",
            "System.Xml.Serialization.XmlArrayItemAttribute",
            "System.Xml.Serialization.XmlEnumAttribute",
            "System.Xml.Serialization.XmlRootAttribute",
            "System.Xml.Serialization.XmlTypeAttribute"
        };

        public static bool IsNameArgument(this CodeAttributeArgument argument)
        {
            if (string.IsNullOrEmpty(argument.Name))
            {
                var expr = argument.Value as CodePrimitiveExpression;
                if (expr == null)
                    return false;
                return expr.Value is string;
            }

            return argument.Name == "Name";
        }

        public static bool IsAttributeWithName(this CodeAttributeDeclaration attribute)
        {
            return _attributesWithNames.Contains(attribute.Name);
        }

        public static string GetOriginalName(this CodeTypeMember member)
        {
            if (member.Name == "Items")
                return member.Name;

            foreach (CodeAttributeDeclaration attribute in member.CustomAttributes)
            {
                if (!attribute.IsAttributeWithName())
                    continue;
                var nameArgument = attribute.Arguments.Cast<CodeAttributeArgument>().FirstOrDefault(x => x.IsNameArgument());
                if (nameArgument != null)
                    return (string)((CodePrimitiveExpression)nameArgument.Value).Value;
            }

            return member.Name;
        }

        public static bool IsAnonymousType(this CodeTypeMember member)
        {
            var attribute = member
                .CustomAttributes
                .Cast<CodeAttributeDeclaration>()
                .FirstOrDefault(x => x.Name == "System.Xml.Serialization.XmlTypeAttribute");
            if (attribute == null)
                return false;

            return attribute.IsAnonymousTypeArgument();
        }

        public static bool IsRootType(this CodeTypeMember member)
        {
            var attribute = member
                .CustomAttributes
                .Cast<CodeAttributeDeclaration>()
                .FirstOrDefault(x => x.Name == "System.Xml.Serialization.XmlRootAttribute");
            return attribute != null;
        }

        public static bool IsAnonymousTypeArgument(this CodeAttributeDeclaration attribute)
        {
            var anonymousTypeArgument = attribute
                .Arguments
                .Cast<CodeAttributeArgument>()
                .FirstOrDefault(x => x.Name == "AnonymousType");
            return anonymousTypeArgument != null;
        }

        public static bool IsIncludeInSchemaFalse(this CodeTypeMember member)
        {
            var attribute = member
                .CustomAttributes
                .Cast<CodeAttributeDeclaration>()
                .FirstOrDefault(x => x.Name == "System.Xml.Serialization.XmlTypeAttribute");
            if (attribute == null)
                return false;

            var includeInSchemaArgument = attribute
                .Arguments
                .Cast<CodeAttributeArgument>()
                .FirstOrDefault(x => x.Name == "IncludeInSchema");
            if (includeInSchemaArgument == null)
                return false;

            return !(bool)((CodePrimitiveExpression)includeInSchemaArgument.Value).Value;
        }

        public static string GetNamespace(this CodeTypeMember member)
        {
            var attribute = member
                .CustomAttributes
                .Cast<CodeAttributeDeclaration>()
                .FirstOrDefault(x => x.Name == "System.Xml.Serialization.XmlTypeAttribute");
            if (attribute == null)
                return null;

            var namespaceArgument = attribute
                .Arguments
                .Cast<CodeAttributeArgument>()
                .FirstOrDefault(x => x.Name == "Namespace");
            if (namespaceArgument == null)
                return string.Empty;

            return (string) ((CodePrimitiveExpression) namespaceArgument.Value).Value;
        }

        public static string GetXmlName(this CodeTypeMember member)
        {
            return GetXmlName(member, "System.Xml.Serialization.XmlTypeAttribute", "TypeName") ??
                GetXmlName(member, "System.Xml.Serialization.XmlRootAttribute", "ElementName") ??
                GetXmlName(member, "System.Xml.Serialization.XmlElementAttribute", "ElementName") ??
                GetXmlName(member, "System.Xml.Serialization.XmlAttributeAttribute", "AttributeName") ??
                member.Name;
        }

        public static CodeTypeReference[] GetXmlTypes(this CodeTypeMember member)
        {
            var attributes = member
                .CustomAttributes
                .Cast<CodeAttributeDeclaration>()
                .Where(x => x.Name == "System.Xml.Serialization.XmlElementAttribute" || x.Name == "System.Xml.Serialization.XmlAttributeAttribute");

            CodeTypeReference GetTypeName(CodeAttributeDeclaration attribute)
            {
                var arguments = attribute
                    .Arguments
                    .Cast<CodeAttributeArgument>()
                    .ToArray();

                var typeArgument = arguments.FirstOrDefault(x => x.Name == "Type");
                if (typeArgument == null && arguments.Length >= 2)
                {
                    // Is the second parameter a type?
                    var arg2 = arguments[1];
                    if (string.IsNullOrEmpty(arg2.Name) && arg2.Value is CodeTypeOfExpression)
                        typeArgument = arg2;
                }
                if (typeArgument == null && arguments.Length == 1 && string.IsNullOrEmpty(arguments[0].Name) && arguments[0].Value is CodeTypeOfExpression)
                    typeArgument = arguments[0];

                if (typeArgument == null)
                    return null;

                return ((CodeTypeOfExpression)typeArgument.Value).Type;
            }

            return attributes.Select(GetTypeName).Where(x => x != null).ToArray();
        }

        public static string GetXmlDataType(this CodeTypeMember member)
        {
            var attribute = member
                .CustomAttributes
                .Cast<CodeAttributeDeclaration>()
                .FirstOrDefault(x => x.Name == "System.Xml.Serialization.XmlElementAttribute" || x.Name == "System.Xml.Serialization.XmlAttributeAttribute");

            if (attribute == null)
                return null;

            var dataTypeArgument = attribute
                .Arguments
                .Cast<CodeAttributeArgument>()
                .FirstOrDefault(x => x.Name == "DataType");

            if (dataTypeArgument == null)
                return null;

            return (string)((CodePrimitiveExpression)dataTypeArgument.Value).Value;
        }

        private static string GetXmlName(CodeTypeMember member, string attributeTypeName, string nameParameterName)
        {
            var attribute = member
                .CustomAttributes
                .Cast<CodeAttributeDeclaration>()
                .FirstOrDefault(x => x.Name == attributeTypeName);
            if (attribute == null)
                return null;

            var namespaceArgument = attribute
               .Arguments
               .Cast<CodeAttributeArgument>()
               .FirstOrDefault(x => x.Name == nameParameterName);
            if (namespaceArgument != null)
                return (string)((CodePrimitiveExpression)namespaceArgument.Value).Value;

            var firstArgument = attribute.Arguments.Cast<CodeAttributeArgument>().FirstOrDefault();
            if (firstArgument != null && string.IsNullOrEmpty(firstArgument.Name))
                return (string)((CodePrimitiveExpression)firstArgument.Value).Value;

            return null;
        }

        public static IEnumerable<string> GetNamesFromItems(this CodeNamespace codeNamespace, string typeName)
        {
            foreach (CodeTypeDeclaration codeType in codeNamespace.Types)
            {
                foreach (CodeTypeMember member in codeType.Members)
                {
                    if (member.Name != "Items")
                        continue;

                    foreach (CodeAttributeDeclaration attribute in member.CustomAttributes)
                    {
                        if (attribute.Name != "System.Xml.Serialization.XmlElementAttribute")
                            continue;

                        var itemName = (string)((CodePrimitiveExpression)attribute.Arguments[0].Value).Value;
                        foreach (CodeAttributeArgument argument in attribute.Arguments)
                        {
                            var typeOfExpr = argument.Value as CodeTypeOfExpression;
                            if (typeOfExpr != null)
                            {
                                if (!string.IsNullOrEmpty(typeOfExpr.Type.BaseType) && typeOfExpr.Type.BaseType == typeName)
                                    yield return itemName;

                                if (typeOfExpr.Type.ArrayElementType != null && typeOfExpr.Type.ArrayElementType.BaseType == typeName)
                                    yield return itemName;
                            }
                        }
                    }
                }
            }
        }
    }
}
