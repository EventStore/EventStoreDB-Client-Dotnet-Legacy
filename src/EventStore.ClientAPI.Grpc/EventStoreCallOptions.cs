using System;
using System.Threading;
using EventStore.ClientAPI.SystemData;
using Grpc.Core;

#nullable enable
namespace EventStore.ClientAPI {
	internal static class EventStoreCallOptions {
		public static CallOptions Create(ConnectionSettings settings, UserCredentials? userCredentials,
			CancellationToken cancellationToken = default) => new(
			cancellationToken: cancellationToken,
			deadline: DeadlineAfter(settings.OperationTimeout),
			headers: new Metadata(),
			credentials: (settings.DefaultUserCredentials ?? userCredentials) == null
				? null
				: CallCredentials.FromInterceptor(async (context, metadata) => {
					var credentials = settings.DefaultUserCredentials ?? userCredentials;

					// var authorizationHeader = await settings.OperationOptions
					// 	.GetAuthenticationHeaderValue(credentials!, CancellationToken.None)
					// 	.ConfigureAwait(false);
					// metadata.Add(Constants.Headers.Authorization, authorizationHeader);
				})
		);

		private static DateTime? DeadlineAfter(TimeSpan? timeoutAfter) => !timeoutAfter.HasValue
			? new DateTime?()
			: timeoutAfter.Value == TimeSpan.MaxValue || timeoutAfter.Value == Timeout.InfiniteTimeSpan
				? DateTime.MaxValue
				: DateTime.UtcNow.Add(timeoutAfter.Value);
	}
}
