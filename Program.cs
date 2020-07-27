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

                Console.WriteLine(classDef + ": " + classDef.Description);
                classDefs.Add(classDef);
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

    public class Property
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsArray { get; set; }
        public object Value { get; set; }
        public string Description { get; set; }
        public bool ReadOnly { get; set; }
        public bool IsStatic { get; set; }
    }

    public class Method
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ReturnType { get; set; }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Description { get; set; }
    }
}
