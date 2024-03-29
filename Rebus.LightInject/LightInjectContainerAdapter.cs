﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightInject;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.LightInject;

/// <summary>
/// Implementation of <see cref="IContainerAdapter"/> that uses LightInject to get handler instances
/// </summary>
public class LightInjectContainerAdapter : IContainerAdapter
{
    readonly IServiceContainer _serviceContainer;

    /// <summary>
    /// Constructs the adapter, using the specified container
    /// </summary>
    public LightInjectContainerAdapter(IServiceContainer container)
    {
        _serviceContainer = container ?? throw new ArgumentNullException(nameof(container));
    }

    /// <summary>
    /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
    /// </summary>
    public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
    {
        var handlerInstances = _serviceContainer
            .GetAllInstances<IHandleMessages<TMessage>>()
            .ToList();

        transactionContext.OnDisposed(_ =>
        {
            foreach (var handlerInstance in handlerInstances)
            {
                if (!(handlerInstance is IDisposable disposable)) continue;

                disposable.Dispose();
            }
        });

        return handlerInstances;
    }

    /// <summary>
    /// Stores the bus instance
    /// </summary>
    public void SetBus(IBus bus)
    {
        if (_serviceContainer.AvailableServices.Any(s => s.ServiceType == typeof(IBus)))
        {
            throw new InvalidOperationException("Cannot register IBus in this container because it has already been registered. If you want to host multiple Rebus instances in a single process, please use separate containers for them.");
        }

        _serviceContainer.Register(_ => bus, new PerContainerLifetime());
        _serviceContainer.Register(_ => GetCurrentMessageContext());
        _serviceContainer.Register(factory => factory.Create<IBus>().Advanced.SyncBus);

        // Force the per container lifetime to get the reference to the bus object so it will be disposed when the container is disposed
        _serviceContainer.GetInstance<IBus>();
    }

    /// <summary>
    /// Returns the current message context and ensures it is not null
    /// </summary>
    /// <returns>IMessageContext</returns>
    static IMessageContext GetCurrentMessageContext()
    {
        var currentMessageContext = MessageContext.Current;
        if (currentMessageContext == null)
        {
            throw new InvalidOperationException("Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
        }
        return currentMessageContext;
    }
}