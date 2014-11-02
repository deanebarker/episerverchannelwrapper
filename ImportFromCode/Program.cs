using System;
using System.Data;
using EPiServerChannelLib;

namespace ImportFromCode
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var channel = new EPiServerChannel("Press Releases", "http://sandbox2.local/webservices/contentchannelservice.asmx");
            channel.Username = "page.importer";
            channel.Password = "page.importer";

            // Import from object
            var page = new Page
            {
                MainBody = "This is the body",
                TeaserText = "This is the teaser"
            };
            channel.Process("Imported from Code", "imported-from-code", page);

            // Import from datarow
            var table = new DataTable();
            var mainBody = new DataColumn()
            {
                DataType = typeof (string),
                ColumnName = "MainBody"
            };
            var teaserText = new DataColumn()
            {
                DataType = typeof(string),
                ColumnName = "TeaserText"
            };
            table.Columns.Add(mainBody);
            table.Columns.Add(teaserText);
            var row = table.NewRow();

            row["MainBody"] = "This is the body";
            row["TeaserText"] = "This is the teaser text";

            channel.Process("Imported from Data Row", "imported-from-data-row", row);

            channel.Close();
        }
    }

    internal class Page
    {
        [Ignore]
        public string SomePropertyThatShouldNotBeMappedAndWillNeverBeSet { get; set; }

        public string TeaserText { get; set; }
        public string MainBody { get; set; }
    }
}