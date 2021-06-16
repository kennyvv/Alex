namespace Alex.Items
{
	using System;
	using System.Collections.Generic;

	using System.Globalization;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public partial class ItemMapping
	{
		[JsonProperty("bedrock_id")]
		public long BedrockId { get; set; }

		[JsonProperty("bedrock_data")]
		public long BedrockData { get; set; }

		[JsonProperty("blockRuntimeId", NullValueHandling = NullValueHandling.Ignore)]
		public long? BlockRuntimeId { get; set; }

		[JsonProperty("stack_size", NullValueHandling = NullValueHandling.Ignore)]
		public long? StackSize { get; set; }

		[JsonProperty("tool_type", NullValueHandling = NullValueHandling.Ignore)]
		public string ToolType { get; set; }

		[JsonProperty("tool_tier", NullValueHandling = NullValueHandling.Ignore)]
		public string ToolTier { get; set; }
	}
}