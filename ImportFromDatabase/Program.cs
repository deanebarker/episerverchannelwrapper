using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServerChannelLib;

namespace ImportFromDatabase
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get the articles
            var command = new SqlCeCommand();
            command.Connection = new SqlCeConnection("Data Source=Data.sdf;Persist Security Info=False;");
            command.Connection.Open();
            command.CommandText = "SELECT CONVERT(int,ID) as ID, Title as PageName, Excerpt as TeaserText, Text AS MainBody FROM Articles";
            var reader = command.ExecuteReader();


            // Open the channel
            var channel = new EPiServerChannel("Press Releases", "http://sandbox2.local/webservices/contentchannelservice.asmx");
            channel.Username = "page.importer";
            channel.Password = "page.importer";
            channel.PageNameKey = "PageName";
            channel.ExternalIdKey = "ID";

            while (reader.Read())
            {
                channel.Process(reader);
            }

            channel.Close();
        }
    }
}
