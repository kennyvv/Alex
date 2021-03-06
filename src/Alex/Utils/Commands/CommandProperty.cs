using System;
using System.IO;
using System.Linq;
using Alex.Common.Data;
using NLog;
using NLog.Fluent;

namespace Alex.Utils.Commands
{
	public class CommandProperty
	{
		public string Name { get; }
		public bool Required { get; }

		public string TypeIdentifier { get; set; }

		public CommandProperty(string name, bool required = true, string typeIdentifier = "Unknown")
		{
			Name = name;
			Required = required;
			TypeIdentifier = typeIdentifier;
		}

		public string[] Matches { get; set; } = new string[0];
		public virtual bool TryParse(SeekableTextReader reader)
		{
			if (reader.ReadSingleWord(out string result) > 0)
			{
				Matches = new string[] {result};
				return true;
			}

			return false;
		}
		
		/// <inheritdoc />
		public override string ToString()
		{
			return Required ? $"<{Name}: {TypeIdentifier}>" : $"[{Name}: {TypeIdentifier}]";
		}
	}
	
	public class EnumCommandProperty : CommandProperty
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger(typeof(EnumCommandProperty));
		public string[] Options { get; }

		/// <inheritdoc />
		public EnumCommandProperty(string name, bool required = true, string[] options = null, string enumName = "enum") : base(name, required, enumName)
		{
			Options = options;
		}

		/// <inheritdoc />
		public override bool TryParse(SeekableTextReader reader)
		{
			if (reader.ReadSingleWord(out string result) > 0)
			{
				Log.Debug($"Enum Read: {result}");
				var options = Options.Any(x => x.StartsWith(result, StringComparison.InvariantCultureIgnoreCase));

				if (options)
				{
					Matches = Options.Where(x => x.StartsWith(result, StringComparison.InvariantCultureIgnoreCase))
					   .ToArray();
					return true;
				}
			}
			
		//	Log.Debug($"")

			return false;
		}
	}

	public class TargetCommandProperty : CommandProperty
	{
		/// <inheritdoc />
		public TargetCommandProperty(string name, bool required = true) : base(name, required, "target")
		{
			
		}
	}

	public class TextCommandProperty : CommandProperty
	{
		/// <inheritdoc />
		public TextCommandProperty(string name, bool required = true) : base(name, required, "text")
		{
			
		}
	}
	
	public class IntCommandProperty : CommandProperty
	{
		public int MinValue { get; set; }
		public int MaxValue { get; set; }
		/// <inheritdoc />
		public IntCommandProperty(string name, bool required = true) : base(name, required, "integer")
		{
			
		}
		
		/// <inheritdoc />
		public override bool TryParse(SeekableTextReader reader)
		{
			if (reader.ReadSingleWord(out string result) > 0)
			{
				if (int.TryParse(result, out int val))
				{
					return true;
				}
			}

			return false;
		}
	}
	
	public class FloatCommandProperty : CommandProperty
	{
		public float MinValue { get; set; }
		public float MaxValue { get; set; }
		/// <inheritdoc />
		public FloatCommandProperty(string name, bool required = true) : base(name, required, "float")
		{
			
		}
		
		/// <inheritdoc />
		public override bool TryParse(SeekableTextReader reader)
		{
			if (reader.ReadSingleWord(out string result) > 0)
			{
				if (float.TryParse(result, out float val))
				{
					return true;
				}
			}

			return false;
		}
	}
	
	public class DoubleCommandProperty : CommandProperty
	{
		public double MinValue { get; set; }
		public double MaxValue { get; set; }
		/// <inheritdoc />
		public DoubleCommandProperty(string name, bool required = true) : base(name, required, "float")
		{
			
		}
		
		/// <inheritdoc />
		public override bool TryParse(SeekableTextReader reader)
		{
			if (reader.ReadSingleWord(out string result) > 0)
			{
				if (double.TryParse(result, out double val))
				{
					return true;
				}
			}

			return false;
		}
	}

	public class AskServerProperty : CommandProperty
	{
		/// <inheritdoc />
		public AskServerProperty(string name, bool required = true, string typeIdentifier = "Unknown") : base(name, required, typeIdentifier) { }
	}
}