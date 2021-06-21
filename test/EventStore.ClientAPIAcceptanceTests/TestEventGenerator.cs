using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventStore.ClientAPI {
	public static class TestEventGenerator {
		private const string TestEventType = "-";

		public static IEnumerable<EventData> CreateTestEvents(int count = 1, int metadataSize = 1)
			=> Enumerable.Range(0, count).Select(x => CreateTestEvent(x, metadataSize));

		private static EventData CreateTestEvent(int index, int metadataSize) =>
			new EventData(
				eventId: Guid.NewGuid(),
				type: TestEventType,
				isJson: true,
				data: Encoding.UTF8.GetBytes($@"{{""x"":{index}}}"),
				metadata: Encoding.UTF8.GetBytes("\"" + new string('$', metadataSize) + "\""));
	}
}
