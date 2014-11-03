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
            ChannelName = channelName;
            SiteUrl = url;
            CultureName = cultureName;

            KeyMap = new Dictionary<string, Guid>();
            ExistingKeys = new List<string>();

            // TODO: This needs to be extracted into a provider-type model
            fileLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "key-map.txt");
            if (File.Exists(fileLocation))
            {
                string text = File.ReadAllText(fileLocation);
                foreach (string line in text.Split(Environment.NewLine.ToCharArray()))
                {
                    if (!line.Contains(":"))
                    {
                        continue;
                    }

                    KeyMap.Add(
                        line.Split(':')[0],
                        Guid.Parse(line.Split(':')[1])
                        );
                }
            }
        }

        public string ChannelName { get; private set; }
        public string CultureName { get; set; }
        public string SiteUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        // TODO: Need intelligent defaults for these two
        public string PageNameKey { get; set; }
        public string ExternalIdKey { get; set; }

        public Dictionary<string, Guid> KeyMap { get; set; }
        public List<string> ExistingKeys { get; set; }


        public void Process(object obj)
        {
            Dictionary<string, object> dictionary;

            // We need to turn the incoming object into a Dictionary. How we do this depends on what it is.
            // TODO: There's no-doubt a much more elegant way of doing this.
            if (obj is IDictionary<string, object>)
            {
                dictionary = obj as Dictionary<string, object>;
            }
            else if (obj is DataRow)
            {
                dictionary = DataRowToDictionary(obj as DataRow);
            }
            else if (obj is SqlDataReader)
            {
                dictionary = SqlDataReaderToDictionary(obj as SqlDataReader);
            }
            else if (obj is SqlCeDataReader)
            {
                // I hate that there's a different type for SqlCe...seems silly.
                dictionary = SqlCeDataReaderToDictionary(obj as SqlCeDataReader);
            }
            else if (obj is XmlElement)
            {
                dictionary = XmlElementToDictionary(obj as XmlElement);
            }
            else
            {
                // This is our catch-all. If this is a POCO object that doesn't fit any of the above types, then we're just going to reflect its properties into a dictionary
                dictionary = ObjectToDictionary(obj);
            }

            // At this point, no matter what was passed in, we have a Dictionary object. This is what we set properties from.

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
            Guid episerverKey = Guid.Empty;
            if (KeyMap.ContainsKey(externalKey))
            {
                episerverKey = KeyMap[externalKey];
            }

            // Add this to the list of keys that we know exist
            ExistingKeys.Add(externalKey);

            // Make the actual web service call
            ContentChannelServiceSoapClient service = GetService();

            // TODO: Need error handling around this
            Guid episerverId = service.ImportPage1(ChannelName, pageName, propertyKeys, propertyValues, CultureName, episerverKey, Guid.Empty, null);

            // Ensure this is valid inside the keymap
            KeyMap.Remove(externalKey);
            KeyMap.Add(externalKey, episerverId);
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
            File.WriteAllText(fileLocation, String.Empty);

            foreach (var entry in KeyMap)
            {
                File.AppendAllText(fileLocation, String.Concat(entry.Key, ":", entry.Value, Environment.NewLine));
            }
        }


        // Below are our "[Type]ToDictionary" methods. Again, there's a better way of doing this, I'm quite sure...

        // SqlCeDataReader
        private Dictionary<string, object> SqlCeDataReaderToDictionary(SqlCeDataReader reader)
        {
            var dictionary = new Dictionary<string, object>();
            for (int lp = 0; lp < reader.FieldCount; lp++)
            {
                dictionary.Add(reader.GetName(lp), reader.GetValue(lp));
            }
            return dictionary;
        }

        // DataRow
        private Dictionary<string, object> DataRowToDictionary(DataRow row)
        {
            return row.Table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row.Field<object>(col.ColumnName));
        }

        // SqlDataReader
        private Dictionary<string, object> SqlDataReaderToDictionary(SqlDataReader reader)
        {
            var dictionary = new Dictionary<string, object>();
            for (int lp = 0; lp < reader.FieldCount; lp++)
            {
                dictionary.Add(reader.GetName(lp), reader.GetValue(lp));
            }
            return dictionary;
        }

        // XmlElement
        private Dictionary<string, object> XmlElementToDictionary(XmlElement element)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (XmlElement child in element.ChildNodes)
            {
                dictionary.Add(child.Name, child.InnerText);
            }

            return dictionary;
        }

        // POCO object (our catch-all)
        private Dictionary<string, object> ObjectToDictionary(object obj)
        {
            var dictionary = new Dictionary<string, object>();

            // Reflect all the properties on the object and set them to properties of the same name and value
            foreach (PropertyInfo propertyDef in obj.GetType().GetProperties())
            {
                // If this property has an "Ignore" attribute, then skip it
                if (propertyDef.GetCustomAttributes(typeof (IgnoreAttribute), false).Any())
                {
                    continue;
                }

                dictionary.Add(propertyDef.Name, obj.GetType().GetProperty(propertyDef.Name).GetValue(obj));
            }

            return dictionary;
        }
    }
}