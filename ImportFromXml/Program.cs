using EPiServerChannelLib;
using System;
using System.IO;
using System.Xml;

namespace ImportFromXml
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Open the channel
            var channel = new EPiServerChannel("Press Releases", "http://sandbox2.local/", string.Empty, "page.importer", "page.importer");

            foreach (FileInfo file in new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "content")).GetFiles("*.xml"))
            {
                // Parse the document
                var doc = new XmlDocument();
                doc.Load(file.FullName);

                // Inject the filename (which we're using as a key) into a temporary element, so it can be found by the channel (alternately, you could manually put it there, but then you're trusting the editors...)
                XmlElement externalIdElement = doc.CreateElement("ExternalId");
                externalIdElement.InnerText = file.Name;
                doc.DocumentElement.AppendChild(externalIdElement);

                channel.Process(doc);
            }

            channel.Close();
        }
    }
}