using System;
using System.Collections.Generic;
using System.IO;

namespace EPiServerChannelLib.RecordManagers
{
    public class FileSystemRecordManager : IRecordManager
    {
        private readonly Dictionary<string, Guid> keyMap;
        private readonly string path;
        private Boolean initialized;

        public FileSystemRecordManager()
        {
            this.path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "key-map.txt");
            this.keyMap = new Dictionary<string, Guid>();
        }

        public void Init()
        {
            // Only do this once...
            if (this.initialized)
            {
                return;
            }

            // TODO: This needs to be extracted into a provider-type model
            if (File.Exists(path))
            {
                string text = File.ReadAllText(this.path);
                foreach (string line in text.Split(Environment.NewLine.ToCharArray()))
                {
                    if (!line.Contains(":"))
                    {
                        continue;
                    }

                    this.keyMap.Add(
                        line.Split(':')[0],
                        Guid.Parse(line.Split(':')[1])
                        );
                }
            }

            this.initialized = true;
        }

        public Guid GetEPiServerGuid(string key)
        {
            if (this.keyMap.ContainsKey(key))
            {
                return this.keyMap[key];
            }

            return Guid.Empty;
        }

        public void AddEPiServerGuid(string key, Guid pageGuid)
        {
            this.keyMap.Remove(key);
            this.keyMap.Add(key, pageGuid);
        }

        public void Close()
        {
            File.WriteAllText(this.path, String.Empty);

            foreach (var entry in this.keyMap)
            {
                File.AppendAllText(this.path, String.Concat(entry.Key, ":", entry.Value, Environment.NewLine));
            }
        }
    }
}