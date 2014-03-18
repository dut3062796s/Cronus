using System.Linq;
using NMSD.Cronus.DomainModelling;
using NMSD.Cronus.Pipeline.Strategy;

namespace NMSD.Cronus.Pipeline.Config
{
    public class EndpointEventStoreConsumableSettings : EndpointConsumerSetting<IEvent>
    {
        public EndpointEventStoreConsumableSettings()
        {
            PipelineSettings.PipelineNameConvention = new EventStorePipelinePerApplication();
            PipelineSettings.EndpointNameConvention = new EventStoreEndpointPerBoundedContext(PipelineSettings.PipelineNameConvention);
        }

        protected override IEndpointConsumable BuildConsumer()
        {
            var consumer = new EndpointEventStoreConsumer(GlobalSettings.EventStores.Single(es => es.BoundedContext == BoundedContext), GlobalSettings.EventPublisher, GlobalSettings.CommandPublisher, MessagesAssemblies.First().ExportedTypes.First(), GlobalSettings.Serializer);
            return new EndpointConsumable(Transport.EndpointFactory, consumer);
        }
    }
}
