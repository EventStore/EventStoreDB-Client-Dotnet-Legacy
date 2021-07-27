using System;
#if !NET5_0_OR_GREATER
using System.Collections.Generic;
#else
using System.Net.Http;
using System.Threading;
#endif
using Grpc.Core;
#if NET5_0_OR_GREATER
using Grpc.Net.Client;
#endif

namespace EventStore.ClientAPI {
	internal static class ChannelFactory {
		public static ChannelBase CreateChannel(ConnectionSettings settings, Uri address) {
#if NET5_0_OR_GREATER
			if (address.Scheme == Uri.UriSchemeHttp || !settings.UseSslConnection) {
				//this must be switched on before creation of the HttpMessageHandler
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}

			return GrpcChannel.ForAddress(address, new GrpcChannelOptions {
				HttpClient = new HttpClient(settings.CustomHttpMessageHandler ?? new SocketsHttpHandler {
					//KeepAlivePingDelay = Math.Max(settings.HeartbeatInterval, TimeSpan.FromSeconds(1)),
					//KeepAlivePingTimeout = Math.Max(settings.HeartbeatTimeout, TimeSpan.FromSeconds(1))
				}, true) {
					Timeout = Timeout.InfiniteTimeSpan,
					DefaultRequestVersion = new Version(2, 0),
				},
				DisposeHttpClient = true
			});
#else
			return new Channel(address.Host, address.Port, ChannelCredentials.Insecure,
				GetChannelOptions());

			IEnumerable<ChannelOption> GetChannelOptions() {
				yield return new ChannelOption("grpc.keepalive_time_ms",
					GetValue((int)settings.HeartbeatInterval.TotalMilliseconds));

				yield return new ChannelOption("grpc.keepalive_timeout_ms",
					GetValue((int)settings.HeartbeatTimeout.TotalMilliseconds));
			}

			static int GetValue(int value) => value switch {
				< 0 => int.MaxValue,
				_ => value
			};
#endif
		}
	}
}
