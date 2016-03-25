namespace NServiceBus.Features
{
    using System;
    using Config;
    using ConsistencyGuarantees;
    using Settings;
    using Transports;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    public class SecondLevelRetries : Feature
    {
        internal SecondLevelRetries()
        {
            EnableByDefault();

            DependsOn<DelayedDeliveryFeature>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use SLR since it requires receive capabilities");

            Prerequisite(IsEnabledInConfig, "SLR was disabled in config");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var retryPolicy = GetRetryPolicy(context.Settings);

            context.Container.RegisterSingleton(typeof(SecondLevelRetryPolicy), retryPolicy);

            if (context.Settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.None)
            {
                context.Pipeline.Register<SecondLevelRetriesNoTransactionBehavior.Registration>();

                context.Container.ConfigureComponent(b => new SecondLevelRetriesNoTransactionBehavior(retryPolicy, context.Settings.LocalAddress()), DependencyLifecycle.InstancePerCall);
            }
            else
            {
                context.Pipeline.Register<SecondLevelRetriesBehavior.Registration>();

                context.Container.ConfigureComponent(b => new SecondLevelRetriesBehavior(retryPolicy, context.Settings.LocalAddress(), b.Build<FailureInfoStorage>()), DependencyLifecycle.InstancePerCall);
            }
        }

        bool IsEnabledInConfig(FeatureConfigurationContext context)
        {
            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            if (retriesConfig == null)
            {
                return true;
            }

            if (retriesConfig.NumberOfRetries == 0)
            {
                return false;
            }

            return retriesConfig.Enabled;
        }

        static SecondLevelRetryPolicy GetRetryPolicy(ReadOnlySettings settings)
        {
            var customRetryPolicy = settings.GetOrDefault<Func<IncomingMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy");

            if (customRetryPolicy != null)
            {
                return new CustomSecondLevelRetryPolicy(customRetryPolicy);
            }

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null)
            {
                return new DefaultSecondLevelRetryPolicy(retriesConfig.NumberOfRetries, retriesConfig.TimeIncrease);
            }

            return new DefaultSecondLevelRetryPolicy(DefaultSecondLevelRetryPolicy.DefaultNumberOfRetries, DefaultSecondLevelRetryPolicy.DefaultTimeIncrease);
        }
    }
}