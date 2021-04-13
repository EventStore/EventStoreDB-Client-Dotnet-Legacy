using System;
using System.Net;

namespace EventStore.ClientAPI {
	/// <summary>
	/// Represents a node and its possible endpoints
	/// </summary>
	public readonly struct NodeEndPoints {
		/// <summary>
		/// The tcp endpoint of the node.
		/// </summary>
		public readonly EndPoint TcpEndPoint;

		/// <summary>
		/// The ssl endpoint of the node
		/// </summary>
		public readonly EndPoint SecureTcpEndPoint;

		/// <summary>
		/// The http endpoint of the node
		/// </summary>
		public readonly EndPoint HttpEndPoint;


		/// <summary>
		/// Called to create a new NodeEndPoints
		/// </summary>
		/// <param name="tcpEndPoint">The tcp endpoint of the node</param>
		/// <param name="secureTcpEndPoint">The ssl endpoint of the node</param>
		/// <param name="httpEndPoint">The http endpoint of the node</param>
		public NodeEndPoints(EndPoint tcpEndPoint, EndPoint secureTcpEndPoint, EndPoint httpEndPoint = null) {
			if ((tcpEndPoint ?? secureTcpEndPoint) == null) throw new ArgumentException("Both endpoints are null.");
			TcpEndPoint = tcpEndPoint;
			SecureTcpEndPoint = secureTcpEndPoint;
			HttpEndPoint = httpEndPoint;
		}

		/// <summary>
		/// Formats the endpoints as a string
		/// </summary>
		public override string ToString() =>
			$"[{(TcpEndPoint == null ? "n/a" : TcpEndPoint.ToString())}, {(SecureTcpEndPoint == null ? "n/a" : SecureTcpEndPoint.ToString())}, {HttpEndPoint}]";
	}
}
