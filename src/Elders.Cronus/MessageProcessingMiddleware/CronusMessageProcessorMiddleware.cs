﻿using Elders.Cronus.Logging;
using Elders.Cronus.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elders.Cronus.MessageProcessingMiddleware
{
    public class CronusMessageProcessorMiddleware : Middleware<List<TransportMessage>, IFeedResult>, IMessageProcessor
    {
        Middleware<TransportMessage, IFeedResult> transportMessageMiddleware;

        public CronusMessageProcessorMiddleware(string name, Middleware<TransportMessage, IFeedResult> transportMessageMiddleware)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Name = name;

            this.transportMessageMiddleware = transportMessageMiddleware;
        }

        public string Name { get; private set; }

        protected override IFeedResult Invoke(List<TransportMessage> messages, MiddlewareExecution<List<TransportMessage>, IFeedResult> middlewareControl)
        {
            IFeedResult feedResult = FeedResult.Empty();
            try
            {
                messages.ForEach(msg =>
                {
                    var messageFeedResult = transportMessageMiddleware.Invoke(msg);
                    feedResult = feedResult.With(messageFeedResult);
                });
            }
            catch (Exception ex)
            {
                feedResult = feedResult.AppendUnitOfWorkError(messages, ex);
            }
            return feedResult;
        }
    }
}