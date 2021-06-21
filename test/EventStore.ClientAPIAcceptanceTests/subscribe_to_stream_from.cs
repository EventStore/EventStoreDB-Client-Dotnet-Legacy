using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.ClientAPI {
	public class subscribe_to_stream_from : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public subscribe_to_stream_from(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task from_non_existing_stream() {
			var streamName = GetStreamName();
			var eventAppearedSource = new TaskCompletionSource<ResolvedEvent>();
			var connection = _fixture.Connection;

			connection.SubscribeToStreamFrom(streamName, default, CatchUpSubscriptionSettings.Default,
				EventAppeared, subscriptionDropped: SubscriptionDropped);

			var testEvents = _fixture.CreateTestEvents().ToArray();
			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, testEvents).WithTimeout();

			var resolvedEvent = await eventAppearedSource.Task.WithTimeout();

			Assert.Equal(testEvents[0].EventId, resolvedEvent.OriginalEvent.EventId);

			Task EventAppeared(EventStoreCatchUpSubscription s, ResolvedEvent e) {
				eventAppearedSource.TrySetResult(e);
				return Task.CompletedTask;
			}

			void SubscriptionDropped(EventStoreCatchUpSubscription s, SubscriptionDropReason reason, Exception ex) =>
				eventAppearedSource.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));
		}


		[Fact]
		public async Task concurrently() {
			var streamName = GetStreamName();
			var eventAppearedSource1 = new TaskCompletionSource<ResolvedEvent>();
			var eventAppearedSource2 = new TaskCompletionSource<ResolvedEvent>();
			var connection = _fixture.Connection;

			connection.SubscribeToStreamFrom(streamName, default, CatchUpSubscriptionSettings.Default,
				EventAppeared1, subscriptionDropped: SubscriptionDropped1);

			connection.SubscribeToStreamFrom(streamName, default, CatchUpSubscriptionSettings.Default,
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

		[Fact]
		public async Task from_checkpoint() {
			var streamName = GetStreamName();
			var caughtup = new TaskCompletionSource<ResolvedEvent>();
			var receivedLive = new TaskCompletionSource<ResolvedEvent>();
			var connection = _fixture.Connection;

			var catchupEvents = _fixture.CreateTestEvents(5).ToArray();
			var liveEvents = _fixture.CreateTestEvents(1).ToArray();

			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, catchupEvents).WithTimeout();

			var lastCheckpoint = 3;
			connection.SubscribeToStreamFrom(streamName, lastCheckpoint, CatchUpSubscriptionSettings.Default,
				EventAppeared, subscriptionDropped: SubscriptionDropped);

			var receivedCatchupEvent = await caughtup.Task.WithTimeout();

			await connection.AppendToStreamAsync(streamName, catchupEvents.Length - 1, liveEvents).WithTimeout();

			var receivedLiveEvent = await receivedLive.Task.WithTimeout();

			Assert.Equal(catchupEvents.Last().EventId, receivedCatchupEvent.OriginalEvent.EventId);
			Assert.Equal(liveEvents.Last().EventId, receivedLiveEvent.OriginalEvent.EventId);

			Task EventAppeared(EventStoreCatchUpSubscription s, ResolvedEvent e) {
				if (!caughtup.TrySetResult(e))
					receivedLive.TrySetResult(e);
				return Task.CompletedTask;
			}

			void SubscriptionDropped(EventStoreCatchUpSubscription s, SubscriptionDropReason reason, Exception ex) {
				caughtup.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));
				receivedLive.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));
			}
		}

		[Fact]
		public async Task drops_on_subscriber_error() {
			var streamName = GetStreamName();
			var droppedSource = new TaskCompletionSource<(SubscriptionDropReason, Exception)>();
			var expectedException = new Exception("subscriber error");
			var connection = _fixture.Connection;

			connection.SubscribeToStreamFrom(streamName, default, CatchUpSubscriptionSettings.Default,
				EventAppeared, subscriptionDropped: SubscriptionDropped);

			var testEvents = _fixture.CreateTestEvents().ToArray();
			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, testEvents).WithTimeout();

			var (dropped, exception) = await droppedSource.Task.WithTimeout();

			Assert.Equal(SubscriptionDropReason.EventHandlerException, dropped);
			Assert.IsType(expectedException.GetType(), exception);
			Assert.Equal(expectedException.Message, exception.Message);

			Task EventAppeared(EventStoreCatchUpSubscription s, ResolvedEvent e)
				=> Task.FromException(expectedException);

			void SubscriptionDropped(EventStoreCatchUpSubscription s, SubscriptionDropReason reason, Exception ex) =>
				droppedSource.TrySetResult((reason, ex));
		}

		[Fact]
		public async Task drops_on_unsubscribed() {
			var streamName = GetStreamName();
			var droppedSource = new TaskCompletionSource<(SubscriptionDropReason, Exception)>();
			var connection = _fixture.Connection;

			var subscription = connection.SubscribeToStreamFrom(streamName, 0L, CatchUpSubscriptionSettings.Default,
				EventAppeared, subscriptionDropped: SubscriptionDropped);

			subscription.Stop(TimeSpan.FromSeconds(3));

			var (dropped, exception) = await droppedSource.Task.WithTimeout();

			Assert.Equal(SubscriptionDropReason.UserInitiated, dropped);
			Assert.Null(exception);

			Task EventAppeared(EventStoreCatchUpSubscription s, ResolvedEvent e)
				=> Task.CompletedTask;

			void SubscriptionDropped(EventStoreCatchUpSubscription s, SubscriptionDropReason reason, Exception ex) =>
				droppedSource.TrySetResult((reason, ex));
		}
	}
}
