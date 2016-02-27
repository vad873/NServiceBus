namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;

    class DevelopmentTransportDispatcher : IDispatchMessages
    {
        public DevelopmentTransportDispatcher()
        {
            basePath = Path.Combine("c:\\bus");
        }

        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            DispatchUnicast(outgoingMessages.UnicastTransportOperations, context);
            DispatchMulticast(outgoingMessages.MulticastTransportOperations, context);

            return TaskEx.CompletedTask;
        }

        void DispatchMulticast(IEnumerable<MulticastTransportOperation> transportOperations, ContextBag context)
        {
            foreach (var transportOperation in transportOperations)
            {
                var subscribers = GetSubscribersFor(transportOperation.MessageType);

                foreach (var subscriber in subscribers)
                {
                    WriteMessage(subscriber, transportOperation, context);
                }
            }
        }

        IEnumerable<string> GetSubscribersFor(Type messageType)
        {
            var eventDir = Path.Combine(basePath, ".events", messageType.FullName);

            foreach (var file in Directory.GetFiles(eventDir))
            {
                yield return File.ReadAllText(file);
            }
        }

        void DispatchUnicast(IEnumerable<UnicastTransportOperation> transportOperations, ContextBag context)
        {
            foreach (var transportOperation in transportOperations)
            {
                WriteMessage(transportOperation.Destination, transportOperation, context);
            }
        }

        void WriteMessage(string destination, IOutgoingTransportOperation transportOperation, ContextBag context)
        {
            var nativeMessageId = Guid.NewGuid().ToString();
            var destinationPath = Path.Combine(basePath, destination);
            var bodyPath = Path.Combine(destinationPath, ".bodies", nativeMessageId) + ".xml"; //TODO: pick the correct ending based on the serialized type

            File.WriteAllBytes(bodyPath, transportOperation.Message.Body);

            var messageContents = new List<string>
            {
                bodyPath,
                HeaderSerializer.ToXml(transportOperation.Message.Headers)
            };

            DirectoryBasedTransaction transaction;

            var messagePath = Path.Combine(destinationPath, nativeMessageId) + ".txt";

            if (transportOperation.RequiredDispatchConsistency != DispatchConsistency.Isolated &&
                context.TryGet(out transaction))
            {
                transaction.Enlist(messagePath, messageContents);
            }
            else
            {
                var tempFile = Path.GetTempFileName();

                //write to temp file first so we can do a atomic move 
                //this avoids the file being locked when the receiver tries to process it
                File.WriteAllLines(tempFile, messageContents);
                File.Move(tempFile, messagePath);
            }
        }

        string basePath;
    }
}