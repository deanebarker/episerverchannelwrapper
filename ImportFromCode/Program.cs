using System.Collections.Generic;
using System.Data;
using EPiServerChannelLib;

namespace ImportFromCode
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Open the channel
            var channel = new EPiServerChannel("Press Releases", "http://sandbox2.local/", "page.importer", "page.importer");

            // Import from POCO object
            var poco = new Page
            {
                PageName = "Imported from Code via POCO",
                ExternalId = "imported-from-code-via-poco",
                MainBody = "This is the body",
                TeaserText = "This is the teaser"
            };
            channel.Process(poco);

            // Import from Anonymous object
            var anon = new
            {
                PageName = "Imported from Code via Anonymous Type",
                ExternalId = "imported-from-code-via-anon",
                MainBody = "This is the body",
                TeaserText = "This is the teaser"
            };
            channel.Process(anon);

            // Import from Dictionary
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("PageName", "Imported from Code via Dictionary");
            dictionary.Add("ExternalId", "imported-from-code-via-dictionary");
            dictionary.Add("MainBody", "This is the body.");
            dictionary.Add("TeaserText", "This is the teaser");
            channel.Process(dictionary);

            // Import from DataRow (the idea here is that you'd have an entire DataTable, then iterate the rows....)
            DataRow dataRow = GetTable().NewRow();
            dataRow["PageName"] = "Imported from Data Row";
            dataRow["ExternalId"] = "imported-from-data-row";
            dataRow["MainBody"] = "This is the body";
            dataRow["TeaserText"] = "This is the teaser text";
            channel.Process(dataRow);

            channel.Close();
        }

        private static DataTable GetTable()
        {
            // Just moving this out the code above for simplicity's sake

            var table = new DataTable();
            var pageName = new DataColumn
            {
                DataType = typeof (string),
                ColumnName = "PageName"
            };
            var externalId = new DataColumn
            {
                DataType = typeof (string),
                ColumnName = "ExternalId"
            };
            var mainBody = new DataColumn
            {
                DataType = typeof (string),
                ColumnName = "MainBody"
            };
            var teaserText = new DataColumn
            {
                DataType = typeof (string),
                ColumnName = "TeaserText"
            };
            table.Columns.Add(pageName);
            table.Columns.Add(mainBody);
            table.Columns.Add(teaserText);
            table.Columns.Add(externalId);

            return table;
        }
    }

    internal class Page
    {
        [Ignore]
        public string SomePropertyThatShouldNotBeMappedAndWillNeverBeSet { get; set; }

        public string PageName { get; set; }

        public string ExternalId { get; set; }

        public string TeaserText { get; set; }

        public string MainBody { get; set; }
    }
}