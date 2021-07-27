using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.Messages;

namespace EventStore.ClientAPI {
	/// <summary>
	/// A structure representing a single event or an resolved link event.
	/// </summary>
	public struct ResolvedEvent : IResolvedEvent {
		/// <summary>
		/// The event, or the resolved link event if this <see cref="ResolvedEvent"/> is
		/// a link event.
		/// </summary>
		public readonly RecordedEvent Event;

		/// <summary>
		/// The link event if this <see cref="ResolvedEvent"/> is a link event.
		/// </summary>
		public readonly RecordedEvent Link;

		/// <summary>
		/// Returns the event that was read or which triggered the subscription.
		/// 
		/// If this <see cref="ResolvedEvent"/> represents a link event, the Link
		/// will be the <see cref="OriginalEvent"/>, otherwise it will be the
		/// event.
		/// </summary>
		public RecordedEvent OriginalEvent {
			get { return Link ?? Event; }
		}

		/// <summary>
		/// Indicates whether this <see cref="ResolvedEvent"/> is a resolved link
		/// event.
		/// </summary>
		public bool IsResolved {
			get { return Link != null && Event != null; }
		}

		/// <summary>
		/// The logical position of the <see cref="OriginalEvent"/>.
		/// </summary>
		public readonly Position? OriginalPosition;

		/// <summary>
		/// The stream name of the <see cref="OriginalEvent" />.
		/// </summary>
		public string OriginalStreamId {
			get { return OriginalEvent.EventStreamId; }
		}

		/// <summary>
		/// The event number in the stream of the <see cref="OriginalEvent"/>.
		/// </summary>
		public long OriginalEventNumber {
			get { return OriginalEvent.EventNumber; }
		}

		internal ResolvedEvent(ClientMessage.ResolvedEvent evnt) : this(
			evnt.Event == null ? null : new RecordedEvent(evnt.Event),
			evnt.Link == null ? null : new RecordedEvent(evnt.Link),
			new Position(evnt.CommitPosition, evnt.PreparePosition)) {
		}

		internal ResolvedEvent(ClientMessage.ResolvedIndexedEvent evnt): this(
			evnt.Event == null ? null : new RecordedEvent(evnt.Event),
			evnt.Link == null ? null : new RecordedEvent(evnt.Link),
			null) {
		}

		internal ResolvedEvent(RecordedEvent @event, RecordedEvent link, Position? originalPosition) {
			Event = @event;
			Link = link;
			OriginalPosition = originalPosition;
		}

		Position? IResolvedEvent.OriginalPosition => OriginalPosition;
		RecordedEvent IResolvedEvent.OriginalEvent => OriginalEvent;
		long IResolvedEvent.OriginalEventNumber => OriginalEventNumber;
		string IResolvedEvent.OriginalStreamId => OriginalStreamId;
	}
}
