using EPiServer.Data;
using EPiServer.Data.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

namespace Episerver.Labs.Divvy
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class DivvyContentMapping
    {
        public DivvyContentMapping()
        {
        }

        public DivvyContentMapping(int episerverContentId, int divvyId)
        {
            EpiserverContentId = episerverContentId;
            DivvyId = divvyId;
            CreatedOn = DateTime.Now;
            Status = ActiveStatusLabel;
            RequestKey = DivvyLogManager.GetRequestLogKey();
        }

        public static string ArchivedStatusLabel = "archived";
        public static string ActiveStatusLabel = "active";

        public Identity Id { get; set; }
        public int EpiserverContentId { get; set; }
        public int DivvyId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ArchivedOn { get; set; }
        public Guid RequestKey { get; set; }


        // Static methods to maintain the mappings
        public static void Create(int episerverContentId, int divvyId)
        {
            var existingMapping = FindFromEpiserverContent(episerverContentId) ?? FindFromDivvyContent(divvyId);
            if (existingMapping != null)
            {
                // There is already a mapping for this
                return;
            }

            var contentMapping = new DivvyContentMapping(episerverContentId, divvyId);
            GetStore().Save(contentMapping);
        }

        public static void Save(DivvyContentMapping mapping)
        {
            GetStore().Save(mapping);
        }

        public static void Delete(int episerverId)
        {
            var mapping = GetStore().Items<DivvyContentMapping>().FirstOrDefault(m => m.EpiserverContentId == episerverId);
            if (mapping != null)
            {
                GetStore().Delete(mapping);
            }
        }

        public static void Archive(DivvyContentMapping mapping)
        {
            mapping.ArchivedOn = DateTime.Now;
            mapping.Status = ArchivedStatusLabel;
            var store = GetStore();
            store.Save(mapping);
        }

        public static void DeleteAll()
        {
            GetStore().DeleteAll();
        }

        public static void Delete(Guid id)
        {
            var mapping = GetStore().Items<DivvyContentMapping>().Where(m => m.Id.ExternalId == id).FirstOrDefault();
            if(mapping != null)
            {
                GetStore().Delete(mapping);
            }
        }

        public static IEnumerable<DivvyContentMapping> FindAll()
        {
            return GetStore().Items<DivvyContentMapping>().ToList();
        }

        public static DivvyContentMapping FindFromDivvyContent(int divvyId)
        {
            return GetStore().Items<DivvyContentMapping>().FirstOrDefault(m => m.DivvyId == divvyId);
        }

        public static DivvyContentMapping FindFromEpiserverContent(int episerverId)
        {
            return GetStore().Items<DivvyContentMapping>().FirstOrDefault(m => m.EpiserverContentId == episerverId);
        }

        public static bool HasDivvyMapping(int episerverId)
        {
            return FindFromEpiserverContent(episerverId) != null;
        }

        private static DynamicDataStore GetStore()
        {
            return DynamicDataStoreFactory.Instance.CreateStore(typeof(DivvyContentMapping));
        }
    }
}
