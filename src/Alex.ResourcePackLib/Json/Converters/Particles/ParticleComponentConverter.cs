using System;
using System.Collections.Generic;
using Alex.ResourcePackLib.Json.Bedrock.Particles.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Alex.ResourcePackLib.Json.Converters.Particles
{
	public class ParticleComponentConverter : JsonConverter
	{
		/// <inheritdoc />
		public override bool CanWrite => false;

		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			Dictionary<string, ParticleComponent> components = new Dictionary<string, ParticleComponent>();

			var obj = JToken.Load(reader);

			if (obj.Type != JTokenType.Object) 
				return null;

			var jObj = (JObject) obj;

			foreach (var kvp in jObj)
			{
				if (kvp.Value == null)
					continue;
				switch (kvp.Key)
				{
					case "minecraft:particle_appearance_billboard":
						components.Add(kvp.Key, kvp.Value.ToObject<AppearanceComponent>(serializer));
						break;
					case "minecraft:particle_motion_dynamic":
						components.Add(kvp.Key, kvp.Value.ToObject<MotionComponent>(serializer));
						break;
					case "minecraft:emitter_rate_manual":
						components.Add(kvp.Key, kvp.Value.ToObject<EmitterRateComponent>(serializer));
						break;
					case "minecraft:particle_lifetime_expression":
						components.Add(kvp.Key, kvp.Value.ToObject<LifetimeExpressionComponent>(serializer));
						break;
				}
			}
			
			return components;
		}

		/// <inheritdoc />
		public override bool CanConvert(Type objectType)
		{
			return typeof(Dictionary<string,ParticleComponent>).IsAssignableFrom(objectType);
		}
	}
}