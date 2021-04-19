using System;
using System.Text;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.Internal;

namespace EventStore.ClientAPI {
	/// <summary>
	/// Represents a previously written event
	/// </summary>
	public class RecordedEvent {
		/// <summary>
		/// The Event Stream that this event belongs to
		/// </summary>
		public readonly string EventStreamId;

		/// <summary>
		/// The Unique Identifier representing this event
		/// </summary>
		public readonly Guid EventId;

		/// <summary>
		/// The number of this event in the stream
		/// </summary>
		public readonly long EventNumber;

		/// <summary>
		/// The type of event this is
		/// </summary>
		public readonly string EventType;

		/// <summary>
		/// A byte array representing the data of this event
		/// </summary>
		public readonly byte[] Data;

		/// <summary>
		/// A byte array representing the metadata associated with this event
		/// </summary>
		public readonly byte[] Metadata;

		/// <summary>
		/// Indicates whether the content is internally marked as json
		/// </summary>
		public readonly bool IsJson;

		/// <summary>
		/// A datetime representing when this event was created in the system
		/// </summary>
		public DateTime Created;

		/// <summary>
		/// A long representing the milliseconds since the epoch when the was created in the system
		/// </summary>
		public long CreatedEpoch;


#if DEBUG
		/// <summary>
		/// Shows the event data interpreted as a UTF8-encoded string.
		/// 
		/// NOTE: This is only available in DEBUG builds of the client API.
		/// </summary>
		public string DebugDataView {
			get { return Encoding.UTF8.GetString(Data); }
		}

		/// <summary>
		/// Shows the event metadata interpreted as a UTF8-encoded string.
		/// 
		/// NOTE: This is only available in DEBUG builds of the client API.
		/// </summary>
		public string DebugMetadataView {
			get { return Encoding.UTF8.GetString(Metadata); }
		}
#endif

		internal RecordedEvent(string eventStreamId, Guid eventId, long eventNumber, string eventType, byte[] data,
			byte[] metadata, bool isJson, DateTime created, long createdEpoch) {
			EventStreamId = eventStreamId;
			EventId = eventId;
			EventNumber = eventNumber;
			EventType = eventType;
			Data = data;
			Metadata = metadata;
			IsJson = isJson;
			Created = created;
			CreatedEpoch = createdEpoch;
		}

		internal RecordedEvent(ClientMessage.EventRecord systemRecord) : this(
			systemRecord.EventStreamId, new Guid(systemRecord.EventId), systemRecord.EventNumber,
			systemRecord.EventType, systemRecord.Data ?? Array.Empty<byte>(),
			systemRecord.Metadata ?? Array.Empty<byte>(), systemRecord.DataContentType == 1,
			systemRecord.Created.HasValue ? DateTime.FromBinary(systemRecord.Created.Value) : default,
			systemRecord.CreatedEpoch.GetValueOrDefault()) {
		}
	}
}
