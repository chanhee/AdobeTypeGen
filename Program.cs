using System;
using System.IO;
using System.Xml;


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

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            var classes = xmlDoc.GetElementsByTagName("classdef");
            foreach (XmlNode cc in classes) {
                Console.WriteLine(cc.Attributes["name"].Value);
            }
        }
    }

    public class ClassDef
    {
        public string Name { get; set; }
        public bool IsEnum { get; set; }
        public string Description { get; set; }
    }

    public class Property
    {

    }

    public class Method
    {

    }
}
