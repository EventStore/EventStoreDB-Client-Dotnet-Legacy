using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.Client.Streams;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Interceptors;
using EventStore.ClientAPI.SystemData;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Empty = EventStore.Client.Empty;

namespace EventStore.ClientAPI {
	internal class EventStoreGrpcNodeConnection : IEventStoreConnection {
		private readonly ClusterSettings _clusterSettings;
		private readonly MultiChannel _channel;

		public string ConnectionName { get; }
		public ConnectionSettings Settings { get; }

		public event EventHandler<ClientConnectionEventArgs> Connected;
		public event EventHandler<ClientConnectionEventArgs> Disconnected;
		public event EventHandler<ClientReconnectingEventArgs> Reconnecting;
		public event EventHandler<ClientClosedEventArgs> Closed;
		public event EventHandler<ClientErrorEventArgs> ErrorOccurred;
		public event EventHandler<ClientAuthenticationFailedEventArgs> AuthenticationFailed;

		public EventStoreGrpcNodeConnection(ConnectionSettings settings, ClusterSettings clusterSettings,
			IEndPointDiscoverer endPointDiscoverer, string connectionName) {
			_clusterSettings = clusterSettings;
			Settings = settings;
			ConnectionName = connectionName ?? $"ES-{Guid.NewGuid()}";
			_channel = new MultiChannel(settings, endPointDiscoverer);
		}

		private static RecordedEvent ConvertToResolvedEvent(ReadResp.Types.ReadEvent.Types.RecordedEvent @event) =>
			new(@event.StreamIdentifier.StreamName.ToString(), Uuid.FromDto(@event.Id).ToGuid(),
				(long)@event.StreamRevision, @event.Metadata[Constants.Metadata.Type],
				@event.Data.ToByteArray(), @event.CustomMetadata.ToByteArray(),
				@event.Metadata[Constants.Metadata.ContentType] == Constants.Metadata.ContentTypes.ApplicationJson,
				default, default);

		private async ValueTask<CallInvoker> GetCallInvoker() {
			var channel = await _channel.GetCurrentChannel().ConfigureAwait(false);
			return channel.CreateCallInvoker().Intercept(
				new TypedExceptionInterceptor(new Dictionary<string, Func<RpcException, Exception>>()),
				new ConnectionNameInterceptor(ConnectionName),
				new ReportLeaderInterceptor(_channel.SetEndPoint));
		}

		public Task ConnectAsync() => Task.CompletedTask;

		public void Close() => _channel.Dispose();

		public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, bool hardDelete,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		// ReSharper disable RedundantArgumentDefaultValue
		// ReSharper disable RedundantCast
		public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, params EventData[] events) =>
			AppendToStreamAsync(stream, expectedVersion, (IEnumerable<EventData>)events, null);
		// ReSharper restore RedundantCast
		// ReSharper restore RedundantArgumentDefaultValue

