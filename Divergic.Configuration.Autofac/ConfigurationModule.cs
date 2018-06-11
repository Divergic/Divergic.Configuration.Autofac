﻿namespace Divergic.Configuration.Autofac
{
    using System;
    using System.Linq;
    using global::Autofac;

    /// <summary>
    /// The <see cref="ConfigurationModule"/>
    /// class is used to register nested configuration types.
    /// </summary>
    public class ConfigurationModule : Module
    {
        private readonly IConfigurationResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationModule"/>.
        /// </summary>
        /// <param name="resolver">The configuration resolver.</param>
        public ConfigurationModule(IConfigurationResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {
            var configuration = _resolver.Resolve();

            if (configuration == null)
            {
                return;
            }

            var configType = configuration.GetType();

            if (configType.IsValueType)
            {
                return;
            }

            if (configType == typeof(string))
            {
                return;
            }

            RegisterConfigTypes(builder, configuration);

            if (configuration.GetType().GetInterfaces().Any())
            {
                builder.RegisterInstance(configuration).AsImplementedInterfaces();
            }

            builder.RegisterInstance(configuration).AsSelf();
        }

        private static void RegisterConfigTypes(ContainerBuilder builder, object configuration)
        {
            // Register all the properties of the configuration as their interfaces This must be done
            // after registering assembly types and modules because type scanning may have already
            // registered the configuration classes as their interfaces which means Autofac will
            // return the default classes rather than these configuration instances that have values populated.
            var properties = configuration.GetType().GetProperties();

            foreach (var property in properties)
            {
                if (property.PropertyType.IsValueType)
                {
                    // Skip value types
                    continue;
                }

                if (property.PropertyType == typeof(string))
                {
                    // Skip strings
                    continue;
                }

                var value = property.GetValue(configuration);

                if (value == null)
                {
                    // There is no value on the property, skip to the next one
                    continue;
                }

                if (value.GetType().GetInterfaces().Any())
                {
                    builder.RegisterInstance(value).AsImplementedInterfaces();
                }

                builder.RegisterInstance(value).AsSelf();

                // Recurse into the child properties
                RegisterConfigTypes(builder, value);
            }
        }
    }
}