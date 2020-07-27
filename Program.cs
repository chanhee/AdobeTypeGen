using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                System.Console.WriteLine(cls);
                foreach (var prop in cls.Props) {
                    System.Console.WriteLine($"\t{prop.Name}: {(prop.IsDataTypeArray ? "Array<"+prop.DataType+">" : prop.DataType)}");
                }
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
                        Console.WriteLine(param.Attribute("name"));
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
        public List<PropertyDef> Props { get; set; } = new List<PropertyDef>();
        public List<MethodDef> Methods { get; set; } = new List<MethodDef>();
        public override string ToString()
        {
            if (IsEnum) {
                return $"{Name}: Enum";
            }
            if (IsCollection) {
                return $"{Name}: Collection";
            }
            return $"{Name}: Class";
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
    }

    public class MethodDef
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ReturnType { get; set; }
        public bool IsReturnTypeArray { get; set; }
        public List<Parameter> Params { get; set; } = new List<Parameter>();
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsDataTypeArray { get; set; }
        public object Value { get; set; }
        public bool Optional { get; set; }
        public string Description { get; set; }
    }
}
