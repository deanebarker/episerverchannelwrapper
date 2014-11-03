﻿using System.Data;
using EPiServerChannelLib;

namespace ImportFromCode
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Open the channel
            var channel = new EPiServerChannel("Press Releases")
            {
                SiteUrl = "http://sandbox2.local/",
                Username = "page.importer",
                Password = "page.importer",
                PageNameKey = "PageName",
                ExternalIdKey = "ExternalId"
            };

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

            DataRow row = table.NewRow();
            row["PageName"] = "Imported from Data Row";
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