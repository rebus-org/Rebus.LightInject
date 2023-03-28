using System;
using System.Collections.Generic;
using System.Linq;
using LightInject;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.LightInject.Tests;

public class LightInjectActivationContext : IActivationContext
{
    public IHandlerActivator CreateActivator(Action<IHandlerRegistry> configureHandlers, out IActivatedContainer container)
    {
        var lightInjectContainer = new ServiceContainer();
        configureHandlers(new HandlerRegistry(lightInjectContainer));

        container = new ActivatedContainer(lightInjectContainer);

        return new LightInjectContainerAdapter(lightInjectContainer);
    }

    public IBus CreateBus(Action<IHandlerRegistry> configureHandlers, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container)
    {
        var lightInjectContainer = new ServiceContainer();
        configureHandlers(new HandlerRegistry(lightInjectContainer));

        container = new ActivatedContainer(lightInjectContainer);

        return configureBus(Configure.With(new LightInjectContainerAdapter(lightInjectContainer))).Start();
    }

    private class HandlerRegistry : IHandlerRegistry
    {
        private readonly ServiceContainer _serviceContainer;

        public HandlerRegistry(ServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }

        public IHandlerRegistry Register<THandler>() where THandler : class, IHandleMessages
        {
            foreach (var handlerInterfaceType in GetHandlerInterfaces<THandler>())
            {
                var componentName = $"{typeof(THandler).FullName}:{handlerInterfaceType.FullName}";

                _serviceContainer.Register(handlerInterfaceType, typeof(THandler), componentName);
            }

            return this;
        }

        static IEnumerable<Type> GetHandlerInterfaces<THandler>() where THandler : class, IHandleMessages
        {
#if NETSTANDARD1_6
            return typeof(THandler).GetInterfaces().Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
#else
            return typeof(THandler).GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
#endif
        }
    }

    private class ActivatedContainer : IActivatedContainer
    {
        private readonly ServiceContainer _serviceContainer;

        public ActivatedContainer(ServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }

        public void Dispose()
        {
            _serviceContainer.Dispose();
        }

        public IBus ResolveBus()
        {
            return _serviceContainer.GetInstance<IBus>();
        }
    }
}