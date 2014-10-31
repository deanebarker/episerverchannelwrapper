using System;
using System.IO;
using System.Xml;
using EPiServerChannelLib;

namespace ImportFromXml
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Create the job
            var channel = new EPiServerChannel("Deane", "http://beaumontdemo.local/webservices/contentchannelservice.asmx");

            // Loop through the files
            foreach (FileInfo file in new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "content")).GetFiles("*.xml"))
            {
                // Parse the document
                var doc = new XmlDocument();
                doc.Load(file.FullName);

                // Configure the page
                var page = new Page
                {
                    MainBody = doc.SelectSingleNode("//body").InnerText,
                    TeaserText = doc.SelectSingleNode("//excerpt").InnerText
                };

                // Process it (this does the actual web service call)
                channel.Process(doc.SelectSingleNode("//title").InnerText, file.Name, page);
            }

            // Process the deletions (the ExistingKeys was populated by the Process method above)
            // If you're not processing EVERY external item, then you'd want to manually populate "ExistingKeys" with the value of every key before processing deletions.
            channel.ProcessDeletions();

            // Close the job, which rewrites the keymap
            channel.Close();
        }
    }

    public class Page
    {
        public string MainBody { get; set; }
        public string TeaserText { get; set; }
    }
}