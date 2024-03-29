﻿using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;

namespace EventStore.ClientAPI.Internal {
	internal class StaticEndPointDiscoverer : IEndPointDiscoverer {
		private readonly Task<NodeEndPoints> _task;

		public StaticEndPointDiscoverer(EndPoint endPoint, bool isSsl) {
			Ensure.NotNull(endPoint, "endPoint");
			_task = Task.FromResult(new NodeEndPoints(isSsl ? null : endPoint, isSsl ? endPoint : null));
		}

		public Task<NodeEndPoints> DiscoverAsync(EndPoint failedTcpEndPoint) => _task;
	}
}
