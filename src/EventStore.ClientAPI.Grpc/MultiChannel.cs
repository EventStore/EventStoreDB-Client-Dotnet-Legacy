using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Internal;
using Grpc.Core;

namespace EventStore.ClientAPI {
	internal class MultiChannel : IDisposable, IAsyncDisposable {
		private readonly ConnectionSettings _settings;
		private readonly IEndPointDiscoverer _endpointDiscoverer;
		private readonly ConcurrentDictionary<EndPoint, ChannelBase> _channels;
		private readonly ILogger _log;

		private EndPoint _current;
		private int _disposed;

		public MultiChannel(ConnectionSettings settings, IEndPointDiscoverer endPointDiscoverer) {
			_settings = settings;
			_endpointDiscoverer = endPointDiscoverer;
			_channels = new ConcurrentDictionary<EndPoint, ChannelBase>();
			_log = settings.Log;
		}

		public void SetEndPoint(EndPoint value) => _current = value;

		public async Task<ChannelBase> GetCurrentChannel() {
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0) {
				throw new ObjectDisposedException(GetType().ToString());
			}

			var current = _current ??= (await _endpointDiscoverer.DiscoverAsync(null).ConfigureAwait(false))
				.HttpEndPoint;
			return _channels.GetOrAdd(current, ChannelFactory.CreateChannel(_settings, new UriBuilder {
				Host = current.GetHost(),
				Port = current.GetPort(),
				Scheme = _settings.UseSslConnection ? Uri.UriSchemeHttps : Uri.UriSchemeHttp
			}.Uri));
		}

		public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

		public async ValueTask DisposeAsync() {
			if (Interlocked.Exchange(ref _disposed, 1) == 1) {
				return;
			}

			foreach (var channel in _channels.Values) {
				if (channel is IDisposable disposable) {
					disposable.Dispose();
				} else {
					await channel.ShutdownAsync().ConfigureAwait(false);
				}
			}
		}
	}
}
