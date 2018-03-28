using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
	namespace Fluid
	{
		public static class FluidLineExtensions
		{
			public static FluidLine NearestFluidLineTo(this IEnumerable<FluidLine> fluidLines, int xPosition)
			{
				if(fluidLines == null)
				{
					return null;
				}
				else
				{
					FluidLine nearestFluidLine = null;
					int minimalDistance = int.MaxValue;
					int currentDistance;

					foreach(FluidLine fluidLine in fluidLines)
					{
						currentDistance = Mathf.Abs (fluidLine.x - xPosition);
						if(currentDistance < minimalDistance)
						{
							nearestFluidLine = fluidLine;
							minimalDistance = currentDistance;
						}

						currentDistance = Mathf.Abs (fluidLine.x + fluidLine.length - 1 - xPosition);
						if(currentDistance < minimalDistance)
						{
							nearestFluidLine = fluidLine;
							minimalDistance = currentDistance;
						}
					}

					return nearestFluidLine;
				}
			}
		}
	}
}