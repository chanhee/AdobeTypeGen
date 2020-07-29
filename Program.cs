﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;


namespace AdobeTypeGen
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) {
                Console.WriteLine("Usage: AdobeTypeGen <adobe-type>.xml > <adobe-type>.d.ts");
                return;
            }

            var xmlPath = args[0];

            if (!File.Exists(xmlPath)) {
                Console.Error.WriteLine($"File not exists: {xmlPath}");
            }

            // dictionary
            // - map
            //  - topicref
            //   - topicref
            // - package
            //  - classdef
            //   - elements
            //    - property
            //    - method
            var xmlDoc = XElement.Load(xmlPath);
            var classDefs = GenerateClassDefs(xmlDoc);
            foreach (var cls in classDefs) {
                Console.WriteLine(cls.ToExpr());
            }
        }

        public static List<ClassDef> GenerateClassDefs(XElement xmlDoc)
        {
            var ns = xmlDoc.GetDefaultNamespace();
            XName NsName(string name) => ns + name;

            // INFO: <topicref navtitle="Collections">
            var collections = xmlDoc.Descendants(NsName("topicref"))
                .First(x => (string)x.Attribute("navtitle") == "Collections")
                .Descendants().Select(x => (string)x.Attribute("navtitle"))
                .ToList();

            var classDefs = new List<ClassDef>();
            var classes = xmlDoc.Descendants(NsName("classdef"));
            foreach (var cls in classes) {
                var classDef = new ClassDef();
                classDef.Name = (string)cls.Attribute("name");
                classDef.IsEnum = (string)cls.Attribute("enumeration") == "true";
                classDef.IsCollection = collections.IndexOf(classDef.Name) > -1;
                classDef.Description = (string)cls.Element(NsName("shortdesc"));
                classDef.SuperClass = (string)cls.Element(NsName("superclass"));
                if (classDef.IsCollection && string.IsNullOrWhiteSpace(classDef.SuperClass)) {
                    classDef.SuperClass = (string)cls.Descendants(NsName("method"))
                        .First(x => "getByName" == (string)x.Attribute("name"))
                        .Element(NsName("datatype")).Element(NsName("type"));
                }
                classDefs.Add(classDef);

                var props = cls.Descendants(NsName("property"));
                foreach (var prop in props) {
                    var dataTypeEl = prop.Element(NsName("datatype"));
                    var propDef = new PropertyDef();
                    propDef.Name = (string)prop.Attribute("name");
                    propDef.ReadOnly = (string)prop.Attribute("rwaccess") == "readonly";
                    propDef.Description = (string)prop.Element(NsName("shortdesc"));
                    propDef.DataType = (string)dataTypeEl.Element(NsName("type"));
                    propDef.Min = (string)dataTypeEl.Element(NsName("min"));
                    propDef.Max = (string)dataTypeEl.Element(NsName("max"));
                    propDef.Value = (string)dataTypeEl.Element(NsName("value"));
                    propDef.IsDataTypeArray = dataTypeEl.Element(NsName("array")) != null;
                    classDef.Props.Add(propDef);
                }

                var methods = cls.Descendants(NsName("method"));
                foreach (var m in methods) {
                    var returnTypeEl = m.Element(NsName("datatype"));
                    var methodDef = new MethodDef();
                    methodDef.Name = (string)m.Attribute("name");
                    methodDef.Description = (string)m.Element(NsName("shortdesc"));
                    if (returnTypeEl != null) { // void
                        methodDef.ReturnType = (string)returnTypeEl.Element(NsName("type"));
                        methodDef.IsReturnTypeArray = returnTypeEl.Element(NsName("array")) != null;
                    }

                    var parameters = m.Descendants(NsName("parameter"));
                    foreach (var param in parameters) {
                        var paramTypeEl = param.Element(NsName("datatype"));
                        var paramDef = new ParameterDef();
                        paramDef.Name = (string)param.Attribute("name");
                        paramDef.Description = (string)param.Element(NsName("shortdesc"));
                        paramDef.Optional = (string)param.Attribute("optional") == "true";
                        paramDef.DataType = (string)paramTypeEl.Element(NsName("type"));
                        paramDef.IsDataTypeArray = paramTypeEl.Element(NsName("array")) != null;
                        paramDef.Value = (string)paramTypeEl.Element(NsName("value"));
                        methodDef.Params.Add(paramDef);
                    }

                    classDef.Methods.Add(methodDef);
                }
            }

            return classDefs;
        }
    }

    public class ClassDef
    {
        public string Name { get; set; }
        public string SuperClass { get; set; }
        public bool IsEnum { get; set; }
        public string Description { get; set; }
        public bool IsCollection { get; set; }
        public List<PropertyDef> Props { get; } = new List<PropertyDef>();
        public List<MethodDef> Methods { get; } = new List<MethodDef>();
        public string ToExpr()
        {
            if (IsEnum) {
                return $"declare enum {Name} {{\n" +
                       $"{string.Join("\n\n", Props.Select(p => p.ToEnumExpr()))}" +
                       $"\n}}\n\n";
            }
            else {
                var superClass = IsCollection ? $"Array<{SuperClass}>" : SuperClass;
                var sb = new StringBuilder();
                sb.Append($"declare class {Name}");
                sb.Append($"{(!string.IsNullOrWhiteSpace(superClass) ? $" extends {superClass}" : "")} {{\n");
                if (Props.Any()) {
                    sb.AppendJoin("\n\n", Props.Select(p => p.ToClassExpr()));
                }
                if (Props.Any() && Methods.Any()) {
                    sb.Append("\n\n");
                }
                if (Methods.Any()) {
                    sb.AppendJoin("\n\n", Methods.Select(m => m.ToExpr()));
                }
                sb.Append("\n}\n\n");
                return sb.ToString();
            }
        }
    }

    public class PropertyDef
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsDataTypeArray { get; set; }
        public object Value { get; set; }
        public string Min { get; set; }
        public string Max { get; set; }
        public string Description { get; set; }
        public bool ReadOnly { get; set; }

        public string ToClassExpr()
        {
            return $"  {(ReadOnly ? "readonly " : "")}" +
                   $"{Name}: " +
                   $"{TypeConverter.Convert(DataType, IsDataTypeArray)};";
        }

        public string ToEnumExpr()
        {
            return $"  {Name} = {Value},";
        }
    }

    public class MethodDef
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ReturnType { get; set; }
        public bool IsReturnTypeArray { get; set; }
        public List<ParameterDef> Params { get; } = new List<ParameterDef>();

        public string ToExpr()
        {
            var resultType = "void";
            if (!string.IsNullOrWhiteSpace(ReturnType)) {
                resultType = TypeConverter.Convert(ReturnType, IsReturnTypeArray);
            }

            var sb = new StringBuilder();
            return "  /**";
            return $"  {Name}({string.Join(", ", Params.Select(p => p.ToExpr()))}): {resultType};";
        }
    }

    public class ParameterDef
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsDataTypeArray { get; set; }
        public object Value { get; set; }
        public bool Optional { get; set; }
        public string Description { get; set; }

        public string ToExpr()
        {
            return $"{Name}" +
                   $"{(Optional ? "?" : "")}: " +
                   $"{TypeConverter.Convert(DataType, IsDataTypeArray)}";
        }
    }

    public class TypeConverter
    {
        public static string Convert(string srcType, bool isArray)
        {
            var destType = srcType switch {
                "int" => "number",
                "Int32" => "number",
                "bool" => "boolean",
                "Object" => "object",
                _ => srcType // string, number, other class
            };
            return destType + (isArray ? "[]" : string.Empty);
        }
    }
}
