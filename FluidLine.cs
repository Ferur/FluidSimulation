using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terrain
{
	namespace Fluid
	{

		// represents one horizontal line of waterdrops in a possibly larger waterbody
		public class FluidLine : IComparable<FluidLine>
		{
			// Class variables

			private static uint nextInstanceID = 0;

			// Member variables

			private readonly uint instanceID;

			private HashSet<FluidLine> aboveLinesSet;
			private HashSet<FluidLine> belowLinesSet;
			private int[] sinkDistances;

			// Property variables

			private int _xPosition;
			private readonly int _yPosition;
			private int _length;
			private int _maxFlowCapacity;
			private int _flowCapacity;
			private uint _createdOnUpdateID;
			private uint _currentUpdateID;
			private bool _changed;

			#region Properties

			public bool JustCreated(uint updateID)
			{
				if(updateID == _createdOnUpdateID)
					return true;
				else
					return false;
			}

			public bool Changed
			{
				get { return _changed; }
				private set { _changed = value; }
			}

			public void ChangeProcessed()
			{
				Changed = false;
			}

			private int MaximumFlowCapacity
			{
				get { return _maxFlowCapacity; }
				set
				{
					if(value > 0)
						_maxFlowCapacity = value;
					else
						_maxFlowCapacity = 0;

					if(_maxFlowCapacity < FlowCapacity)
						FlowCapacity = _maxFlowCapacity;
				}
			}

			private void ResetFlowCapacity()
			{
				_flowCapacity = MaximumFlowCapacity;
			}

			/// <summary>
			/// Decrements the capacity by one.
			/// </summary>
			/// <returns><c>true</c>, if capacity was decremented, <c>false</c> otherwise.</returns>
			/// <param name="updateID">Through this ID the FluidLine knows when to reset its capacity.</param>
			public bool DecrementCapacity(uint updateID)
			{
				if(updateID != _currentUpdateID)
				{
					ResetFlowCapacity();
					_currentUpdateID = updateID;
				}
				
				if(FlowCapacity > 0)
				{
					FlowCapacity--;
					return true;
				}
				else
				{
					return false;
				}
			}

			public int GetFlowCapacity(uint updateID)
			{
				if(updateID != _currentUpdateID)
				{
					ResetFlowCapacity();
					_currentUpdateID = updateID;
				}

				return FlowCapacity;
			}

			private int FlowCapacity
			{
				get { return _flowCapacity; }
				set
				{
					_flowCapacity = value;
					if(_flowCapacity > MaximumFlowCapacity)
						_flowCapacity = MaximumFlowCapacity;
				}
			}

			public int x 
			{ 
				get { return _xPosition; } 
				private set 
				{
					if(value != _xPosition)
					{
						Changed = true;

						int difference = value - _xPosition;

						if(difference < 0)
						{
							SinkDistancesChanged(difference, -1, true);
							_length += Mathf.Abs (difference);
							_xPosition = value; 
							SinkDistancesChanged(_length - Mathf.Abs (difference), _length - 1, false);
							_length -= Mathf.Abs (difference);
						}
						else
						{
							SinkDistancesChanged(0, difference - 1, false);
							_length -= difference;
							_xPosition = value; 
							SinkDistancesChanged (_length, _length + difference - 1, true);
							_length += difference;
						}
					}
				}
			}
			
			public int y 
			{ 
				get { return _yPosition; } 
			}
			
			public int length
			{
				get { return _length; }
				private set 
				{ 
					if(value != _length)
					{
						Changed = true;

						if(sinkDistances != null)
						{
							if(value > _length)
								SinkDistancesChanged(_length, value - 1, true);
							else
								SinkDistancesChanged(value, _length - 1, false);
						}

						_length = value; 
						MaximumFlowCapacity = _length;
					}
				}
			}

			#endregion

			// public functions

			#region IComparable implementation

			public int CompareTo (FluidLine other)
			{
				if(other == null)
					return -1;

				if(this.y < other.y)
				{
					return -1;
				}
				else if(this.y > other.y)
				{
					return 1;
				}
				else
				{
					if(this.x < other.x)
					{
						return -1;
					}
					else if(this.x > other.x)
					{
						return 1;
					}
					else
					{
						if(this.instanceID == other.instanceID)
							return 0;
						else
							return -1;
					}
				}
			}

			#endregion
			#region public functions

			public override int GetHashCode ()
			{
				return ((17 * 31) + (int)instanceID) * 31 + this.y;
			}

			public override bool Equals (System.Object obj)
			{
				if(obj != null)
				{
					FluidLine other = obj as FluidLine;
					if(other != null)
						return Equals (other);
				}
				
				return false;
			}
			
			public bool Equals(FluidLine other)
			{
				if(other != null)
				{
					if(this.instanceID == other.instanceID)
						return true;
					else
						return false;
				}
				
				return false;
			}

			public override string ToString ()
			{
				return string.Format ("[FluidLine: x={0}, y={1}, length={2}]", x, y, length);
			}
			
			public FluidLine(int x, int y, int length, uint updateID)
			{
				// set the unique identifier
				instanceID = nextInstanceID;
				nextInstanceID++;

				// set readonly instance properties
				this._yPosition = y;

				// set the instance properties
				this._xPosition = x;
				this.length = length;
				aboveLinesSet = new HashSet<FluidLine>();
				belowLinesSet = new HashSet<FluidLine>();

				// initialize the internal status
				_changed = true;
				_createdOnUpdateID = updateID;

				CalculateSinkDistances ();
			}

			public int SinkDistanceAt(int position)
			{
				if(position < 0 || position >= sinkDistances.Length)
					Debug.Log("[ERROR] FluidLine = " + this.ToString () + ", position = " + position + ", sinkDistances = " + sinkDistances.ElementsToString ());

				return sinkDistances[position];
			}

			public void AddAboveLine(FluidLine aboveLine)
			{
				if(aboveLine.length > 0)
					aboveLinesSet.Add (aboveLine);
			}

			public void RemoveAboveLine(FluidLine aboveLine)
			{
				aboveLinesSet.Remove (aboveLine);
			}

			public void AddBelowLine(FluidLine belowLine)
			{
				if(belowLine.length > 0)
				{
					belowLinesSet.Add (belowLine);
				}
			}

			public void RemoveBelowLine(FluidLine belowLine)
			{
				belowLinesSet.Remove (belowLine);
			}

			public void AddAboveLines(ICollection<FluidLine> aboveLines)
			{
				foreach(FluidLine aboveLine in aboveLines)
					aboveLinesSet.Add (aboveLine);
			}

			public void AddBelowLines(ICollection<FluidLine> belowLines)
			{
				foreach(FluidLine belowLine in belowLines)
					belowLinesSet.Add (belowLine);
			}

			public ICollection<FluidLine> GetAboveLines ()
			{
				return aboveLinesSet;
			}
			
			public int GetAboveLinesCount()
			{
				return aboveLinesSet.Count;
			}

			public ICollection<FluidLine> GetBelowLines()
			{
				return belowLinesSet;
			}

			public int GetBelowLinesCount()
			{
				return belowLinesSet.Count;
			}
			
			public bool AddRight(FluidLine other)
			{
				if(this.y == other.y)
				{
					if(this.x + this.length == other.x)
					{
						length += other.length;

						foreach(FluidLine aboveLine in other.GetAboveLines ().ToArray ())
						{
							this.AddAboveLine(aboveLine);
							aboveLine.AddBelowLine (this);
						}

						foreach(FluidLine belowLine in other.GetBelowLines ().ToArray ())
						{
							this.AddBelowLine (belowLine);
							belowLine.AddAboveLine (this);
						}

						return true;
					}
					else
					{
						Debug.Log ("[ERROR]Trying to combine with a fluidList that isnt a neighbour anymore : " + this.ToString () + " + " + other.ToString ());
						return false;
					}
				}
				else
				{
					Debug.Log ("[ERROR]Trying to combine two lines that are on different heights.");
					return false;
				}
			}

			public bool AddLeft(FluidLine other)
			{
				if(this.y == other.y)
				{
					if(this.x == other.x + other.length)
					{
						this.x = other.x;
						this.length += other.length;
						
						foreach(FluidLine aboveLine in other.GetAboveLines ().ToArray ())
						{
							this.AddAboveLine(aboveLine);
							aboveLine.AddBelowLine (this);
						}
						
						foreach(FluidLine belowLine in other.GetBelowLines ().ToArray ())
						{
							this.AddBelowLine (belowLine);
							belowLine.AddAboveLine (this);
						}
						
						return true;
					}
					else
					{
						Debug.Log ("[ERROR]Trying to combine with a fluidList that isnt a neighbour anymore : " + this.ToString () + " + " + other.ToString ());
						return false;
					}
				}
				else
				{
					Debug.Log ("[ERROR]Trying to combine two lines that are on different heights.");
					return false;
				}
			}

			// checks if a FluidLine has one or more x-tiles in common with another FluidLine
			// and is 1 line above or below
			public bool VerticallyConnectedTo(FluidLine other)
			{
				if(this.length != 0 && other.length != 0)
				{
					if(this.y + 1 == other.y || this.y - 1 == other.y)
					{
						if(this.x <= other.x)
						{
							if(this.x + this.length > other.x)
								return true;
							else 
								return false;
						}
						else 
						{
							if(other.x + other.length > this.x)
								return true;
							else
								return false;
						}
					}
					else
					{
						Debug.Log ("[DEBUG] Not connected vertically because the y-coordinates dont match: this = " + this.ToString () + ", other = " + other.ToString ());
						return false;
					}
				}
				else
				{
					Debug.Log ("[ERROR] Not Connected because one of the FluidLines has 0 length");
					return false;
				}
			}
			
			// this function doesnt check the y-values for performance reasons. be careful
			public bool CanFlowTo(FluidLine other)
			{
				if(this.length != 0 && other.length != 0)
				{
					if(this.x <= other.x)
					{
						if(this.x + this.length >= other.x)
							return true;
						else
							return false;
					}
					else
					{
						if(other.x + other.length >= this.x)
							return true;
						else
							return false;
					}
				}
				else
				{
					return false;
				}
			}
			
			public bool Contains(int x)
			{
				if(this.x <= x && this.x + this.length > x)
					return true;
				else
					return false;
			}

			public bool RemoveDropRight()
			{
				return RemoveDropsRight(1);
			}

			public bool RemoveDropsRight(int dropsCount)
			{
				if(this.length > dropsCount)
				{
					this.length -= dropsCount;

					foreach(FluidLine belowFluidLine in GetBelowLines().ToArray())
					{
						if(!this.VerticallyConnectedTo (belowFluidLine))
						{
							this.RemoveBelowLine(belowFluidLine);
							belowFluidLine.RemoveAboveLine(this);
						}
					}
					
					foreach(FluidLine aboveFluidLine in GetAboveLines().ToArray ())
					{
						if(!this.VerticallyConnectedTo (aboveFluidLine))
						{
							this.RemoveAboveLine (aboveFluidLine);
							aboveFluidLine.RemoveBelowLine (this);
						}
					}
					return true;

				}
				else
				{
					Debug.Log("[ERROR] Not enough drops to remove.");
					return false;
				}
			}

			public bool RemoveDropLeft()
			{
				return RemoveDropsLeft (1);
			}

			public bool RemoveDropsLeft(int dropsCount)
			{
				if(this.length > dropsCount)
				{
					this.length -= dropsCount;
					this.x += dropsCount;

					foreach(FluidLine belowFluidLine in GetBelowLines().ToArray())
					{
						if(!this.VerticallyConnectedTo (belowFluidLine))
						{
							this.RemoveBelowLine(belowFluidLine);
							belowFluidLine.RemoveAboveLine(this);
						}
					}
					
					foreach(FluidLine aboveFluidLine in GetAboveLines().ToArray ())
					{
						if(!this.VerticallyConnectedTo (aboveFluidLine))
						{
							this.RemoveAboveLine (aboveFluidLine);
							aboveFluidLine.RemoveBelowLine (this);
						}
					}
					return true;
				}
				else
				{
					Debug.Log("[ERROR] Not enough drops to remove.");
					return false;
				}
			}
			
			public void AddRight(int dropsCount, FluidSimulation fluidSimulation)
			{
				length += dropsCount;

				// check if the line is now connected to a new line above
				List<FluidLine> aboveLines;
				if(fluidSimulation.TryGetFluidLines (this.y + 1, out aboveLines))
				{
					foreach(FluidLine aboveLine in aboveLines)
					{
						// check if it contains the new x-value
						if(this.VerticallyConnectedTo (aboveLine))
						{
							this.AddAboveLine (aboveLine);
							aboveLine.AddBelowLine (this);
						}
					}
				}
				
				// check if the line is now connected to a new line below
				List<FluidLine> belowLines;
				if(fluidSimulation.TryGetFluidLines(this.y - 1, out belowLines))
				{
					foreach(FluidLine belowLine in belowLines)
					{
						// check if it contains the new x-value
						if(this.VerticallyConnectedTo (belowLine))
						{
							this.AddBelowLine (belowLine);
							belowLine.AddAboveLine (this);
						}
					}
				}
			}

			public void AddLeft(int dropsCount, FluidSimulation fluidSimulation)
			{
				x -= dropsCount;
				length += dropsCount;

				// check if the line is now connected to a new line above
				List<FluidLine> aboveLines;
				if(fluidSimulation.TryGetFluidLines (this.y + 1, out aboveLines))
				{
					foreach(FluidLine aboveLine in aboveLines)
					{
						// check if it contains the new x-value
						if(this.VerticallyConnectedTo (aboveLine))
						{
							this.AddAboveLine (aboveLine);
							aboveLine.AddBelowLine (this);
						}
					}
				}
				
				// check if the line is now connected to a new line below
				List<FluidLine> belowLines;
				if(fluidSimulation.TryGetFluidLines(this.y - 1, out belowLines))
				{
					foreach(FluidLine belowLine in belowLines)
					{
						// check if it contains the new x-value
						if(this.VerticallyConnectedTo (belowLine))
						{
							this.AddBelowLine (belowLine);
							belowLine.AddAboveLine (this);
						}
					}
				}
			}

			// Removes the drop at position and every drop right from it from this FluidLine and
			// returns a new FluidLine from position + 1 to the end of the old FluidLine
			public FluidLine Split(int position, uint updateID)
			{
				if(position > 0 && position < this.length - 1)
				{
					FluidLine rightFluidLine = new FluidLine(this.x + position + 1, this.y, this.length - position - 1, updateID);
					
					SinkDistancesChanged (position, this.length - 1, false);
					this.RemoveDropsRight(this.length - position);

					return rightFluidLine;
				}
				else
				{
					Debug.Log ("[ERROR] No need to split as we only remove the left or right end of FluidLine " + this.ToString ());
					return null;
				}
			}

			#endregion
			#region private Functions

			// updates SinkDistances at the positions from leftPosition to rightPosition 
			// but uses the old data to only update where needed. This function needs to be called
			// before changes to x or length are processed!
			//
			// example(for index clarification): 
			// 01234 (fluidLine with already changed length as indexes)
			// l---r (l = 0 = leftPosition, r = 4 = rightPosition)
			// length = 5

			private void SinkDistancesChanged(int leftBound, int rightBound, bool add)
			{
				//string debugMessage = "(" + this.ToString () + ") Changed SinkDistances from " + sinkDistances.ElementsToString ();

				if(add)
				{
					int[] oldSinkDistances = sinkDistances;

					leftBound = Mathf.Min (0, leftBound);
					rightBound = Mathf.Max (length - 1, rightBound);

					// increase the sinkDistances array size
					sinkDistances = new int[rightBound + 1 - leftBound];
					// copy the old array to its new position in the array
					oldSinkDistances.CopyTo (sinkDistances, -leftBound);

					// this is the distance to the last found sink that is kept while propagating to the right
					int currentSinkDistance = int.MaxValue;

					for(int i = leftBound; i <= rightBound; i++)
					{
						// only process new blocks and skip old ones
						if(i == 0)
						{
							if(currentSinkDistance != int.MaxValue)
								currentSinkDistance++;

							// does the new or old side provide a shorter sinkDistance(if equal do nothing)
							if(sinkDistances[-leftBound] > currentSinkDistance)
							{
								// propagate the new sinkDistance to the right as far as possible
								for(int j = 0; j < length; j++)
								{
									if(sinkDistances[j - leftBound] > currentSinkDistance)
										sinkDistances[j - leftBound] = currentSinkDistance;
									else
										break;
									currentSinkDistance++;
								}
							}
							else if(sinkDistances[-leftBound] < currentSinkDistance)
							{
								currentSinkDistance = sinkDistances[-leftBound] + 1;

								// backpropagate the new sinkDistance to the left as far as possible
								for(int j = -leftBound - 1; j >= 0; j--)
								{
									if(sinkDistances[j] > currentSinkDistance)
										sinkDistances[j] = currentSinkDistance;
									else
										break;
									currentSinkDistance++;
								}
							}
						}
						else if(i > 0 && i < this.length - 1)
						{
							//  skip old blocks that dont need changing
							i = this.length - 2;
						}
						else if( i == this.length - 1)
						{
							if(currentSinkDistance != int.MaxValue)
								currentSinkDistance = sinkDistances[i - leftBound];
						}
						else
						{
							// these are the new blocks
							if(TerrainManager.instance.GetBlock(this.x + i, this.y - 1).Passable())
							{
								currentSinkDistance = 0;

								int distanceToCurrentSink = 0;
								for(int j = i - leftBound - 1; j >= 0; j--)
								{
									distanceToCurrentSink++;
									if(sinkDistances[j] > distanceToCurrentSink)
										sinkDistances[j] = distanceToCurrentSink;
									else
										break;
								}
							}
							else
							{
								if(currentSinkDistance != int.MaxValue)
									currentSinkDistance++;
							}

							sinkDistances[i - leftBound] = currentSinkDistance;
						}
					}
				}
				else
				{
					leftBound = Mathf.Max (0, leftBound);
					rightBound = Mathf.Min (this.length - 1, rightBound);

					if(rightBound - leftBound >= 0)
					{
						int[] oldSinkDistances = sinkDistances;

						if(leftBound == 0 && rightBound < length - 1)
						{
							// reduce the sinkDistances array size, keep the right part
							sinkDistances = new int[this.length - rightBound - 1];
							// copy the remaining part of the old array to its new array
							Array.Copy (oldSinkDistances, rightBound + 1, sinkDistances, 0, sinkDistances.Length);

							if(sinkDistances[0] > 0 && sinkDistances[0] != int.MaxValue)
							{
								bool sinkFound = false;
								for(int i = 0; i < sinkDistances.Length - 1; i++)
								{
									if(sinkDistances[i] > sinkDistances[i + 1])
									{
										// found the peak, go backwards and update
										for(int j = i - 1; j >= 0; j--)
											sinkDistances[j] = sinkDistances[j + 1] + 1;
										sinkFound = true;
										break;
									}
								}

								if(!sinkFound)
								{
									// no more sinks in this line
									for(int i = 0; i < sinkDistances.Length; i++)
										sinkDistances[i] = int.MaxValue;
								}
							}
						}
						else if(leftBound > 0 && rightBound == length - 1)
						{
							// reduce the sinkDistances array size, keep the left part
							sinkDistances = new int[leftBound];
							// copy the remaining part of the old array to its new array
							Array.Copy (oldSinkDistances, 0, sinkDistances, 0, sinkDistances.Length);

							if(sinkDistances[sinkDistances.Length - 1] > 0 && sinkDistances[sinkDistances.Length - 1] != int.MaxValue)
							{
								bool sinkFound = false;
								for(int i = sinkDistances.Length - 1; i > 0; i--)
								{
									if(sinkDistances[i] > sinkDistances[i - 1])
									{
										// found the peak, go backwards and update
										for(int j = i + 1; j < sinkDistances.Length; j++)
											sinkDistances[j] = sinkDistances[j - 1] + 1;
										sinkFound = true;
										break;
									}
								}

								if(!sinkFound)
								{
									// no more sinks in this line
									for(int i = 0; i < sinkDistances.Length; i++)
										sinkDistances[i] = int.MaxValue;
								}
							}
						}
						else if(leftBound == 0 && rightBound == length - 1)
						{
							// Delete all
							sinkDistances = new int[0];
						}
						else
						{
							Debug.Log ("[ERROR]SinkDistancesChanged(): Cant remove drops only in the middle of a FluidLine as this would result in two FluidLines.");
						}
					}
					else
					{
						Debug.Log ("[DEBUG]SinkDistancesChanged(): bounds define an empty area. LeftBound = " + leftBound.ToString () + ", RightBound = " + rightBound.ToString ());
					}
				}

				//Debug.Log (debugMessage + " to " + sinkDistances.ElementsToString ());
			}

			private void CalculateSinkDistances()
			{
				sinkDistances = new int[length];

				// Find the sinks and enter them into the array
				for(int i = 0; i < length; i++)
				{
					if(TerrainManager.instance.GetBlock (((float)(this.x + i) + 0.5f) * DataManager.instance.BlockSize,
					                                     ((float)(this.y - 1) + 0.5f) * DataManager.instance.BlockSize).Passable())
						sinkDistances[i] = 0;
					else
						sinkDistances[i] = int.MaxValue;
				}

				// propagate the sink distances accordingly
				for(int i = 0; i < length; i++)
				{
					if(sinkDistances[i] == 0)	
					{
						// update left
						for(int j = i - 1; j >= 0; j--)
						{
							int distance = i - j;
							if(distance < sinkDistances[j])
								sinkDistances[j] = distance;
							else 
								break;
						}
						
						// update right
						for(int j = i + 1; j < length; j++)
						{
							int distance = j - i;
							if(distance < sinkDistances[j])
								sinkDistances[j] = distance;
							else
								break;
						}
					}
				}

				//Debug.Log ("Calculated SinkDistances: " + sinkDistances.ElementsToString ());
			}

			#endregion
		}
	}
}
