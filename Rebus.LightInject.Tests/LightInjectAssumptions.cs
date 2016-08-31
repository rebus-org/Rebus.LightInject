﻿using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.LightInject.Tests
{
    [TestFixture]
    public class LightInjectAssumptions
    {

        [Test]
        public void RegisterWorks()
        {
            var factory = new LightInjectContainerAdapterFactory();

            factory.RegisterHandlerType<SomeHandler>();
            factory.RegisterHandlerType<AnotherHandler>();

            using (var context = new DefaultTransactionContext())
            {
                const string stringMessage = "bimse";
                
                var handlers = factory.GetActivator().GetHandlers(stringMessage, context).Result.ToList();
                
                Assert.That(handlers.Count, Is.EqualTo(2));
            }
        }

        class SomeHandler : IHandleMessages<string>
        {
            public Task Handle(string message)
            {
                throw new NotImplementedException();
            }
        }

        class AnotherHandler : IHandleMessages<string>
        {
            public Task Handle(string message)
            {
                throw new NotImplementedException();
            }
        }

    }
}
