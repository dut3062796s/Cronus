﻿using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

namespace Elders.Cronus.Pipeline.Transport.InMemory
{
    public class InMemoryPipelineTransport : IPipelineTransport
    {
        public static int TotalMessagesConsumed { get; private set; }

        static ConcurrentDictionary<IPipeline, ConcurrentDictionary<IEndpoint, BlockingCollection<EndpointMessage>>> pipelineStorage = new ConcurrentDictionary<IPipeline, ConcurrentDictionary<IEndpoint, BlockingCollection<EndpointMessage>>>(new PipelineComparer());

        public void Bind(IPipeline pipeline, IEndpoint endpoint)
        {
            ConcurrentDictionary<IEndpoint, BlockingCollection<EndpointMessage>> endpointStorage;
            if (pipelineStorage.TryGetValue(pipeline, out endpointStorage))
            {
                if (endpointStorage.ContainsKey(endpoint))
                    return;
                endpointStorage.TryAdd(endpoint, new BlockingCollection<EndpointMessage>());
            }
        }

        public bool BlockDequeue(IEndpoint endpoint, uint timeoutInMiliseconds, out EndpointMessage msg)
        {
            msg = null;
            BlockingCollection<EndpointMessage> endpointStorage;
            if (TryGetEndpointStorage(endpoint, out endpointStorage))
            {

                if (endpointStorage.TryTake(out msg, (int)timeoutInMiliseconds))
                {
                    TotalMessagesConsumed++;
                    return true;
                }
                return false;
            }
            return false;
        }

        public IEndpoint GetOrAddEndpoint(EndpointDefinition endpointDefinition)
        {
            var pipeline = GetOrAddPipeline(endpointDefinition.PipelineName);
            IEndpoint endpoint;
            if (!TryGetEndpoint(endpointDefinition.EndpointName, out endpoint))
            {
                endpoint = new InMemoryEndpoint(this, endpointDefinition.EndpointName, endpointDefinition.RoutingHeaders);
                Bind(pipeline, endpoint);
            }
            return endpoint;
        }

        public IPipeline GetOrAddPipeline(string pipelineName)
        {
            var pipeline = new InMemoryPipeline(this, pipelineName);
            if (!pipelineStorage.ContainsKey(pipeline))
            {
                pipelineStorage.TryAdd(pipeline, new ConcurrentDictionary<IEndpoint, BlockingCollection<EndpointMessage>>(new EndpointComparer()));
            }
            return pipeline;
        }

        public void SendMessage(IPipeline pipeline, EndpointMessage message)
        {
            ConcurrentDictionary<IEndpoint, BlockingCollection<EndpointMessage>> endpointStorage;
            if (pipelineStorage.TryGetValue(pipeline, out endpointStorage))
            {
                foreach (var store in endpointStorage)
                {
                    var endpoint = store.Key;

                    bool accept = false;
                    foreach (var messageHeader in message.RoutingHeaders)
                    {
                        if (endpoint.RoutingHeaders.ContainsKey(messageHeader.Key))
                            accept = endpoint.RoutingHeaders[messageHeader.Key] == messageHeader.Value;
                        if (accept)
                            break;
                    }
                    if (!accept)
                        continue;

                    store.Value.TryAdd(message);
                }
            }
        }

        private bool TryGetEndpoint(string endpointName, out IEndpoint endpoint)
        {
            endpoint = null;
            var searchResult = (from pipeline in pipelineStorage
                                from es in pipelineStorage.Values
                                where es.Keys.Any(ep => ep.Name == endpointName)
                                select es)
                                .SingleOrDefault();
            if (searchResult != null)
                endpoint = searchResult.First().Key;

            return !ReferenceEquals(null, endpoint);
        }

        private bool TryGetEndpointStorage(IEndpoint endpoint, out BlockingCollection<EndpointMessage> endpointStorage)
        {
            endpointStorage = null;
            var searchResult = (from es in pipelineStorage.Values
                                where es.Keys.Contains(endpoint)
                                select es)
                                .SingleOrDefault();
            if (searchResult != null)
                endpointStorage = searchResult[endpoint];

            return !ReferenceEquals(null, endpointStorage);
        }

        public class PipelineComparer : IEqualityComparer<IPipeline>
        {
            public bool Equals(IPipeline x, IPipeline y)
            {
                return x.Name.Equals(y.Name);
            }

            public int GetHashCode(IPipeline obj)
            {
                return 133 ^ obj.Name.GetHashCode();
            }
        }

        public class EndpointComparer : IEqualityComparer<IEndpoint>
        {
            public bool Equals(IEndpoint x, IEndpoint y)
            {
                return x.Name.Equals(y.Name);
            }

            public int GetHashCode(IEndpoint obj)
            {
                return 101 ^ obj.Name.GetHashCode();
            }
        }

        public InMemoryPipelineTransport(IPipelineNameConvention pipelineNameConvention, IEndpointNameConvention endpointNameConvention)
        {
            this.EndpointFactory = new InMemoryEndpointFactory(this, endpointNameConvention);
            this.PipelineFactory = new InMemoryPipelineFactory(this, pipelineNameConvention);
        }

        public IEndpointFactory EndpointFactory { get; private set; }

        public IPipelineFactory<IPipeline> PipelineFactory { get; private set; }

        public void Dispose()
        {
        }
    }
}
