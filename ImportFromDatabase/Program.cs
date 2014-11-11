using System.Data.SqlServerCe;
using EPiServerChannelLib;
using ImportFromDatabase.RecordManagers;

namespace ImportFromDatabase
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Note: you're going to need to change this path...
            // If you're debugging, this needs to be somewhere stable, and not just in the bin/debug directory, because it will get overwritten there, which defeats the purpose of having it...
            string rmDatabaseLocation = @"C:\Data\Code\EPiServerChannelWrapper\ImportFromDatabase\RecordManager.sdf";

            // Get the articles
            var command = new SqlCeCommand
            {
                Connection = new SqlCeConnection("Data Source=Data.sdf;Persist Security Info=False;"),
                CommandText = "SELECT ID as ExternalId, Title as PageName, Excerpt as TeaserText, Text AS MainBody FROM Articles",
            };
            command.Connection.Open();
            SqlCeDataReader reader = command.ExecuteReader();

            // Open the channel
            var channel = new EPiServerChannel("Press Releases", "http://sandbox2.local/", "page.importer", "page.importer")
            {
                RecordManager = new SqlCeRecordManager(rmDatabaseLocation)
            };

            while (reader.Read())
            {
                channel.Process(reader);
            }

            channel.Close();
        }
    }
}