using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI {
	/// <summary>
	/// Used to build a connection settings (fluent API)
	/// </summary>
	public class ConnectionSettingsBuilder {
		private ILogger _log = new NoopLogger();
		private bool _verboseLogging;

		private int _maxQueueSize = Consts.DefaultMaxQueueSize;
		private int _maxConcurrentItems = Consts.DefaultMaxConcurrentItems;
		private int _maxRetries = Consts.DefaultMaxOperationRetries;
		private int _maxReconnections = Consts.DefaultMaxReconnections;

		private bool _requireLeader = Consts.DefaultRequireLeader;

		private TimeSpan _reconnectionDelay = Consts.DefaultReconnectionDelay;
		private TimeSpan _queueTimeout = Consts.DefaultQueueTimeout;
		private TimeSpan _operationTimeout = Consts.DefaultOperationTimeout;
		private TimeSpan _operationTimeoutCheckPeriod = Consts.DefaultOperationTimeoutCheckPeriod;

		private UserCredentials _defaultUserCredentials;
		private bool _useSslConnection = true;
		private bool _validateServer = true;

		private bool _failOnNoServerResponse;
		private TimeSpan _heartbeatInterval = TimeSpan.FromMilliseconds(750);
		private TimeSpan _heartbeatTimeout = TimeSpan.FromMilliseconds(1500);
		private TimeSpan _clientConnectionTimeout = TimeSpan.FromMilliseconds(1000);
		private string _clusterDns;
		private int _maxDiscoverAttempts = Consts.DefaultMaxClusterDiscoverAttempts;
		private int _httpPort = Consts.DefaultHttpPort;
		private TimeSpan _gossipTimeout = TimeSpan.FromSeconds(1);
		private GossipSeed[] _gossipSeeds;
		private NodePreference _nodePreference = NodePreference.Leader;
		private string _compatibilityMode = "";
		private bool _retryAuthenticationOnTimeout;
		private HttpMessageHandler _customHttpMessageHandler;

		internal ConnectionSettingsBuilder() {
		}

		/// <summary>
		/// Configures a custom <see cref="HttpMessageHandler"/> to use when issuing Http requests
		/// </summary>
		/// <param name="httpMessageHandler">The <see cref="HttpMessageHandler"/> to use.</param>
		/// <returns></returns>
		public ConnectionSettingsBuilder UseCustomHttpMessageHandler(HttpMessageHandler httpMessageHandler) {
			_customHttpMessageHandler = httpMessageHandler;
			return this;
		}

		/// <summary>
		/// Configures the connection to output log messages to the given <see cref="ILogger" />. You should implement this interface using another library such as Serilog.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <returns></returns>
		public ConnectionSettingsBuilder UseCustomLogger(ILogger logger) {
			Ensure.NotNull(logger, "logger");
			_log = logger;
			return this;
		}

		/// <summary>
		/// Configures the connection to output log messages to the console.
		/// </summary>
		public ConnectionSettingsBuilder UseConsoleLogger() {
			_log = new ConsoleLogger();
			return this;
		}

		/// <summary>
		/// Configures the connection to output log messages to the listeners
		/// configured on <see cref="System.Diagnostics.Debug" />.
		/// </summary>
		public ConnectionSettingsBuilder UseDebugLogger() {
			_log = new DebugLogger();
			return this;
		}


		/// <summary>
		/// Configures the connection to output log messages to a file.
		/// </summary>
		public ConnectionSettingsBuilder UseFileLogger(string filename) {
			_log = new FileLogger(filename);
			return this;
		}

		/// <summary>
		/// Turns on verbose <see cref="EventStoreConnection"/> internal logic logging. By contains default information about connection, disconnection and errors, but you can customize output.
		/// </summary>
		/// <returns></returns>
		public ConnectionSettingsBuilder EnableVerboseLogging() {
			_verboseLogging = true;
			return this;
		}

		/// <summary>
		/// Sets the limit for number of outstanding operations.
		/// </summary>
		/// <param name="limit">The new limit of outstanding operations</param>
		/// <returns></returns>
		public ConnectionSettingsBuilder LimitOperationsQueueTo(int limit) {
			Ensure.Positive(limit, "limit");

			_maxQueueSize = limit;
			return this;
		}

		/// <summary>
		/// Limits the number of concurrent operations that this connection can have.
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder LimitConcurrentOperationsTo(int limit) {
			Ensure.Positive(limit, "limit");

			_maxConcurrentItems = limit;
			return this;
		}

		/// <summary>
		/// Limits the number of operation attempts.
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder LimitAttemptsForOperationTo(int limit) {
			Ensure.Positive(limit, "limit");

			_maxRetries = limit - 1;
			return this;
		}

		/// <summary>
		/// Limits the number of operation retries.
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder LimitRetriesForOperationTo(int limit) {
			Ensure.Nonnegative(limit, "limit");

			_maxRetries = limit;
			return this;
		}

		/// <summary>
		/// Allows infinite operation retry attempts.
		/// </summary>
		/// <returns></returns>
		public ConnectionSettingsBuilder KeepRetrying() {
			_maxRetries = -1;
			return this;
		}

		/// <summary>
		/// Limits the number of reconnections this connection can try to make.
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder LimitReconnectionsTo(int limit) {
			Ensure.Nonnegative(limit, "limit");

			_maxReconnections = limit;
			return this;
		}

		/// <summary>
		/// Allows infinite reconnection attempts.
		/// </summary>
		/// <returns></returns>
		public ConnectionSettingsBuilder KeepReconnecting() {
			_maxReconnections = -1;
			return this;
		}

		/// <summary>
		/// Requires all write and read requests to be served only by leader, when running in a cluster.
		/// </summary>
		/// <returns></returns>
		public ConnectionSettingsBuilder PerformOnLeaderOnly() {
			_requireLeader = true;
			return this;
		}

		/// <summary>
		/// Allow for writes to be forwarded and read requests served locally if node is not leader, when running in a cluster.
		/// </summary>
		/// <returns></returns>
		public ConnectionSettingsBuilder PerformOnAnyNode() {
			_requireLeader = false;
			return this;
		}

		/// <summary>
		/// Sets the delay between reconnection attempts.
		/// </summary>
		/// <param name="reconnectionDelay"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder SetReconnectionDelayTo(TimeSpan reconnectionDelay) {
			_reconnectionDelay = reconnectionDelay;
			return this;
		}

		/// <summary>
		/// Sets the maximum permitted time a request may be queued awaiting transmission; if exceeded an <see cref="Exceptions.OperationExpiredException"/> is thrown.
		/// </summary>
		/// <param name="queueTimeout"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder SetQueueTimeoutTo(TimeSpan queueTimeout) {
			_queueTimeout = queueTimeout;
			return this;
		}

		/// <summary>
		/// Sets the operation timeout duration.
		/// </summary>
		/// <param name="operationTimeout"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder SetOperationTimeoutTo(TimeSpan operationTimeout) {
			_operationTimeout = operationTimeout;
			return this;
		}

		/// <summary>
		/// Sets how often timeouts should be checked for.
		/// </summary>
		/// <param name="timeoutCheckPeriod"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder SetTimeoutCheckPeriodTo(TimeSpan timeoutCheckPeriod) {
			_operationTimeoutCheckPeriod = timeoutCheckPeriod;
			return this;
		}

		/// <summary>
		/// Sets the default <see cref="UserCredentials"/> used for this connection.
		/// If user credentials are not given for an operation, these credentials will be used.
		/// </summary>
		/// <param name="userCredentials"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder SetDefaultUserCredentials(UserCredentials userCredentials) {
			_defaultUserCredentials = userCredentials;
			return this;
		}

		/// <summary>
		/// Disables TLS
		/// </summary>
		/// <returns></returns>
		public ConnectionSettingsBuilder DisableTls() {
			_useSslConnection = false;
			return this;
		}

		/// <summary>
		/// Disables Server Certificate Validation
		/// </summary>
		/// <returns></returns>
		public ConnectionSettingsBuilder DisableServerCertificateValidation() {
			_validateServer = false;
			return this;
		}

		/// <summary>
		/// Marks that no response from server should cause an error on the request.
		/// </summary>
		/// <returns></returns>
		public ConnectionSettingsBuilder FailOnNoServerResponse() {
			_failOnNoServerResponse = true;
			return this;
		}

		/// <summary>
		/// Sets how often heartbeats should be expected on the connection (lower values detect broken sockets faster).
		/// </summary>
		/// <param name="interval"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder SetHeartbeatInterval(TimeSpan interval) {
			_heartbeatInterval = interval;
			return this;
		}

		/// <summary>
		/// Sets how long to wait without heartbeats before determining a connection to be dead (must be longer than heartbeat interval).
		/// </summary>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder SetHeartbeatTimeout(TimeSpan timeout) {
			_heartbeatTimeout = timeout;
			return this;
		}

		/// <summary>
		/// Sets the timeout for attempting to connect to a server before aborting and attempting a reconnect.
		/// </summary>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public ConnectionSettingsBuilder WithConnectionTimeoutOf(TimeSpan timeout) {
			_clientConnectionTimeout = timeout;
			return this;
		}

		/// <summary>
		/// Sets the DNS name under which cluster nodes are listed.
		/// </summary>
		/// <param name="clusterDns">The DNS name under which cluster nodes are listed.</param>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="clusterDns" /> is null or empty.</exception>
		public ConnectionSettingsBuilder SetClusterDns(string clusterDns) {
			Ensure.NotNullOrEmpty(clusterDns, "clusterDns");
			_clusterDns = clusterDns;
			return this;
		}

		/// <summary>
		/// Sets the maximum number of attempts for discovery.
		/// </summary>
		/// <param name="maxDiscoverAttempts">The maximum number of attempts for DNS discovery.</param>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="maxDiscoverAttempts" /> is less than or equal to 0.</exception>
		public ConnectionSettingsBuilder SetMaxDiscoverAttempts(int maxDiscoverAttempts) {
			if (maxDiscoverAttempts <= 0)
				throw new ArgumentOutOfRangeException("maxDiscoverAttempts",
					string.Format("maxDiscoverAttempts value is out of range: {0}. Allowed range: [1, infinity].",
						maxDiscoverAttempts));
			_maxDiscoverAttempts = maxDiscoverAttempts;
			return this;
		}

		/// <summary>
		/// Sets the period after which gossip times out if none is received.
		/// </summary>
		/// <param name="timeout">The period after which gossip times out if none is received.</param>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		public ConnectionSettingsBuilder SetGossipTimeout(TimeSpan timeout) {
			_gossipTimeout = timeout;
			return this;
		}

		/// <summary>
		/// Whether to randomly choose a node that's alive from the known nodes.
		/// </summary>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		public ConnectionSettingsBuilder PreferRandomNode() {
			_nodePreference = NodePreference.Random;
			return this;
		}

		/// <summary>
		/// Whether to prioritize choosing a follower node that's alive from the known nodes.
		/// </summary>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		public ConnectionSettingsBuilder PreferFollowerNode() {
			_nodePreference = NodePreference.Follower;
			_requireLeader = false;
			return this;
		}

		/// <summary>
		/// Whether to prioritize choosing a read only replica that's alive from the known nodes.
		/// </summary>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		public ConnectionSettingsBuilder PreferReadOnlyReplica() {
			_nodePreference = NodePreference.ReadOnlyReplica;
			_requireLeader = false;
			return this;
		}

		/// <summary>
		/// Sets the well-known port on which the cluster gossip is taking place.
		///
		/// This should be the HTTP port that the nodes are running on. If you cannot use a well-known
		/// port for this across all nodes, you can instead use gossip seed discovery and set
		/// the <see cref="EndPoint" /> of some seed nodes instead.
		/// </summary>
		/// <param name="clusterGossipPort">The cluster gossip port.</param>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		public ConnectionSettingsBuilder SetClusterGossipPort(int clusterGossipPort) {
			Ensure.Positive(clusterGossipPort, "clusterGossipPort");
			_httpPort = clusterGossipPort;
			return this;
		}

		/// <summary>
		/// Sets gossip seed endpoints for the client.
		///
		/// <note>
		/// This should be the HTTP endpoint of the server, as it is required
		/// for the client to exchange gossip with the server. The standard port is 2113.
		/// </note>
		///
		/// If the server requires a specific Host header to be sent as part of the gossip
		/// request, use the overload of this method taking <see cref="GossipSeed" /> instead.
		/// </summary>
		/// <param name="gossipSeeds"><see cref="EndPoint" />s representing the endpoints of nodes from which to seed gossip.</param>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		/// <exception cref="ArgumentException">If no gossip seeds are specified.</exception>
		public ConnectionSettingsBuilder SetGossipSeedEndPoints(params EndPoint[] gossipSeeds) {
			return SetGossipSeedEndPoints(true, gossipSeeds);
		}

		/// <summary>
		/// Sets gossip seed endpoints for the client.
		///
		/// <note>
		/// This should be the HTTP endpoint of the server, as it is required
		/// for the client to exchange gossip with the server. The standard port is 2113.
		/// </note>
		///
		/// If the server requires a specific Host header to be sent as part of the gossip
		/// request, use the overload of this method taking <see cref="GossipSeed" /> instead.
		/// </summary>
		/// <param name="seedOverTls">Specifies that eventstore should use https when connecting to gossip</param>
		/// <param name="gossipSeeds"><see cref="EndPoint" />s representing the endpoints of nodes from which to seed gossip.</param>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		/// <exception cref="ArgumentException">If no gossip seeds are specified.</exception>
		public ConnectionSettingsBuilder SetGossipSeedEndPoints(bool seedOverTls, params EndPoint[] gossipSeeds) {
			if (gossipSeeds == null || gossipSeeds.Length == 0)
				throw new ArgumentException("Empty FakeDnsEntries collection.");

			_gossipSeeds = gossipSeeds.Select(x => new GossipSeed(x, seedOverTls: seedOverTls)).ToArray();

			return this;
		}

		/// <summary>
		/// Sets gossip seed endpoints for the client.
		/// </summary>
		/// <param name="gossipSeeds"><see cref="GossipSeed"/>s representing the endpoints of nodes from which to seed gossip.</param>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		/// <exception cref="ArgumentException">If no gossip seeds are specified.</exception>
		public ConnectionSettingsBuilder SetGossipSeedEndPoints(params GossipSeed[] gossipSeeds) {
			if (gossipSeeds == null || gossipSeeds.Length == 0)
				throw new ArgumentException("Empty FakeDnsEntries collection.");
			_gossipSeeds = gossipSeeds;
			return this;
		}

		/// <summary>
		/// Specifies if the client should run in a specific version compatibility mode.
		/// </summary>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		public ConnectionSettingsBuilder SetCompatibilityMode(string value) {
			_compatibilityMode = value;
			return this;
		}

		/// <summary>
		/// If enabled, the client will not skip authentication if the process times out. The maximun retries count is the same as
		/// ConnectionSettings.MaxRetries.
		/// </summary>
		/// <returns>A <see cref="ConnectionSettingsBuilder"/> for further configuration.</returns>
		public ConnectionSettingsBuilder RetryAuthenticationOnTimeout() {
			_retryAuthenticationOnTimeout = true;
			return this;
		}

		/// <summary>
		/// Convert the mutable <see cref="ConnectionSettingsBuilder"/> object to an immutable
		/// <see cref="ConnectionSettings"/> object.
		/// </summary>
		/// <param name="builder">The <see cref="ConnectionSettingsBuilder"/> to convert.</param>
		/// <returns>An immutable <see cref="ConnectionSettings"/> object with the values specified by the builder.</returns>
		public static implicit operator ConnectionSettings(ConnectionSettingsBuilder builder) {
			return builder.Build();
		}

		/// <summary>
		/// Convert the mutable <see cref="ConnectionSettingsBuilder"/> object to an immutable
		/// <see cref="ConnectionSettings"/> object.
		/// </summary>
		public ConnectionSettings Build() {
			return new ConnectionSettings(_log,
				_verboseLogging,
				_maxQueueSize,
				_maxConcurrentItems,
				_maxRetries,
				_maxReconnections,
				_requireLeader,
				_reconnectionDelay,
				_queueTimeout,
				_operationTimeout,
				_operationTimeoutCheckPeriod,
				_defaultUserCredentials,
				_useSslConnection,
				_validateServer,
				_failOnNoServerResponse,
				_heartbeatInterval,
				_heartbeatTimeout,
				_clientConnectionTimeout,
				_clusterDns,
				_gossipSeeds,
				_maxDiscoverAttempts,
				_httpPort,
				_gossipTimeout,
				_nodePreference,
				_compatibilityMode,
				_retryAuthenticationOnTimeout,
				_customHttpMessageHandler);
		}
	}
}
