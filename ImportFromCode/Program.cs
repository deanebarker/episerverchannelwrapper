using System;
using System.Data;
using EPiServerChannelLib;
using Microsoft.SqlServer.Server;

namespace ImportFromCode
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var channel = new EPiServerChannel("Press Releases", "http://sandbox2.local/webservices/contentchannelservice.asmx");
            channel.Username = "page.importer";
            channel.Password = "page.importer";
            channel.PageNameKey = "PageName";
            channel.ExternalIdKey = "ExternalId";

            // Import from object
            var page = new Page
            {
                PageName = "Imported from Code",
                ExternalId = "imported-from-code",
                MainBody = "This is the body",
                TeaserText = "This is the teaser"
            };
            channel.Process(page);

            // Import from datarow
            var table = new DataTable();
            var pageName = new DataColumn()
            {
                DataType = typeof(string),
                ColumnName = "PageName"
            };
            var externalId = new DataColumn()
            {
                DataType = typeof(string),
                ColumnName = "ExternalId"
            };
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
            table.Columns.Add(pageName);
            table.Columns.Add(mainBody);
            table.Columns.Add(teaserText);
            table.Columns.Add(externalId);

            var row = table.NewRow();
            row["PageName"] = "Import from Data Row";
            row["ExternalId"] = "imported-from-data-row";
            row["MainBody"] = "This is the body";
            row["TeaserText"] = "This is the teaser text";

            channel.Process(row);

            channel.Close();
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