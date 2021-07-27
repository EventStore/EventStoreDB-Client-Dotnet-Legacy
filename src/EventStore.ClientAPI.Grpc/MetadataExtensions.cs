using Grpc.Core;

#nullable enable
namespace EventStore.ClientAPI {
	internal static class MetadataExtensions {
		public static bool TryGetValue(this Metadata metadata, string key, out string? value) {
			value = default;

			foreach (var entry in metadata) {
				if (entry.Key != key) {
					continue;
				}

				value = entry.Value;
				return true;
			}

			return false;
		}

		public static int GetIntValueOrDefault(this Metadata metadata, string key)
			=> metadata.TryGetValue(key, out var s) && int.TryParse(s, out var value)
				? value
				: default;
	}
}
