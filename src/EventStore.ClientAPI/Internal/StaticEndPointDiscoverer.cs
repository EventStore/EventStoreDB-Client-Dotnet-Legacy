using System;
using System.Net;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Internal {
	internal class StaticEndPointDiscoverer : IEndPointDiscoverer {
		private readonly TaskCompletionSource<NodeEndPoints> _source;

		public StaticEndPointDiscoverer(EndPoint endPoint, bool isSsl) : this(endPoint, null, isSsl) {
		}

		public StaticEndPointDiscoverer(EndPoint endPoint, EndPoint httpEndPoint, bool isSsl) {
			if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));
			_source = new TaskCompletionSource<NodeEndPoints>();
			_source.SetResult(new NodeEndPoints(isSsl ? null : endPoint,
				isSsl ? endPoint : null, httpEndPoint));
		}

		public Task<NodeEndPoints> DiscoverAsync(EndPoint failedTcpEndPoint) => _source.Task;
	}
}