		// ReSharper disable RedundantCast
		public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion,
			UserCredentials userCredentials, params EventData[] events) =>
			AppendToStreamAsync(stream, expectedVersion, (IEnumerable<EventData>)events, userCredentials);
		// ReSharper restore RedundantCast

		public async Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion,
			IEnumerable<EventData> events, UserCredentials userCredentials = null) {
			if (expectedVersion is -3 or < ExpectedVersion.StreamExists) {
				throw new ArgumentOutOfRangeException(nameof(expectedVersion));
			}

			var client = new Streams.StreamsClient(await GetCallInvoker().ConfigureAwait(false));
			using var call = client.Append(EventStoreCallOptions.Create(Settings, userCredentials));

			var options = new AppendReq.Types.Options {
				StreamIdentifier = new StreamIdentifier {
					StreamName = ByteString.CopyFromUtf8(stream)
				}
			};

			if (expectedVersion == ExpectedVersion.Any) {
				options.Any = new Empty();
			} else if (expectedVersion == ExpectedVersion.NoStream) {
				options.NoStream = new Empty();
			} else if (expectedVersion == ExpectedVersion.StreamExists) {
				options.StreamExists = new Empty();
			} else {
				options.Revision = (uint)expectedVersion;
			}

			await call.RequestStream.WriteAsync(new AppendReq {
					Options = options
				})
				.ConfigureAwait(false);

			foreach (var eventData in events) {
				var eventId = Uuid.FromGuid(eventData.EventId);
				await call.RequestStream.WriteAsync(new AppendReq {
					ProposedMessage = new AppendReq.Types.ProposedMessage {
						Data = ByteString.CopyFrom(eventData.Data),
						CustomMetadata = ByteString.CopyFrom(eventData.Data),
						Metadata = {
							{Constants.Metadata.Type, eventData.Type}, {
								Constants.Metadata.ContentType,
								eventData.IsJson
									? Constants.Metadata.ContentTypes.ApplicationJson
									: Constants.Metadata.ContentTypes.ApplicationOctetStream
							}
						},
						Id = eventId.ToDto()
					}
				}).ConfigureAwait(false);
			}

			await call.RequestStream.CompleteAsync().ConfigureAwait(false);

			var response = await call.ResponseAsync.ConfigureAwait(false);

			return response.ResultCase switch {
				AppendResp.ResultOneofCase.Success => new WriteResult(
					response.Success.CurrentRevisionOptionCase switch {
						AppendResp.Types.Success.CurrentRevisionOptionOneofCase.CurrentRevision =>
							(long)response.Success.CurrentRevision,
						AppendResp.Types.Success.CurrentRevisionOptionOneofCase.NoStream =>
							ExpectedVersion.NoStream,
						_ => ExpectedVersion.Any
					}, response.Success.PositionOptionCase switch {
						AppendResp.Types.Success.PositionOptionOneofCase.Position => new Position(
							(long)response.Success.Position.CommitPosition,
							(long)response.Success.Position.PreparePosition),
						_ => Position.End,
					}),
				AppendResp.ResultOneofCase.WrongExpectedVersion => throw new WrongExpectedVersionException(
					$"Append failed due to WrongExpectedVersion. Stream: {stream}, " +
					$"Expected version: {expectedVersion}, " +
					$@"Current version: {response.WrongExpectedVersion.CurrentRevisionOptionCase switch {
						AppendResp.Types.WrongExpectedVersion.CurrentRevisionOptionOneofCase.CurrentRevision =>
							(long)response.WrongExpectedVersion.CurrentRevision,
						_ => ExpectedVersion.NoStream
					}}", expectedVersion, response.WrongExpectedVersion.CurrentRevisionOptionCase switch {
						AppendResp.Types.WrongExpectedVersion.CurrentRevisionOptionOneofCase.CurrentRevision =>
							(long)response.WrongExpectedVersion.CurrentRevision,
						_ => ExpectedVersion.NoStream
					}),
				_ => throw new InvalidOperationException()
			};
		}

		public Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(string stream, long expectedVersion,
			IEnumerable<EventData> events, UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<EventStoreTransaction> StartTransactionAsync(string stream, long expectedVersion,
			UserCredentials userCredentials = null) => throw new NotSupportedException();

		public EventStoreTransaction ContinueTransaction(long transactionId, UserCredentials userCredentials = null) =>
			throw new NotSupportedException();

		public Task<EventReadResult> ReadEventAsync(string stream, long eventNumber, bool resolveLinkTos,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public async Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, long start, int count,
			bool resolveLinkTos, UserCredentials userCredentials = null) {
			var client = new Streams.StreamsClient(await GetCallInvoker().ConfigureAwait(false));
			using var call = client.Read(new ReadReq {
				Options = new ReadReq.Types.Options {
					Stream = new ReadReq.Types.Options.Types.StreamOptions {
						StreamIdentifier = new StreamIdentifier {
							StreamName = ByteString.CopyFromUtf8(stream)
						},
						Revision = (ulong)start
					},
					Count = (ulong)count,
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks = resolveLinkTos,
					UuidOption = new ReadReq.Types.Options.Types.UUIDOption {
						Structured = new Empty()
					},
					NoFilter = new Empty()
				}
			}, EventStoreCallOptions.Create(Settings, userCredentials));

			var events = new List<ResolvedEvent>();
			await foreach (var readResponse in call.ResponseStream.ReadAllAsync().ConfigureAwait(false)) {
				switch (readResponse.ContentCase) {
					case ReadResp.ContentOneofCase.StreamNotFound:
						return new StreamEventsSlice(SliceReadStatus.StreamNotFound, stream, start,
							ReadDirection.Forward,
							Array.Empty<ResolvedEvent>(),
							0, 0, true);
					case ReadResp.ContentOneofCase.Event:
						events.Add(new ResolvedEvent(ConvertToResolvedEvent(readResponse.Event.Event),
							ConvertToResolvedEvent(readResponse.Event.Link), readResponse.Event.PositionCase switch {
								ReadResp.Types.ReadEvent.PositionOneofCase.CommitPosition => new Position(
									(long)readResponse.Event.CommitPosition, (long)readResponse.Event.CommitPosition),
								_ => null
							}));
						break;
					default:
						throw new InvalidOperationException();
				}
			}

			return new StreamEventsSlice(SliceReadStatus.Success, stream, start, ReadDirection.Forward,
				events.ToArray(), 0, 0, false);
		}

		public Task<StreamEventsSlice> ReadStreamEventsBackwardAsync(string stream, long start, int count,
			bool resolveLinkTos, UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<AllEventsSlice> ReadAllEventsForwardAsync(Position position, int maxCount, bool resolveLinkTos,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<AllEventsSlice> FilteredReadAllEventsForwardAsync(Position position, int maxCount,
			bool resolveLinkTos, Filter filter, UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<AllEventsSlice> FilteredReadAllEventsForwardAsync(Position position, int maxCount,
			bool resolveLinkTos, Filter filter, int maxSearchWindow, UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<AllEventsSlice> ReadAllEventsBackwardAsync(Position position, int maxCount, bool resolveLinkTos,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<AllEventsSlice> FilteredReadAllEventsBackwardAsync(Position position, int maxCount,
			bool resolveLinkTos, Filter filter, int maxSearchWindow, UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<AllEventsSlice> FilteredReadAllEventsBackwardAsync(Position position, int maxCount,
			bool resolveLinkTos, Filter filter, UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, bool resolveLinkTos,
			Func<EventStoreSubscription, ResolvedEvent, Task> eventAppeared,
			Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint,
			CatchUpSubscriptionSettings settings,
			Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared,
			Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
			Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<EventStoreSubscription> SubscribeToAllAsync(bool resolveLinkTos,
			Func<EventStoreSubscription, ResolvedEvent, Task> eventAppeared,
			Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<EventStoreSubscription> FilteredSubscribeToAllAsync(bool resolveLinkTos, Filter filter,
			Func<EventStoreSubscription, ResolvedEvent, Task> eventAppeared,
			Func<EventStoreSubscription, Position, Task> checkpointReached,
			int checkpointInterval,
			Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<EventStoreSubscription> FilteredSubscribeToAllAsync(bool resolveLinkTos, Filter filter,
			Func<EventStoreSubscription, ResolvedEvent, Task> eventAppeared,
			Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(string stream, string groupName,
			Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task> eventAppeared,
			Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null, int bufferSize = 10,
			bool autoAck = true) {
			throw new NotImplementedException();
		}

		public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream,
			string groupName, Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task> eventAppeared,
			Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null, int bufferSize = 10,
			bool autoAck = true) {
			throw new NotImplementedException();
		}

		public EventStoreAllCatchUpSubscription SubscribeToAllFrom(Position? lastCheckpoint,
			CatchUpSubscriptionSettings settings,
			Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared,
			Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
			Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public EventStoreAllFilteredCatchUpSubscription FilteredSubscribeToAllFrom(Position? lastCheckpoint,
			Filter filter,
			CatchUpSubscriptionFilteredSettings settings,
			Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared,
			Func<EventStoreCatchUpSubscription, Position, Task> checkpointReached,
			int checkpointIntervalMultiplier, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
			Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public EventStoreAllFilteredCatchUpSubscription FilteredSubscribeToAllFrom(Position? lastCheckpoint,
			Filter filter,
			CatchUpSubscriptionFilteredSettings settings,
			Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared,
			Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
			Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task UpdatePersistentSubscriptionAsync(string stream, string groupName,
			PersistentSubscriptionSettings settings, UserCredentials credentials) {
			throw new NotImplementedException();
		}

		public Task CreatePersistentSubscriptionAsync(string stream, string groupName,
			PersistentSubscriptionSettings settings, UserCredentials credentials) {
			throw new NotImplementedException();
		}

		public Task DeletePersistentSubscriptionAsync(string stream, string groupName,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion,
			StreamMetadata metadata, UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, byte[] metadata,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<StreamMetadataResult>
			GetStreamMetadataAsync(string stream, UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task<RawStreamMetadataResult> GetStreamMetadataAsRawBytesAsync(string stream,
			UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public Task SetSystemSettingsAsync(SystemSettings settings, UserCredentials userCredentials = null) {
			throw new NotImplementedException();
		}

		public void Dispose() {
		}
	}
}
