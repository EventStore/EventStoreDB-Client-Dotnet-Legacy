using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.ClientAPI {
	public class subscribe_to_stream : EventStoreClientAPITest {
		private readonly EventStoreClientAPIFixture _fixture;

		public subscribe_to_stream(EventStoreClientAPIFixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task from_non_existing_stream() {
			var streamName = GetStreamName();
			var eventAppearedSource = new TaskCompletionSource<ResolvedEvent>();
			var connection = _fixture.Connection;

			using var _ = await connection
				.SubscribeToStreamAsync(streamName, false, EventAppeared, SubscriptionDropped).WithTimeout();

			var testEvents = _fixture.CreateTestEvents().ToArray();
			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, testEvents).WithTimeout();

			var resolvedEvent = await eventAppearedSource.Task.WithTimeout();

			Assert.Equal(testEvents[0].EventId, resolvedEvent.OriginalEvent.EventId);

			Task EventAppeared(EventStoreSubscription s, ResolvedEvent e) {
				eventAppearedSource.TrySetResult(e);
				return Task.CompletedTask;
			}

			void SubscriptionDropped(EventStoreSubscription s, SubscriptionDropReason reason, Exception ex) =>
				eventAppearedSource.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));
		}

		[Fact]
		public async Task concurrently() {
			var streamName = GetStreamName();
			var eventAppearedSource1 = new TaskCompletionSource<ResolvedEvent>();
			var eventAppearedSource2 = new TaskCompletionSource<ResolvedEvent>();
			var connection = _fixture.Connection;

			using (await connection.SubscribeToStreamAsync(streamName, false, EventAppeared1, SubscriptionDropped1)
				.WithTimeout())
			using (await connection.SubscribeToStreamAsync(streamName, false, EventAppeared2, SubscriptionDropped2)
				.WithTimeout()) {
				var testEvents = _fixture.CreateTestEvents().ToArray();
				await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, testEvents).WithTimeout();

				var resolvedEvents =
					await Task.WhenAll(eventAppearedSource1.Task, eventAppearedSource2.Task).WithTimeout();

				Assert.Equal(testEvents[0].EventId, resolvedEvents[0].OriginalEvent.EventId);
				Assert.Equal(testEvents[0].EventId, resolvedEvents[1].OriginalEvent.EventId);
			}


			Task EventAppeared1(EventStoreSubscription s, ResolvedEvent e) {
				eventAppearedSource1.TrySetResult(e);
				return Task.CompletedTask;
			}

			Task EventAppeared2(EventStoreSubscription s, ResolvedEvent e) {
				eventAppearedSource2.TrySetResult(e);
				return Task.CompletedTask;
			}

			void SubscriptionDropped1(EventStoreSubscription s, SubscriptionDropReason reason, Exception ex) =>
				eventAppearedSource1.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));

			void SubscriptionDropped2(EventStoreSubscription s, SubscriptionDropReason reason, Exception ex) =>
				eventAppearedSource2.TrySetException(ex ?? new ObjectDisposedException(nameof(s)));
		}

		[Fact(Skip = nameof(drops_on_subscriber_error) + " is bugged")]
		public async Task drops_on_subscriber_error() {
			var streamName = GetStreamName();
			var droppedSource = new TaskCompletionSource<(SubscriptionDropReason, Exception)>();
			var expectedException = new Exception("subscriber error");
			var connection = _fixture.Connection;

			using var _ = await connection
				.SubscribeToStreamAsync(streamName, false, EventAppeared, SubscriptionDropped).WithTimeout();

			var testEvents = _fixture.CreateTestEvents().ToArray();
			await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, testEvents).WithTimeout();

			var (dropped, exception) = await droppedSource.Task.WithTimeout();

			Assert.Equal(SubscriptionDropReason.SubscribingError, dropped);
			Assert.IsType(expectedException.GetType(), exception);
			Assert.Equal(expectedException.Message, exception.Message);

			Task EventAppeared(EventStoreSubscription s, ResolvedEvent e)
				=> Task.FromException(expectedException);

			void SubscriptionDropped(EventStoreSubscription s, SubscriptionDropReason reason, Exception ex) =>
				droppedSource.TrySetResult((reason, ex));
		}

		[Fact]
		public async Task drops_on_unsubscribed() {
			var streamName = GetStreamName();
			var droppedSource = new TaskCompletionSource<(SubscriptionDropReason, Exception)>();
			var connection = _fixture.Connection;

			using var subscription = await connection
				.SubscribeToStreamAsync(streamName, false, EventAppeared, SubscriptionDropped).WithTimeout();

			subscription.Unsubscribe();

			var (dropped, exception) = await droppedSource.Task.WithTimeout();

			Assert.Equal(SubscriptionDropReason.UserInitiated, dropped);
			Assert.Null(exception);

			Task EventAppeared(EventStoreSubscription s, ResolvedEvent e)
				=> Task.CompletedTask;

			void SubscriptionDropped(EventStoreSubscription s, SubscriptionDropReason reason, Exception ex) =>
				droppedSource.TrySetResult((reason, ex));
		}
	}
}
