﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Management.EventHub;
using Microsoft.Azure.Management.EventHub.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Rest;
using Microsoft.Rest.Azure;

namespace Azure.Messaging.EventHubs.Tests
{
    /// <summary>
    ///  Provides a dynamically created Event Hub instance which exists only in the context
    ///  of the scope; disposal removes the instance.
    /// </summary>
    ///
    /// <seealso cref="System.IAsyncDisposable" />
    ///
    public sealed class EventHubScope : IAsyncDisposable
    {
        /// <summary>The manager for common live test resource operations.</summary>
        private static readonly LiveResourceManager ResourceManager = new LiveResourceManager();

        /// <summary>Serves as a sentinel flag to denote when the instance has been disposed.</summary>
        private bool _disposed = false;

        /// <summary>
        ///   The name of the Event Hub that was created.
        /// </summary>
        ///
        public string EventHubName { get; }

        /// <summary>
        ///   Flags whether the Event Hub was created for the current test run
        ///   or was retrieved from environment variables.
        /// </summary>
        ///
        public bool WasEventHubCreated { get; }

        /// <summary>
        ///   The consumer groups created and associated with the Event Hub, not including
        ///   the default consumer group which is created implicitly.
        /// </summary>
        ///
        public IReadOnlyList<string> ConsumerGroups { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="EventHubScope"/> class.
        /// </summary>
        ///
        /// <param name="eventHubHame">The name of the Event Hub that was created.</param>
        /// <param name="consumerGroups">The set of consumer groups associated with the Event Hub; the default consumer group is not included, as it is implicitly created.</param>
        /// <param name="wasEventHubCreated">Sets whether the Event Hub was created or read from environment variables.</param>
        ///
        private EventHubScope(string eventHubHame,
                              IReadOnlyList<string> consumerGroups,
                              bool wasEventHubCreated)
        {
            EventHubName = eventHubHame;
            ConsumerGroups = consumerGroups;
            WasEventHubCreated = wasEventHubCreated;
        }

        /// <summary>
        ///   Performs the tasks needed to remove the dynamically created
        ///   Event Hub.
        /// </summary>
        ///
        public async ValueTask DisposeAsync()
        {
            if (!WasEventHubCreated)
            {
                _disposed = true;
            }

            if (_disposed)
            {
                return;
            }

            var resourceGroup = TestEnvironment.EventHubsResourceGroup;
            var eventHubNamespace = TestEnvironment.EventHubsNamespace;
            var token = await ResourceManager.AquireManagementTokenAsync();
            var client = new EventHubManagementClient(new TokenCredentials(token)) { SubscriptionId = TestEnvironment.EventHubsSubscription };

            try
            {
                await ResourceManager.CreateRetryPolicy().ExecuteAsync(() => client.EventHubs.DeleteAsync(resourceGroup, eventHubNamespace, EventHubName));
            }
            catch
            {
                // This should not be considered a critical failure that results in a test failure.  Due
                // to ARM being temperamental, some management operations may be rejected.  Throwing here
                // does not help to ensure resource cleanup only flags the test itself as a failure.
                //
                // If an Event Hub fails to be deleted, removing of the associated namespace at the end of the
                // test run will also remove the orphan.
            }
            finally
            {
                client?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        ///   Performs the tasks needed to get or create a new Event Hub instance with the requested
        ///   partition count and a dynamically assigned unique name.
        /// </summary>
        ///
        /// <param name="partitionCount">The number of partitions that the Event Hub should be configured with.</param>
        /// <param name="caller">The name of the calling method; this is intended to be populated by the runtime.</param>
        ///
        public static Task<EventHubScope> CreateAsync(int partitionCount,
                                                      [CallerMemberName] string caller = "") => CreateAsync(partitionCount, Enumerable.Empty<string>(), caller);

        /// <summary>
        ///   Performs the tasks needed to get or create a new Event Hub instance with the requested
        ///   partition count and a dynamically assigned unique name.
        /// </summary>
        ///
        /// <param name="partitionCount">The number of partitions that the Event Hub should be configured with.</param>
        /// <param name="consumerGroups">The set of consumer groups to create and associate with the Event Hub; the default consumer group should not be included, as it is implicitly created.</param>
        /// <param name="caller">The name of the calling method; this is intended to be populated by the runtime.</param>
        ///
        public static Task<EventHubScope> CreateAsync(int partitionCount,
                                                      IEnumerable<string> consumerGroups,
                                                      [CallerMemberName] string caller = "") => BuildScope(partitionCount, consumerGroups, caller);

        /// <summary>
        ///   Creates an instance of <see cref="NamespaceProperties"/>, populates it from a connection string and returns it.
        /// </summary>
        ///
        /// <param name="connectionString">The connection string from the existing Event Hubs namespace</param>
        ///
        /// <returns>The <see cref="NamespaceProperties" /> that will be used in a given test run.</returns>
        ///
        /// <exception cref="ArgumentException">Occurs when <param name="connectionString"/> holds an invalid connection string.</exception>
        ///
        public static NamespaceProperties PopulateNamespacePropertiesFromConnectionString(string connectionString)
        {
            if (TryParseNamespace(connectionString, out string namespaceName))
            {
                return new NamespaceProperties(namespaceName, connectionString, wasNamespaceCreated: false);
            }

            throw new ArgumentException("An endpoint could not be found in the passed connection string");
        }

        /// <summary>
        ///   Performs the tasks needed to create a new Event Hubs namespace within a resource group, intended to be used as
        ///   an ephemeral container for the Event Hub instances used in a given test run.
        /// </summary>
        ///
        /// <returns>The key attributes for identifying and accessing a dynamically created Event Hubs namespace.</returns>
        ///
        public static async Task<NamespaceProperties> CreateNamespaceAsync()
        {
            var subscription = TestEnvironment.EventHubsSubscription;
            var resourceGroup = TestEnvironment.EventHubsResourceGroup;
            var token = await ResourceManager.AquireManagementTokenAsync();

            string CreateName() => $"net-eventhubs-{ Guid.NewGuid().ToString("D") }";

            using (var client = new EventHubManagementClient(new TokenCredentials(token)) { SubscriptionId = subscription })
            {
                var location = await ResourceManager.QueryResourceGroupLocationAsync(token, resourceGroup, subscription);

                var eventHubsNamespace = new EHNamespace(sku: new Sku("Standard", "Standard", 12), tags: ResourceManager.GenerateTags(), isAutoInflateEnabled: true, maximumThroughputUnits: 20, location: location);
                eventHubsNamespace = await ResourceManager.CreateRetryPolicy<EHNamespace>().ExecuteAsync(() => client.Namespaces.CreateOrUpdateAsync(resourceGroup, CreateName(), eventHubsNamespace));

                var accessKey = await ResourceManager.CreateRetryPolicy<AccessKeys>().ExecuteAsync(() => client.Namespaces.ListKeysAsync(resourceGroup, eventHubsNamespace.Name, TestEnvironment.EventHubsDefaultSharedAccessKey));
                return new NamespaceProperties(eventHubsNamespace.Name, accessKey.PrimaryConnectionString, wasNamespaceCreated: true);
            }
        }

        /// <summary>
        ///   Performs the tasks needed to remove an ephemeral Event Hubs namespace used as a container for Event Hub instances
        ///   for a specific test run.
        /// </summary>
        ///
        /// <param name="namespaceName">The name of the namespace to delete.</param>
        ///
        public static async Task DeleteNamespaceAsync(string namespaceName)
        {
            var subscription = TestEnvironment.EventHubsSubscription;
            var resourceGroup = TestEnvironment.EventHubsResourceGroup;
            var token = await ResourceManager.AquireManagementTokenAsync();

            using (var client = new EventHubManagementClient(new TokenCredentials(token)) { SubscriptionId = subscription })
            {
                await ResourceManager.CreateRetryPolicy().ExecuteAsync(() => client.Namespaces.DeleteAsync(resourceGroup, namespaceName));
            }
        }

        /// <summary>
        ///   Builds a new scope based on the Event Hub that is named using <see cref="TestEnvironment.EventHubName" />, if specified.  If not,
        ///   a new EventHub is created for the scope.
        /// </summary>
        ///
        /// <param name="partitionCount">The number of partitions that the Event Hub should be configured with.</param>
        /// <param name="consumerGroups">The set of consumer groups to create and associate with the Event Hub; the default consumer group should not be included, as it is implicitly created.</param>
        /// <param name="caller">The name of the calling method; this is intended to be populated by the runtime.</param>
        ///
        /// <returns>The <see cref="EventHubScope" /> that will be used in a given test run.</returns>
        ///
        /// <remarks>
        ///   This method assumes responsibility of tearing down any Azure resources that it directly creates.
        /// </remarks>
        ///
        private static Task<EventHubScope> BuildScope(int partitionCount,
                                                      IEnumerable<string> consumerGroups,
                                                      [CallerMemberName] string caller = "")
        {
            if (!string.IsNullOrEmpty(TestEnvironment.EventHubName))
            {
                return BuildScopeFromExistingEventHub();
            }

            return BuildScopeWithNewEventHub(partitionCount, consumerGroups, caller);
        }

        /// <summary>
        ///   It returns a scope after instantiating a management client
        ///   and querying the consumer groups from the portal.
        /// </summary>
        ///
        /// <returns>The <see cref="EventHubScope" /> that will be used in a given test run.</returns>
        ///
        private static async Task<EventHubScope> BuildScopeFromExistingEventHub()
        {
            var token = await ResourceManager.AquireManagementTokenAsync();

            using (var client = new EventHubManagementClient(new TokenCredentials(token)) { SubscriptionId = TestEnvironment.EventHubsSubscription })
            {
                var consumerGroups = client.ConsumerGroups.ListByEventHub
                (
                    TestEnvironment.EventHubsResourceGroup,
                    TestEnvironment.EventHubsNamespace,
                    TestEnvironment.EventHubName
                 );

                return new EventHubScope(TestEnvironment.EventHubName, consumerGroups.Select(c => c.Name).ToList(), wasEventHubCreated: false);
            }
        }

        /// <summary>
        ///   Performs the tasks needed to create a new Event Hub instance with the requested
        ///   partition count and a dynamically assigned unique name.
        /// </summary>
        ///
        /// <param name="partitionCount">The number of partitions that the Event Hub should be configured with.</param>
        /// <param name="consumerGroups">The set of consumer groups to create and associate with the Event Hub; the default consumer group should not be included, as it is implicitly created.</param>
        /// <param name="caller">The name of the calling method; this is intended to be populated by the runtime.</param>
        ///
        /// <returns>The <see cref="EventHubScope" /> in which the test should be executed.</returns>
        ///
        private static async Task<EventHubScope> BuildScopeWithNewEventHub(int partitionCount,
                                                                           IEnumerable<string> consumerGroups,
                                                                           [CallerMemberName] string caller = "")
        {
            caller = (caller.Length < 16) ? caller : caller.Substring(0, 15);

            var groups = (consumerGroups ?? Enumerable.Empty<string>()).ToList();
            var resourceGroup = TestEnvironment.EventHubsResourceGroup;
            var eventHubNamespace = TestEnvironment.EventHubsNamespace;
            var token = await ResourceManager.AquireManagementTokenAsync();

            string CreateName() => $"{ Guid.NewGuid().ToString("D").Substring(0, 13) }-{ caller }";

            using (var client = new EventHubManagementClient(new TokenCredentials(token)) { SubscriptionId = TestEnvironment.EventHubsSubscription })
            {
                var eventHub = new Eventhub(partitionCount: partitionCount);
                eventHub = await ResourceManager.CreateRetryPolicy<Eventhub>().ExecuteAsync(() => client.EventHubs.CreateOrUpdateAsync(resourceGroup, eventHubNamespace, CreateName(), eventHub));

                var consumerPolicy = ResourceManager.CreateRetryPolicy<ConsumerGroup>();

                await Task.WhenAll
                (
                    consumerGroups.Select(groupName =>
                    {
                        var group = new ConsumerGroup(name: groupName);
                        return consumerPolicy.ExecuteAsync(() => client.ConsumerGroups.CreateOrUpdateAsync(resourceGroup, eventHubNamespace, eventHub.Name, groupName, group));
                    })
                );

                return new EventHubScope(eventHub.Name, groups, wasEventHubCreated: true);
            }
        }

        /// <summary>
        ///   Attempts to parse an Event Hubs namespace from the specified <paramref name="connectionString"/>.
        /// </summary>
        ///
        /// <param name="connectionString">The connection string to be parsed.</param>
        /// <param name="namespaceName">The namespace name taken from the connection string.</param>
        ///
        /// <returns>
        ///   <c>true</c> if the connection string contains a valid namespace; otherwise, <c>false</c>.
        /// </returns>
        ///
        private static bool TryParseNamespace(string connectionString,
                                              out string namespaceName)
        {
            namespaceName = ParseNamespaceName(connectionString);
            return (!string.IsNullOrEmpty(namespaceName));
        }

        /// <summary>
        ///   It returns the namespace name from a give set of connection string properties.
        /// </summary>
        ///
        /// <param name="connectionString">The connection string to be parsed.</param>
        ///
        /// <returns>The namespace name for a given connection string.</returns>
        ///
        private static string ParseNamespaceName(string connectionString)
        {
            string fqdn = ConnectionStringTokenParser.ParseTokenAndReturnValue(connectionString, "Endpoint");

            if (!string.IsNullOrEmpty(fqdn))
            {
                return ConnectionStringTokenParser.ParseNamespaceName(fqdn);
            }

            return null;
        }

        /// <summary>
        ///   The key attributes for identifying and accessing a dynamically created Event Hubs namespace,
        ///   intended to serve as an ephemeral container for the Event Hub instances used during a test run.
        /// </summary>
        ///
        public struct NamespaceProperties
        {
            /// <summary>The name of the Event Hubs namespace that was dynamically created.</summary>
            public readonly string Name;

            /// <summary>The connection string to use for accessing the dynamically created namespace.</summary>
            public readonly string ConnectionString;

            /// <summary>A flag indicating if the namespace was created or referenced from environment variables.</summary>
            public readonly bool WasNamespaceCreated;

            /// <summary>
            ///   Initializes a new instance of the <see cref="NamespaceProperties"/> struct.
            /// </summary>
            ///
            /// <param name="name">The name of the namespace.</param>
            /// <param name="connectionString">The connection string to use for accessing the namespace.</param>
            /// <param name="wasNamespaceCreated">A flag indicating if the namespace was created or referenced from environment variables.</param>
            ///
            internal NamespaceProperties(string name,
                                         string connectionString,
                                         bool wasNamespaceCreated)
            {
                Name = name;
                ConnectionString = connectionString;
                WasNamespaceCreated = wasNamespaceCreated;
            }
        }
    }
}
