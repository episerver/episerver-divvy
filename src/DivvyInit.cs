using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace Episerver.Labs.Divvy
{
    [InitializableModule]
    public class DivvyInit : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var contentEvents = ServiceLocator.Current.GetInstance<IContentEvents>();
            contentEvents.PublishedContent += DivvyManager.HandlePublish;
            contentEvents.DeletingContent += DivvyManager.HandleDelete;
        }

        public void Uninitialize(InitializationEngine context)
        {

        }
    }


}