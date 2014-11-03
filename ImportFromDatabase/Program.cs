using System.Data.SqlServerCe;
using EPiServerChannelLib;

namespace ImportFromDatabase
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Get the articles
            var command = new SqlCeCommand();
            command.Connection = new SqlCeConnection("Data Source=Data.sdf;Persist Security Info=False;");
            command.Connection.Open();
            command.CommandText = "SELECT ID as ExternalId, Title as PageName, Excerpt as TeaserText, Text AS MainBody FROM Articles";
            SqlCeDataReader reader = command.ExecuteReader();


            // Open the channel
            var channel = new EPiServerChannel("Press Releases")
            {
                SiteUrl = "http://sandbox2.local/",
                Username = "page.importer",
                Password = "page.importer",
                PageNameKey = "PageName",
                ExternalIdKey = "ExternalId"
            };

            while (reader.Read())
            {
                channel.Process(reader);
            }

            channel.Close();
        }
    }
}