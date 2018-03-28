using UnityEngine;
using System.Collections;

namespace Terrain
{
	namespace Fluid
	{
		public class FluidLineKey
		{
			private int x;
			private int y;
			private int length;

			public FluidLineKey(FluidLine fluidLine)
			{
				this.x = fluidLine.x;
				this.y = fluidLine.y;
				this.length = fluidLine.length;
			}

			public override bool Equals (object obj)
			{
				if(obj == null)
					return false;

				FluidLineKey fluidLineKey = (FluidLineKey) obj;

				return Equals (fluidLineKey);
			}

			public bool Equals(FluidLineKey other)
			{
				if(other == null)
					return false;

				if(this.x == other.x && this.y == other.y && this.length == other.length)
					return true;
				else
					return false;
			}

			public override int GetHashCode()
			{
				return (((17 * 31) + this.x) * 31 + this.y) * 31 + this.length;
			}
		}

	}
}
