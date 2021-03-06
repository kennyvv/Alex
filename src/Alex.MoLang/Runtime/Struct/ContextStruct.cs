using System;
using System.Collections.Generic;
using Alex.MoLang.Runtime.Value;
using Alex.MoLang.Utils;

namespace Alex.MoLang.Runtime.Struct
{
	public class ContextStruct : VariableStruct
	{
		public ContextStruct() : base()
		{
			
		}
		
		public ContextStruct(IEnumerable<KeyValuePair<string, IMoValue>> values) : base(values)
		{
			
		}
		
		/// <inheritdoc />
		public override void Set(MoPath key, IMoValue value)
		{
			throw new NotSupportedException("Read-only context");
		}
	}
}