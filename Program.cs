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
                Console.Error.WriteLine("Usage: AdobeTypeGen <adobe-type>.xml");
                return;
            }

            var xmlPath = args[0];

            if (!File.Exists(xmlPath)) {

            }
            var xmlDoc = new XmlDocument();
            // xmlDoc.load
        }
    }
}
