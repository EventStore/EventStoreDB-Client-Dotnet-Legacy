using System;

namespace EventStore.ClientAPI {
	public static class GlobalEnvironment {
		static GlobalEnvironment() {
			var useClusterEnvVar = Environment.GetEnvironmentVariable(UseClusterName);
			if (bool.TryParse(useClusterEnvVar, out var useCluster)) {
				UseCluster = useCluster;
			}
		}

		public static bool UseCluster { get; } = false;

		public static string ImageTag => GetEnvironmentVariable(ImageTagName, ImageTagDefault);
		public static string DbLogFormat => GetEnvironmentVariable(DbLogFormatName, DbLogFormatDefault);

		public static string[] EnvironmentVariables => new[] {
			$"{ImageTagName}={ImageTag}",
			$"{DbLogFormatName}={DbLogFormat}",
		};

		static string UseClusterName => "EVENTSTORE_CLIENT_TESTS_USE_CLUSTER";
		static string ImageTagName => "EVENTSTORE_CLIENT_TESTS_IMAGE_TAG";
		static string ImageTagDefault => "ci";
		static string DbLogFormatName => "EVENTSTORE_DB_LOG_FORMAT";
		static string DbLogFormatDefault => "V2";

		static string GetEnvironmentVariable(string name, string def) {
			var x = Environment.GetEnvironmentVariable(name);
			return string.IsNullOrWhiteSpace(x) ? def : x;
		}
	}
}
