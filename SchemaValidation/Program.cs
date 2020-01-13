using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace SchemaValidation
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlReaderSettings booksSettings = new XmlReaderSettings();
            booksSettings.Schemas.XmlResolver = new CustomXmlUrlResolver();
            booksSettings.Schemas.Add("http://www.contoso.com/books", "books.xsd");
            booksSettings.ValidationType = ValidationType.Schema;
            booksSettings.ValidationEventHandler += new ValidationEventHandler(booksSettingsValidationEventHandler);

            XmlReader books = XmlReader.Create("books.xml", booksSettings);

            while (books.Read()) { }
        }

        static void booksSettingsValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                Console.Write("WARNING: ");
                Console.WriteLine(e.Message);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                Console.Write("ERROR: ");
                Console.WriteLine(e.Message);
            }
        }

        public class CustomXmlUrlResolver : XmlUrlResolver
        {
            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                var path = LocalPathFromUri(absoluteUri);
                if (File.Exists(path))
                    return File.OpenRead(path);

                var baseStreamObject = base.GetEntity(absoluteUri, role, ofObjectToReturn);

                if(baseStreamObject is Stream baseStream)
                {
                    string content;
                    Encoding encoding;
                    using (var streamReader = new StreamReader(baseStream))
                    {
                        encoding = streamReader.CurrentEncoding;
                        content = streamReader.ReadToEnd();
                    }

                    if (content.Contains("</xs:schema>", StringComparison.OrdinalIgnoreCase)) // todo should find a better way?
                    {
                        File.WriteAllText(path, content); // save for next time
                        //return File.OpenRead(path); // slower, but fail faster?
                    }

                    return new MemoryStream(encoding.GetBytes(content));
                }

                return baseStreamObject;
            }

            private string LocalPathFromUri(Uri uri)
                => $"./{Regex.Replace(uri.ToString(), "^.+://|\\.xsd$", string.Empty).Replace("/", "!")}.xsd"; // todo probably a different path
        }
    }
}
