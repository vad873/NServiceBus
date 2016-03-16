namespace NServiceBus
{
    using System.Messaging;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Transports;

    class QueueCreator : ICreateQueues
    {
        public QueueCreator(MsmqSettings settings)
        {
            this.settings = settings;
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            foreach (var receivingAddress in queueBindings.ReceivingAddresses)
            {
                CreateQueueIfNecessary(receivingAddress, identity);
            }

            foreach (var sendingAddress in queueBindings.SendingAddresses)
            {
                CreateQueueIfNecessary(sendingAddress, identity);
            }

            return TaskEx.CompletedTask;
        }

        void CreateQueueIfNecessary(string address, string identity)
        {
            Guard.AgainstNullAndEmpty(nameof(address), address);

            var msmqAddress = MsmqAddress.Parse(address);
            
            Logger.Debug($"Trying to open queue '{address}'.");

            MessageQueue queue;
            if (!MsmqUtilities.TryOpenQueue(msmqAddress, out queue))
            {
                Logger.Warn($"Queue '{address}' does not exist.");
                Logger.Debug($"Creating queue: {address}");

                queue = CreateQueue(msmqAddress, identity, settings.UseTransactionalQueues);
            }

            if (queue != null)
            {
                using (queue)
                {
                    Logger.Debug("Setting queue permissions.");
                    QueuePermissions.SetPermissionsForQueue(queue, identity);
                }
            }
        }

        static MessageQueue CreateQueue(MsmqAddress msmqAddress, string account, bool transactional)
        {
            var queuePath = msmqAddress.PathWithoutPrefix;

            try
            {
                var queue = MessageQueue.Create(queuePath, transactional);

                Logger.DebugFormat($"Created queue, path: [{queuePath}], identity: [{account}], transactional: [{transactional}]");

                return queue;
            }
            catch (MessageQueueException ex)
            {
                if (msmqAddress.IsRemote && (ex.MessageQueueErrorCode == MessageQueueErrorCode.IllegalQueuePathName))
                {
                    return null;
                }
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueExists)
                {
                    //Solve the race condition problem when multiple endpoints try to create same queue (e.g. error queue).
                    return null;
                }

                Logger.Error($"Could not create queue {msmqAddress}. Processing will still continue.", ex);
            }

            return null;
        }

        MsmqSettings settings;
        static ILog Logger = LogManager.GetLogger<QueueCreator>();

        internal static string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();

        internal static string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();

        internal static string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();
    }
}