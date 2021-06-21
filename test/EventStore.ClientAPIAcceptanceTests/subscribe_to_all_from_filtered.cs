using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Core.Services;
using Xunit;

namespace EventStore.ClientAPI {
	public class subscribe_to_all_from_filtered : EventStoreClientAPITest, IAsyncLifetime {
		private readonly EventStoreClientAPIFixture _fixture;

		public subscribe_to_all_from_filtered(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Theory, ClassData(typeof(StreamIdFilterCases))]
		public async Task stream_id_concurrently(Func<string, Filter> getFilter, string name) {
			var streamName = $"{GetStreamName()}_{name}";
			var eventAppearedSource1 = new TaskCompletionSource<ResolvedEvent>();
			var eventAppearedSource2 = new TaskCompletionSource<ResolvedEvent>();
			var connection = _fixture.Connection;
			var filter = getFilter(streamName);

			connection.FilteredSubscribeToAllFrom(default, filter, CatchUpSubscriptionFilteredSettings.Default,
				EventAppeared1, subscriptionDropped: SubscriptionDropped1);

			connection.FilteredSubscribeToAllFrom(default, filter, CatchUpSubscriptionFilteredSettings.Default,
				EventAppeared2, subscriptionDropped: SubscriptionDropped2);

			var testEvents = _fixture.CreateTestEvents().ToArray();
			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, testEvents).WithTimeout();

			var resolvedEvents =
				await Task.WhenAll(eventAppearedSource1.Task, eventAppearedSource2.Task).WithTimeout();

			Assert.Equal(testEvents[0].EventId, resolvedEvents[0].OriginalEvent.EventId);
			Assert.Equal(testEvents[0].EventId, resolvedEvents[1].OriginalEvent.EventId);

			Task EventAppeared1(EventStoreCatchUpSubscription s, ResolvedEvent e) {
				eventAppearedSource1.TrySetResult(e);

				return Task.CompletedTask;
			}

			Task EventAppeared2(EventStoreCatchUpSubscription s, ResolvedEvent e) {
				eventAppearedSource2.TrySetResult(e);

				return Task.CompletedTask;
			}

			void SubscriptionDropped1(EventStoreCatchUpSubscription s, SubscriptionDropReason reason, Exception ex) =>
				eventAppearedSource1.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));

			void SubscriptionDropped2(EventStoreCatchUpSubscription s, SubscriptionDropReason reason, Exception ex) =>
				eventAppearedSource2.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));
		}

		[Theory, ClassData(typeof(StreamIdFilterCases))]
		public async Task from_checkpoint(Func<string, Filter> getFilter, string name) {
			var streamName = $"{GetStreamName()}_{name}";
			var caughtup = new TaskCompletionSource<ResolvedEvent>();
			var receivedLive = new TaskCompletionSource<ResolvedEvent>();
			var connection = _fixture.Connection;
			var filter = getFilter(streamName);

			var catchupEvents = _fixture.CreateTestEvents(5).ToArray();
			var liveEvents = _fixture.CreateTestEvents(1).ToArray();

			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, catchupEvents).WithTimeout();

			var lastCheckpoint = (await connection.ReadAllEventsBackwardAsync(Position.End, 2, false)).Events.Last().OriginalPosition;
			connection.FilteredSubscribeToAllFrom(lastCheckpoint, filter, CatchUpSubscriptionFilteredSettings.Default,
				EventAppeared, subscriptionDropped: SubscriptionDropped);

			var receivedCatchupEvent = await caughtup.Task.WithTimeout();

			await connection.AppendToStreamAsync(streamName, catchupEvents.Length - 1, liveEvents).WithTimeout();

			var receivedLiveEvent = await receivedLive.Task.WithTimeout();

			Assert.Equal(catchupEvents.Last().EventId, receivedCatchupEvent.OriginalEvent.EventId);
			Assert.Equal(liveEvents.Last().EventId, receivedLiveEvent.OriginalEvent.EventId);

			Task EventAppeared(EventStoreCatchUpSubscription s, ResolvedEvent e) {
				if (e.OriginalStreamId == streamName) {
					if (!caughtup.TrySetResult(e)) {
						receivedLive.TrySetResult(e);
					}
				}

				return Task.CompletedTask;
			}

			void SubscriptionDropped(EventStoreCatchUpSubscription s, SubscriptionDropReason reason, Exception ex) {
				caughtup.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));
				receivedLive.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));
			}
		}







		public async Task InitializeAsync() {
			var connection = _fixture.Connection;

			await connection
				.SetStreamMetadataAsync(
					"$all",
					ExpectedVersion.Any,
					StreamMetadata.Build().SetReadRole(SystemRoles.All),
					DefaultUserCredentials.Admin)
				.WithTimeout();
		}

		public async Task DisposeAsync() {
			var connection = _fixture.Connection;

			await connection
				.SetStreamMetadataAsync(
					"$all",
					ExpectedVersion.Any,
					StreamMetadata.Build().SetReadRole(null),
					DefaultUserCredentials.Admin)
				.WithTimeout();
		}
	}
}
