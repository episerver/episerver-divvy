using EPiServer.Data;
using EPiServer.Data.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Divvy
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class DivvyLogEntry
    {
        public Identity Id { get; set; }
        public Guid RequestKey { get; set; }
        public string Message { get; set; }
        public DateTime Created { get; set; }


        public DivvyLogEntry()
        {

        }

        public static void Create(Guid requestKey, string message)
        {
            var entry = new DivvyLogEntry()
            {
                RequestKey = requestKey,
                Message = message,
                Created = DateTime.Now
            };
            GetStore().Save(entry);
        }

        public static List<DivvyLogEntry> Get(Guid requestKey)
        {
            return GetStore().Items<DivvyLogEntry>().Where(e => e.RequestKey == requestKey).ToList();
        }

        public static void Clear()
        {
            GetStore().DeleteAll();
        }

        private static DynamicDataStore GetStore()
        {
            return DynamicDataStoreFactory.Instance.CreateStore(typeof(DivvyLogEntry));
        }
    }
}