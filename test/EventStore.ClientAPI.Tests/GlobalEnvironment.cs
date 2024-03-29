﻿using System;

namespace EventStore.ClientAPI {
	public static class GlobalEnvironment {
		static GlobalEnvironment() {
			var useClusterEnvVar = Environment.GetEnvironmentVariable(UseClusterName);
			if (bool.TryParse(useClusterEnvVar, out var useCluster)) {
				UseCluster = useCluster;
			}
		}

		public static bool UseCluster { get; } = false;

		public static string Image => GetEnvironmentVariable(ImageName, ImageDefault);
		public static string ImageTag => GetEnvironmentVariable(ImageTagName, ImageTagDefault);
		public static string DbLogFormat => GetEnvironmentVariable(DbLogFormatName, DbLogFormatDefault);

		public static string[] EnvironmentVariables => new[] {
			$"{ImageName}={Image}",
			$"{ImageTagName}={ImageTag}",
			$"{DbLogFormatName}={DbLogFormat}",
		};

		public static bool IsLogV2 => string.Equals(DbLogFormat, "V2", StringComparison.OrdinalIgnoreCase);
		public static bool IsLogV3 => string.Equals(DbLogFormat, "experimentalV3", StringComparison.OrdinalIgnoreCase);

		static string UseClusterName => "ES_USE_CLUSTER";
		static string ImageName => "ES_DOCKER_IMAGE";
		static string ImageDefault => "ghcr.io/eventstore/eventstore"; // old: "docker.pkg.github.com/eventstore/eventstore/eventstore"
		static string ImageTagName => "ES_DOCKER_TAG";
		static string ImageTagDefault => "ci"; // e.g. "21.6.0-focal";
		static string DbLogFormatName => "EVENTSTORE_DB_LOG_FORMAT";
		static string DbLogFormatDefault => "V2";

		static string GetEnvironmentVariable(string name, string def) {
			var x = Environment.GetEnvironmentVariable(name);
			return string.IsNullOrWhiteSpace(x) ? def : x;
		}
	}
}
