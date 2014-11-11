using System;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace EPiServerChannelLib.RecordManagers
{
    public class DDSRecordManager : IRecordManager
    {
        private readonly DynamicDataStore store;

        public DDSRecordManager() : this("ContentChannelKeyMap")
        {
        }

        public DDSRecordManager(string storeName)
        {
            store = DynamicDataStoreFactory.Instance.GetStore(storeName);
            if (store == null)
            {
                store = DynamicDataStoreFactory.Instance.CreateStore(storeName, typeof (ContentChannelKeyMap));
            }
        }

        public void AddEPiServerGuid(string key, Guid pageGuid)
        {
            store.Save(new ContentChannelKeyMap
            {
                EPiServerGuid = pageGuid,
                Id = Identity.NewIdentity(new Guid(key))
            });
        }

        public Guid GetEPiServerGuid(string key)
        {
            Identity id = Identity.NewIdentity(new Guid(key));
            var contentChannelKeyMap = store.Load<ContentChannelKeyMap>(id);

            if (contentChannelKeyMap != null)
            {
                return contentChannelKeyMap.EPiServerGuid;
            }
            return Guid.Empty;
        }

        public void Init()
        {
        }

        public void Close()
        {
        }

        private class ContentChannelKeyMap : IDynamicData
        {
            public ContentChannelKeyMap()
            {
                EPiServerGuid = Guid.Empty;
            }

            public Guid EPiServerGuid { get; set; }
            public Identity Id { get; set; }
        }
    }
}