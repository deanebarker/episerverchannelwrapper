using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using EPiServerChannelLib.ContentChannel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;

namespace EPiServerChannelLib
{
    public class EPiServerChannel
    {
        private readonly string fileLocation;

        public string ChannelName { get; private set; }
        public string CultureName { get; set; }
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string PageNameKey { get; set; }
        public string ExternalIdKey { get; set; }

        public Dictionary<string, Guid> KeyMap { get; set; }
        public List<string> ExistingKeys { get; set; }

        public EPiServerChannel(string channelName, string url = null, string cultureName = null)
        {
            ChannelName = channelName;
            Url = url;
            CultureName = cultureName;

            KeyMap = new Dictionary<string, Guid>();
            ExistingKeys = new List<string>();

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


        public void Process(object obj)
        {
            var propertyKeys = new ArrayOfString();
            var propertyValues = new ArrayOfString();

            // If it's not the right type, get it there.
            if (!(obj is IDictionary<string, object>))
            {
                // DataRow has a specific extension method for this
                if (obj is DataRow)
                {
                    obj = DataRowToDictionary(obj as DataRow);
                }
                else if (obj is SqlDataReader)
                {
                    obj = SqlDataReaderToDictionary(obj as SqlDataReader);
                }
                else if (obj is SqlCeDataReader)
                {
                    obj = SqlCeDataReaderToDictionary(obj as SqlCeDataReader);
                }
                else
                {
                    obj = ObjectToDictionary(obj);
                }        
            }

            var pageName = Convert.ToString(((IDictionary<string,object>) obj)[PageNameKey]);
            var externalKey = Convert.ToString(((IDictionary<string, object>)obj)[ExternalIdKey]);

            foreach (var entry in (Dictionary<string, Object>)obj)
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
            var service = GetService();
            Guid episerverId = service.ImportPage1(ChannelName, pageName, propertyKeys, propertyValues, CultureName, episerverKey, Guid.Empty, null);

            // Ensure this is valid inside the keymap
            KeyMap.Remove(externalKey);
            KeyMap.Add(externalKey, episerverId);
        }

        private Dictionary<string, object> SqlCeDataReaderToDictionary(SqlCeDataReader reader)
        {
            var dictionary = new Dictionary<string, object>();
            for (int lp = 0; lp < reader.FieldCount; lp++)
            {
                dictionary.Add(reader.GetName(lp), reader.GetValue(lp));
            }
            return dictionary;
        }

        private Dictionary<string, object> DataRowToDictionary(DataRow row)
        {
            return row.Table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row.Field<object>(col.ColumnName));
        }

        private Dictionary<string, object> SqlDataReaderToDictionary(SqlDataReader reader)
        {
            var dictionary = new Dictionary<string, object>();
            for (int lp = 0; lp < reader.FieldCount; lp++)
            {
                dictionary.Add(reader.GetName(lp), reader.GetValue(lp));
            }
            return dictionary;
        }

        private Dictionary<string, object> ObjectToDictionary(object obj)
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

            return dictionary;
        }

        public int ProcessDeletions()
        {
            int counter = 0;
            var service = GetService();

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
            if (!String.IsNullOrWhiteSpace(Url))
            {
                var binding = new BasicHttpBinding()
                {
                    Security = new BasicHttpSecurity()
                    {
                        Mode = BasicHttpSecurityMode.TransportCredentialOnly,
                        Transport = new HttpTransportSecurity()
                        {
                            ClientCredentialType = HttpClientCredentialType.Basic
                        }
                    }
                };
                
                // A URL was manually specified. Use it.
                service = new ContentChannelServiceSoapClient(binding, new EndpointAddress(Url));

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

        public void Close()
        {
            File.WriteAllText(fileLocation, String.Empty);

            foreach (var entry in KeyMap)
            {
                File.AppendAllText(fileLocation, String.Concat(entry.Key, ":", entry.Value, Environment.NewLine));
            }
        }
    }
}