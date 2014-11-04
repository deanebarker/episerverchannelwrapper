using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.ServiceModel;
using System.Xml;
using EPiServerChannelLib.ContentChannel;

namespace EPiServerChannelLib
{
    public class EPiServerChannel
    {
        private readonly string fileLocation;
        

        public EPiServerChannel(string channelName, string url = null, string cultureName = null)
        {
            // These are the defaults
            PageNameKey = "PageName";
            ExternalIdKey = "ExternalId";

            ChannelName = channelName;
            SiteUrl = url;
            CultureName = cultureName;

            KeyMap = new Dictionary<string, Guid>();
            ExistingKeys = new List<string>();

            RecordManager = new FileSystemRecordManager();
        }

        public string ChannelName { get; private set; }
        public string CultureName { get; set; }
        public string SiteUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public IRecordManager RecordManager { get; set; }

        // TODO: Need intelligent defaults for these two
        public string PageNameKey { get; set; }
        public string ExternalIdKey { get; set; }

        public Dictionary<string, Guid> KeyMap { get; set; }
        public List<string> ExistingKeys { get; set; }


        public void Process(DataRow row)
        {
            var dictionary = row.Table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row.Field<object>(col.ColumnName));
            Process(dictionary);
        }

        public void Process(SqlCeDataReader reader)
        {
            var dictionary = new Dictionary<string, object>();
            for (int lp = 0; lp < reader.FieldCount; lp++)
            {
                dictionary.Add(reader.GetName(lp), reader.GetValue(lp));
            }
            Process(dictionary);
        }

        public void Process(SqlDataReader reader)
        {
            var dictionary = new Dictionary<string, object>();
            for (int lp = 0; lp < reader.FieldCount; lp++)
            {
                dictionary.Add(reader.GetName(lp), reader.GetValue(lp));
            }
            Process(dictionary);
        }

        public void Process(XmlDocument doc)
        {
            // Call the overload for XmlElement for the root element
            Process(doc.DocumentElement);
        }

        public void Process(XmlElement element)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (XmlElement child in element.ChildNodes)
            {
                dictionary.Add(child.Name, child.InnerText);
            }

            Process(dictionary);
        }

       public void Process(object obj)
        {
            var dictionary = new Dictionary<string, object>();

            // Reflect all the properties on the object and set them to properties of the same name and value
            foreach (PropertyInfo propertyDef in obj.GetType().GetProperties())
            {
                // If this property has an "Ignore" attribute, then skip it
                if (propertyDef.GetCustomAttributes(typeof(IgnoreAttribute), false).Any())
                {
                    continue;
                }

                dictionary.Add(propertyDef.Name, obj.GetType().GetProperty(propertyDef.Name).GetValue(obj));
            }

            Process(dictionary);
        }

        // This is the core Process method. All other overloaded calls to Process(whatever) simply turn their input into a Dictionary and then call this method.
        public void Process(Dictionary<string, object> dictionary)
        {

            RecordManager.Init();

            var propertyKeys = new ArrayOfString();
            var propertyValues = new ArrayOfString();

            // TODO: This should throw an exception if either of the two are missing. (Well, it actually will based on the code below, but it should be handled...)
            string pageName = Convert.ToString(dictionary[PageNameKey]);
            string externalKey = Convert.ToString(dictionary[ExternalIdKey]);

            foreach (var entry in dictionary)
            {
                propertyKeys.Add(entry.Key);
                propertyValues.Add(Convert.ToString((entry.Value)));
            }

            // If this external key is in the KeyMap, then ensure it has an EPiServer GUID set
            var episerverKey = RecordManager.GetEPiServerGuid(externalKey);

            // Add this to the list of keys that we know exist
            ExistingKeys.Add(externalKey);

            // Make the actual web service call
            ContentChannelServiceSoapClient service = GetService();

            // TODO: Need error handling around this
            Guid episerverId = service.ImportPage1(ChannelName, pageName, propertyKeys, propertyValues, CultureName, episerverKey, Guid.Empty, null);

            // Ensure this is valid inside the keymap
            RecordManager.AddEPiServerGuid(externalKey, episerverId);
        }

        public int ProcessDeletions()
        {
            int counter = 0;
            ContentChannelServiceSoapClient service = GetService();

            string[] pagesInEPiServer = KeyMap.Keys.ToArray();

            // Loop through everything in the KeyMap (which represents all the pages we're maintaining in EPiServer)
            foreach (string entry in pagesInEPiServer)
            {
                // If you have something in the KeyMap which isn't in the ExistingKeys, then this item has been deleted since the last run
                if (!ExistingKeys.Contains(entry))
                {
                    counter++;

                    // Delete the page from EPiServer, and delete it from the KeyMap
                    service.DeletePage(ChannelName, KeyMap[entry]);
                    KeyMap.Remove(entry);
                }
            }

            return counter;
        }

        public ContentChannelServiceSoapClient GetService()
        {
            ContentChannelServiceSoapClient service;
            if (!String.IsNullOrWhiteSpace(SiteUrl))
            {
                // Append the path to the web service
                // TODO: Should probably be abstracted to a constant
                var serviceUrl = new Uri(new Uri(SiteUrl), "webservices/contentchannelservice.asmx").AbsoluteUri;
                
                var binding = new BasicHttpBinding
                {
                    Security = new BasicHttpSecurity
                    {
                        Mode = BasicHttpSecurityMode.TransportCredentialOnly,
                        Transport = new HttpTransportSecurity
                        {
                            ClientCredentialType = HttpClientCredentialType.Basic
                        }
                    }
                };

                // A URL was manually specified. Use it.
                service = new ContentChannelServiceSoapClient(binding, new EndpointAddress(serviceUrl));

                service.ClientCredentials.UserName.UserName = Username;
                service.ClientCredentials.UserName.Password = Password;
            }
            else
            {
                // Use the URL out of the config file
                service = new ContentChannelServiceSoapClient();
            }


            return service;
        }

        // TODO: Could this be moved into Dispose()?  It needs to be called at the end of every usage of the object, because it rewrites the KeyMap
        public void Close()
        {
            RecordManager.Close();
        }

    }
}