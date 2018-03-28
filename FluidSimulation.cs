using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Terrain
{
	namespace Fluid
	{
		public class FluidSimulation
		{
			// all FluidLines sorted by their height
			private SortedDictionary<int, List<FluidLine>> fluidLines = new SortedDictionary<int, List<FluidLine>>();
			// list of all active Lines
			private DynamicSortedList<FluidLine> activeLines = new DynamicSortedList<FluidLine>();
			private List<FluidLine> deletedLines = new List<FluidLine>();

			private uint _UpdateID = 0;
			private uint UpdateID { get { return _UpdateID; } }

			private void IncrementUpdateID()
			{
				_UpdateID++;
			}

			public FluidSimulation()
			{

			}

			public void AddDrops(ICollection<Vector2> dropPositions)
			{
				throw new System.NotImplementedException();
			}

			public void AddFluidQuad(int left, int bottom, int width, int height)
			{
				FluidLine newFluidLine;
				for(int y = bottom; y < bottom + height; y++)
				{
					newFluidLine = CreateFluidLine (left, y, width);
					activeLines.Add(newFluidLine);
				}
			}

			public ICollection<FluidLine> GetFluidLines ()
			{
				List<FluidLine> completeFluidLineList = new List<FluidLine>();

				foreach(List<FluidLine> fluidLine in fluidLines.Values)
				{
					if(fluidLine.Count > 0)
						completeFluidLineList.AddRange (fluidLine);
				}

				return completeFluidLineList;
			}

			public ICollection<FluidLine> GetDeletedFluidLines()
			{
				return deletedLines;
			}

			/*public void AddLines(ICollection<FluidLine> newLines)
			{
				int minValue = newLines.Min (fluidLine => fluidLine.y);
				int maxValue = newLines.Max (fluidLine => fluidLine.y);

				IEnumerable<FluidLine> linesToAdd;

				for(int i = maxValue; i >= minValue; i--)
				{
					linesToAdd = newLines.Where (fluidLine => fluidLine.y == i);

					foreach(FluidLine fluidLine in linesToAdd)
						AddLine (fluidLine);
				}
			}*/

			/*public void AddLine(FluidLine fluidLine)
			{
				UpdateAboveLines (fluidLine);

				// set it to active
				activeLines.Add (fluidLine);

				// add it to the sorted dictionary
				List<FluidLine> fluidLineList;
				if(fluidLines.TryGetValue (fluidLine.y, out fluidLineList))
				{
					fluidLineList.Add (fluidLine);
				}
				else
				{
					fluidLineList = new List<FluidLine>();
					fluidLineList.Add (fluidLine);
					fluidLines.Add (fluidLine.y, fluidLineList);
				}
			}*/

			/*public void UpdateAboveLines(FluidLine fluidLine)
			{
				List<FluidLine> aboveLines;

				if(fluidLines.TryGetValue(fluidLine.y + 1, out aboveLines))
				{
					foreach(FluidLine aboveLine in aboveLines)
					{
						// if the line is above and connected, add it
						if(fluidLine.ConnectedTo (aboveLine))
							fluidLine.AddAboveLine(aboveLine);
					}
				}
			}*/

			public bool TryGetFluidLines(int y, out List<FluidLine> fluidLineList)
			{
				if(fluidLines.TryGetValue (y, out fluidLineList))
					return true;
				else 
					return false;
			}

			private FluidLine CreateFluidLine(int x, int y, int length)
			{
				FluidLine fluidLine = new FluidLine(x, y, length, UpdateID);
				RegisterFluidLine (fluidLine);

				return fluidLine;
			}

			private void RegisterFluidLine(FluidLine fluidLine)
			{
				List<FluidLine> fluidLineList;

				// add the new FluidLine into the SortedDictionary of all FluidLines
				if(fluidLines.TryGetValue (fluidLine.y, out fluidLineList))
				{
					fluidLineList.Add (fluidLine);
				}
				else
				{
					fluidLineList = new List<FluidLine>();
					fluidLineList.Add (fluidLine);
					fluidLines.Add (fluidLine.y, fluidLineList);
				}

				// connect to above FluidLines
				if(fluidLines.TryGetValue (fluidLine.y + 1, out fluidLineList))
				{
					foreach(FluidLine aboveFluidLine in fluidLineList)
					{
						if(fluidLine.VerticallyConnectedTo (aboveFluidLine))
						{
							fluidLine.AddAboveLine(aboveFluidLine);
							aboveFluidLine.AddBelowLine (fluidLine);
						}
					}
				}
			
				// connect to below FluidLines
				if(fluidLines.TryGetValue (fluidLine.y - 1, out fluidLineList))
				{
					foreach(FluidLine belowFluidLine in fluidLineList)
					{
						if(fluidLine.VerticallyConnectedTo (belowFluidLine))
						{
							fluidLine.AddBelowLine (belowFluidLine);
							belowFluidLine.AddAboveLine (fluidLine);
						}
					}
				}
			}

			public void DeleteFluidLine(FluidLine fluidLine)
			{
				List<FluidLine> fluidLineList;

				if(fluidLines.TryGetValue (fluidLine.y, out fluidLineList))
				{
					foreach(FluidLine belowFluidLine in fluidLine.GetBelowLines())
						belowFluidLine.RemoveAboveLine (fluidLine);

					foreach(FluidLine aboveFluidLine in fluidLine.GetAboveLines())
						aboveFluidLine.RemoveBelowLine (fluidLine);

					if(!fluidLineList.Remove (fluidLine))
					{
						Debug.Log ("[ERROR]DeleteFluidLine(): Couldnt find FluidLineList to remove from.");
						Debug.Log ("FluidLine = " + fluidLine.ToString ());
					}
				}

				activeLines.Remove (fluidLine);

				if(!fluidLine.JustCreated (UpdateID))
					deletedLines.Add (fluidLine);
			}
			
			private FluidLine processedLine;

			public void UpdateFluid()//Fluid fluid)
			{
				int flowX; // the x value of the current fluidLine. This can change during flowingUpdate
				int flowY;
				int lineLength;
				bool containsWater;
				FluidLine fluidLeft;
				FluidLine fluidRight;

				deletedLines.Clear ();

				// check all active Lines
				foreach(FluidLine activeLine in activeLines)
				{
					//Debug.Log ("Current Line = " + activeLine.ToString ());
					//Debug.Log ("Active Lines = " + activeLines.ToString());

					processedLine = activeLine;

					/*Debug.DrawLine (new Vector3(((float)(processedLine.x) + 0.5f) * DataManager.instance.BlockSize, 
					                            ((float)(processedLine.y) + 0.5f) * DataManager.instance.BlockSize,
					                            -2f),
					                new Vector3(((float)(processedLine.x + processedLine.length) + 0.5f) * DataManager.instance.BlockSize, 
					            ((float)(processedLine.y) + 0.5f) * DataManager.instance.BlockSize,
					            -2f),
					                Color.red,
					                0.5f);
					
					fluid.ShowFluidUpdate(GetFluidLines (), GetDeletedFluidLines ());
					yield return new WaitForSeconds(0.2f);*/

					if(processedLine.length == 0)
						Debug.Log ("Empty line detected " + processedLine.ToString ());

					// if the fluid has flown outside the map area
					if(!TerrainManager.instance.InsideMap ((float)(processedLine.x) * DataManager.instance.BlockSize, (float)(processedLine.y) * DataManager.instance.BlockSize)
                        && !TerrainManager.instance.InsideMap((float)(processedLine.x + processedLine.length - 1) * DataManager.instance.BlockSize, (float)(processedLine.y) * DataManager.instance.BlockSize))
					{
						DeleteFluidLine (processedLine);
						break;
					}

					// first check if the fluid can flow down
					flowY = processedLine.y - 1;


					// get the lines that might get changed below this fluidLine
					List<FluidLine> allBelowFluidLines;
					List<FluidLine> canFlowToFluidLines = new List<FluidLine>();
					if(fluidLines.TryGetValue (flowY, out allBelowFluidLines))
					{
						foreach(FluidLine belowFluidLine in allBelowFluidLines)
						{
							if(processedLine.CanFlowTo (belowFluidLine))
								canFlowToFluidLines.Add (belowFluidLine);
						}
					}

					flowX = processedLine.x;
					lineLength = processedLine.length;

					// Check all blocks underneath if the fluid can flow there
					for(int j = 0; j < lineLength; j++)
					{
						if(TerrainManager.instance.GetBlock (((float)(flowX + j) + 0.5f) * DataManager.instance.BlockSize,
						                                     ((float)(flowY) + 0.5f) * DataManager.instance.BlockSize).Passable())
						{
							containsWater = false;
							fluidLeft = null;
							fluidRight = null;
							
							// check all fluidLines to which this line can flow to,
							// whether this block actually contains or is next to fluid
							foreach(FluidLine fluidLine in canFlowToFluidLines)
							{
								if(fluidLine.Contains (flowX + j))
								{
									containsWater = true;
									int waterAmount = 1;
									while(fluidLine.Contains (flowX + j + waterAmount) && (j + waterAmount - 1) < lineLength)
									{
										waterAmount++;
									}
									j += waterAmount - 1;
									break;
								}
								else if(fluidLine.Contains (flowX + j - 1))
								{
									fluidLeft = fluidLine;
								}
								else if(fluidLine.Contains (flowX + j + 1))
								{
									fluidRight = fluidLine;
								}
							}

							if(!containsWater)
							{
								if(RemoveTop(processedLine, flowX + j))
								{
									// Fill Block with water
									if(fluidLeft != null)
									{
										fluidLeft.AddRight(1, this);

										if(fluidRight != null)
										{
											if(fluidLeft.AddRight (fluidRight))
											{
												canFlowToFluidLines.Remove (fluidRight);
												DeleteFluidLine (fluidRight);
											}
											else
											{
												Debug.Log ("[ERROR]Couldnt combine fluidLines!");
											}
										}
									}
									else if(fluidRight != null)
									{
										fluidRight.AddLeft (1, this);
									}
									else
									{
										FluidLine newFluidLine = CreateFluidLine (flowX + j, flowY, 1);
										canFlowToFluidLines.Add (newFluidLine);
										activeLines.Add (newFluidLine);
									}
								}
							}
						}
					}

					// we are now looking on the same level on the left and right side instead of the level below
					flowY = processedLine.y;

					bool canFlowSideways = false;

					// check if we can still flow sideways
					if(processedLine.length > 0)
					{
						int capacityAbove = 0;

						foreach(FluidLine aboveLine in processedLine.GetAboveLines ())
							capacityAbove += aboveLine.GetFlowCapacity(UpdateID);

						if(capacityAbove > 0)
							canFlowSideways = true;
					}

					if(canFlowSideways)
					{
						List<FluidLine> possibleNeighbours;
						bool leftContainsWater = false;
						bool rightContainsWater = false;
						FluidLine leftNeighbour = null;

						if(fluidLines.TryGetValue (flowY, out possibleNeighbours))
						{
							foreach(FluidLine possibleNeighbour in possibleNeighbours)
							{
								if(leftNeighbour == null)
								{
									// is there a fluidline with which this line needs to combine, if it flows left
									if(possibleNeighbour.x + possibleNeighbour.length == processedLine.x - 1)
											leftNeighbour = possibleNeighbour;
								}

								if(possibleNeighbour.Contains (processedLine.x - 1))
								{
									leftContainsWater = true;
									leftNeighbour = possibleNeighbour;
								}

								if(possibleNeighbour.Contains (processedLine.x + processedLine.length))
								{
									rightContainsWater = true;
								}
							}
						}
						else
						{
							Debug.Log ("[ERROR] Couldnt even find my own FluidLine.");
						}
						
						// check left side
						if(leftContainsWater)
						{
							if(processedLine.AddLeft (leftNeighbour))
								DeleteFluidLine (leftNeighbour);
							else
								Debug.Log ("[ERROR]There are two lines directly next to each other, but combining them failed! Maybe check for overlap?");
						}
						else if(TerrainManager.instance.GetBlock (((float)(processedLine.x - 1) + 0.5f) * DataManager.instance.BlockSize,
						                                     ((float)(flowY) + 0.5f) * DataManager.instance.BlockSize).Passable())
						{
							if(RemoveTop(processedLine, processedLine.x - 1))
							{
								// Fill Block with water
								processedLine.AddLeft (1, this);

								// combine with left FluidLine if there is one
								if(leftNeighbour != null)
								{
									if(processedLine.AddLeft (leftNeighbour))
										DeleteFluidLine (leftNeighbour);
									else
										Debug.Log ("[ERROR]There are two lines directly next to each other, but combining them failed! Maybe check for overlap?");
								}
							}
						}

						// if the right side contains water, this line will be added to the rightNeighbour when its processed
						if(!rightContainsWater)
						{
							bool canFlowRight = false;

							// check if we can still flow to the right
							if(processedLine.length > 0)
							{
								int capacityAbove = 0;
								
								foreach(FluidLine aboveLine in processedLine.GetAboveLines ())
									capacityAbove += aboveLine.GetFlowCapacity(UpdateID);
								
								if(capacityAbove > 0)
									canFlowRight = true;
							}

							// still water above us?
							if(canFlowRight)
							{
								if(TerrainManager.instance.GetBlock (((float)(processedLine.x + processedLine.length) + 0.5f) * DataManager.instance.BlockSize,
								                                     ((float)(flowY) + 0.5f) * DataManager.instance.BlockSize).Passable())
								{
									if(RemoveTop(processedLine, processedLine.x + processedLine.length))
									{
										// Fill Block with water
										processedLine.AddRight (1, this);
									}
								}
							}
						}
					}
				}

				IncrementUpdateID();
			}

			private bool RemoveTop(FluidLine fluidLine, int flowingToXPosition)
			{
				if(fluidLine.GetFlowCapacity(UpdateID) == 0)
					return false;

				List<Stack<FluidLine>> topCandidates = new List<Stack<FluidLine>>();
				List<Stack<FluidLine>> activeFluidLineStacks;
				List<Stack<FluidLine>> newActiveFluidLineStacks = new List<Stack<FluidLine>>();

				Stack<FluidLine> fluidLineStack = new Stack<FluidLine>();

				fluidLineStack.Push (fluidLine);
				newActiveFluidLineStacks.Add (fluidLineStack);
				
				// find all topLevel FluidLines = all lines with no lines above them and connected
				while(newActiveFluidLineStacks.Count > 0)
				{
					topCandidates.Clear ();

					activeFluidLineStacks = newActiveFluidLineStacks;
					newActiveFluidLineStacks = new List<Stack<FluidLine>>();

					for(int i = 0; i < activeFluidLineStacks.Count; i++)
					{
						// is this the top line?
						if(activeFluidLineStacks[i].Peek ().GetAboveLinesCount() == 0)
						{
							topCandidates.Add (activeFluidLineStacks[i]);
						}
						else
						{
							bool noCapacity = true;
							// have all lines above already reached max capacity
							foreach(FluidLine aboveLine in activeFluidLineStacks[i].Peek().GetAboveLines ())
							{
								if(aboveLine.GetFlowCapacity(UpdateID) > 0)
								{
									noCapacity = false;
									break;
								}
							}

							if(noCapacity)
							{
								topCandidates.Add (activeFluidLineStacks[i]);
							}
							else
							{
								foreach(FluidLine aboveLine in activeFluidLineStacks[i].Peek().GetAboveLines())
								{
									if(aboveLine.GetFlowCapacity (UpdateID) > 0)
									{
										fluidLineStack = activeFluidLineStacks[i].Clone();
										fluidLineStack.Push (aboveLine);
										newActiveFluidLineStacks.Add (fluidLineStack);
									}
								}
							}
						}
					}
				}

				// no topCandidate found
				if(topCandidates.Count == 0)
					return false;

				// find the nearest top level FluidLine
				int minimalDistance = int.MaxValue;
				int currentDistance;
				int nearestFluidLineStackIndex = -1;

				for(int i = 0; i < topCandidates.Count; i++)
				{
					currentDistance = Mathf.Abs (topCandidates[i].Peek().x - flowingToXPosition);
					if(currentDistance < minimalDistance)
					{
						nearestFluidLineStackIndex = i;
						minimalDistance = currentDistance;
					}

					currentDistance = Mathf.Abs (topCandidates[i].Peek().x + topCandidates[i].Peek().length - 1 - flowingToXPosition);
					if(currentDistance < minimalDistance)
					{
						nearestFluidLineStackIndex = i;
						minimalDistance = currentDistance;
					}
				}

				// remove a drop from the chosen top level FluidLine and adjust 
				// the remaining FlowCapacity of all participating FluidLines
				if(RemoveDrop (topCandidates[nearestFluidLineStackIndex].Pop (), flowingToXPosition))
				{
					foreach(FluidLine flowPathFluidLine in topCandidates[nearestFluidLineStackIndex])
					{
						flowPathFluidLine.DecrementCapacity(UpdateID);
					}

					return true;
				}
				else
				{
					return false;
				}
			}


			// returns all positions where a drop can be taken from this line in its current state.
			private IList<int> FindMostFreePositionsIn(FluidLine fluidLine)
			{
				if(fluidLine.length > 0)
				{

					bool[] connectedAbove = new bool[fluidLine.length];
					bool[] connectedBelow = new bool[fluidLine.length];

					for(int i = 0; i < fluidLine.length; i++)
					{
						foreach(FluidLine aboveLine in fluidLine.GetAboveLines ())
						{
							if(aboveLine.Contains (fluidLine.x + i))
								connectedAbove[i] = true;
						}

						foreach(FluidLine belowLine in fluidLine.GetBelowLines ())
						{
							if(belowLine.Contains (fluidLine.x + i))
								connectedBelow[i] = true;
						}

						if(!connectedBelow[i])
						{
							if(TerrainManager.instance.GetBlock (((float)(fluidLine.x + i) + 0.5f) * DataManager.instance.BlockSize,
							                                     ((float)(fluidLine.y - 1) + 0.5f) * DataManager.instance.BlockSize).Passable())
							{
								connectedBelow[i] = true;
							}
						}
					}

					List<int> mostFreePositions = new List<int>();

					// are there unconnected Drops?
					for(int i = 0; i < fluidLine.length; i++)
					{
						if(!connectedAbove[i] && !connectedBelow[i])
							mostFreePositions.Add (i);
					}

					if(mostFreePositions.Count > 0)
					{
						return mostFreePositions;
					}
					// are there Drops not connected above?
					for(int i = 0; i < fluidLine.length; i++)
					{
						if(!connectedAbove[i] && connectedBelow[i])
							mostFreePositions.Add (i);
					}
					
					if(mostFreePositions.Count > 0)
					{
						return mostFreePositions;
					}

					// are there Drops not connected below?
					for(int i = 0; i < fluidLine.length; i++)
					{
						if(!connectedBelow[i] && connectedAbove[i])
							mostFreePositions.Add (i);
					}
					
					if(mostFreePositions.Count > 0)
					{
						return mostFreePositions;
					}
					
					for(int i = 0; i < fluidLine.length; i++)
						mostFreePositions.Add (i);

					return mostFreePositions;
				}
				else
				{
					Debug.Log("[ERROR] Trying to find a free drop in an empty line!");
					return null;
				}
			}

			private bool RemoveDrop(FluidLine fluidLine, int flowingToPosition)
			{
				if(fluidLine.length == 0)
				{
					Debug.Log ("[ERROR]RemoveTop(): Couldnt remove a drop from the top FluidLine.");
					return false;
				}
				else if(fluidLine.length == 1)
				{
					//Debug.Log ("Removing drop at " + fluidLine.ToString ());
					DeleteFluidLine(fluidLine);
					return true;
				}
				else
				{
					IList<int> mostFreePositions = FindMostFreePositionsIn(fluidLine);
					//Debug.Log("Most Free positions = " + mostFreePositions.ToArray ().ElementsToString ());

					if(mostFreePositions.Count == 1)
					{
						return RemoveDropAtPosition (fluidLine, mostFreePositions.ElementAt (0));
					}
					else
					{
						List<int> removeCandidates = new List<int>();
						int maxSinkDistance = 0;

						// select the drops that are furthest away from their sink lines
						for(int i = 0; i < mostFreePositions.Count; i++)
						{
							if(fluidLine.SinkDistanceAt(mostFreePositions[i]) > maxSinkDistance)
							{
								removeCandidates.Clear ();
								removeCandidates.Add (mostFreePositions[i]);
								maxSinkDistance = fluidLine.SinkDistanceAt(mostFreePositions[i]);
							}
							else if(fluidLine.SinkDistanceAt(mostFreePositions[i]) == maxSinkDistance)
							{
								removeCandidates.Add (mostFreePositions[i]);
							}
						}

						// chose the closest one of those
						int distance;
						int maxDistance = 0;
						int maxDistancePosition = 0;

						foreach(int position in removeCandidates)
						{
							distance = Mathf.Abs (position - (flowingToPosition - fluidLine.x));
							if(maxDistance <= distance)
							{
								maxDistance = distance;
								maxDistancePosition = position;
							}
						}

						//Debug.Log ("Removing drop in " + fluidLine.ToString () + " at position " + minDistancePosition);
						return RemoveDropAtPosition (fluidLine, maxDistancePosition);
					}
				}
			}

			private bool RemoveDropAtPosition(FluidLine fluidLine, int removePosition)
			{
				if(fluidLine.length <= 0)
				{
					Debug.Log ("[ERROR] Can't remove from an empty FluidLine.");
					return false;
				}

				if(fluidLine.length == 1)
				{
					// nothing to do 
					DeleteFluidLine(fluidLine);
					return true;
				}

				if(removePosition >= 0 && removePosition < fluidLine.length)
				{
					if(removePosition == 0)
					{
						// remove left
						fluidLine.RemoveDropLeft ();
						return true;

					}
					else if(removePosition == fluidLine.length - 1)
					{
						// remove right
						fluidLine.RemoveDropRight ();
						return true;
					}
					else
					{
						// remove in the middle. The two lines left and right exist 
						// because we arent removing the left or right end. 

						FluidLine rightFluidLine = fluidLine.Split (removePosition, UpdateID);
						RegisterFluidLine (rightFluidLine);
						if(rightFluidLine.length == 0)
							Debug.Log ("Adding empty FluidLine: " + rightFluidLine.ToString ());
						activeLines.Add (rightFluidLine);

						return true;
					}
				}
				else
				{
					Debug.Log ("[ERROR] Remove position not inside the fluidLine.");
					return false;
				}
			}
			
			public void DebugDrawFluidTree(bool showTree, bool showLineLength)
			{				
				if(activeLines.Count > 0)
				{
					foreach(FluidLine bottomFluidLine in GetFluidLines())
					{
						
						HashSet<FluidLine> currentFluidLines = new HashSet<FluidLine>();
						FluidLine[] currentFluidLinesArray;
						currentFluidLines.Add (bottomFluidLine);
						bool openEnds = true;
						
						// find all topLevel FluidLines = all lines with no lines above them and connected
						while(openEnds)
						{
							openEnds = false;
							currentFluidLinesArray = new FluidLine[currentFluidLines.Count];
							currentFluidLines.CopyTo (currentFluidLinesArray);
							currentFluidLines.Clear ();
							
							for(int i = 0; i < currentFluidLinesArray.Length; i++)
							{
								if(currentFluidLinesArray[i].GetAboveLinesCount() > 0)
								{
									foreach(FluidLine aboveLine in currentFluidLinesArray[i].GetAboveLines())
									{
										if(showTree)
										{
											Debug.DrawLine (new Vector3(((float)(currentFluidLinesArray[i].x) + 0.5f) * DataManager.instance.BlockSize, 
											                            ((float)(currentFluidLinesArray[i].y) + 0.5f) * DataManager.instance.BlockSize,
											                            -2f),
											                new Vector3(((float)(aboveLine.x) + 0.5f) * DataManager.instance.BlockSize, 
											            ((float)(aboveLine.y) + 0.5f) * DataManager.instance.BlockSize,
											            -2f),
											                Color.red,
											                0.1f);
										}

										if(showLineLength)
										{
											Debug.DrawLine (new Vector3(((float)(currentFluidLinesArray[i].x) + 0.5f) * DataManager.instance.BlockSize, 
											                            ((float)(currentFluidLinesArray[i].y) + 0.5f) * DataManager.instance.BlockSize,
											                            -2f),
											                new Vector3 (((float)(currentFluidLinesArray[i].x + currentFluidLinesArray[i].length) + 0.5f) * DataManager.instance.BlockSize, 
											             ((float)(currentFluidLinesArray[i].y) + 0.5f) * DataManager.instance.BlockSize,
											             -2f),
											                Color.green,
											                0.1f);
										}

										if(aboveLine.GetAboveLinesCount() > 0)
										{
											currentFluidLines.Add (aboveLine);
											openEnds = true;
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
}