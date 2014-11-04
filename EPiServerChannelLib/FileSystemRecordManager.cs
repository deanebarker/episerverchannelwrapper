using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPiServerChannelLib
{
    public class FileSystemRecordManager : IRecordManager
    {
        private Dictionary<string, Guid> keyMap;
        private string path;
        private Boolean initialized;

        public FileSystemRecordManager()
        {
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "key-map.txt");
            keyMap = new Dictionary<string, Guid>();


        }

        public void Init()
        {
            // Only do this once...
            if (initialized)
            {
                return;
            }

            // TODO: This needs to be extracted into a provider-type model
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path);
                foreach (string line in text.Split(Environment.NewLine.ToCharArray()))
                {
                    if (!line.Contains(":"))
                    {
                        continue;
                    }

                    keyMap.Add(
                        line.Split(':')[0],
                        Guid.Parse(line.Split(':')[1])
                        );
                }
            }

            initialized = true;
        }

        public Guid GetEPiServerGuid(string key)
        {
            if (keyMap.ContainsKey(key))
            {
                return keyMap[key];
            }

            return Guid.Empty;
        }

        public void AddEPiServerGuid(string key, Guid pageGuid)
        {
            keyMap.Remove(key);
            keyMap.Add(key, pageGuid);
        }

        public void Close()
        {
            File.WriteAllText(path, String.Empty);

            foreach (var entry in keyMap)
            {
                File.AppendAllText(path, String.Concat(entry.Key, ":", entry.Value, Environment.NewLine));
            }
        }

    }
}
