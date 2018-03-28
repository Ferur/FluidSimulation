using UnityEngine;
using System.Collections.Generic;

namespace Terrain
{
	namespace Fluid
	{
		[RequireComponent(typeof(PolygonCollider2D))]
		public class FluidPart : MonoBehaviour 
		{
			private PolygonCollider2D cachedPolygonCollider2D;
			private Transform cachedTransform;

			private SortedList<int> freeIndices = new SortedList<int>();
			private int pathCount = 0;
			private Dictionary<FluidLine, int> fluidLineToPathIndexDictionary = new Dictionary<FluidLine, int>();
			private float blockSize;

			void Awake()
			{
				cachedTransform = GetComponent(typeof(Transform)) as Transform;
				cachedPolygonCollider2D = GetComponent (typeof(PolygonCollider2D)) as PolygonCollider2D;
				cachedPolygonCollider2D.pathCount = pathCount;
			}

			void Start()
			{
				blockSize = DataManager.instance.BlockSize;
			}

			public void FreeFluidLine(FluidLine fluidLine)
			{
				int index;

				//Debug.Log ("[DEBUG] Deleting Path of FluidLine = " + fluidLine.ToString ());

				if(fluidLineToPathIndexDictionary.TryGetValue(fluidLine, out index))
				{
					// the path with index 0 always needs a non empty path or the PolygonCollider2D wont show 
					// -> submit feature request to unity
					if(index == 0)
					{
						// find the first index that doesnt have an empty path
						int replacementIndex = -1;
						for(int i = 1; i < cachedPolygonCollider2D.pathCount; i++)
						{
							if(!freeIndices.Contains (i))
							{
								replacementIndex = i;
								break;
							}
						}

						bool replacemenetSuccessful = false;

						if(replacementIndex > 0)
						{
							// find the corresponding FluidLine
							FluidLine replacementFluidLine = null;
							foreach(KeyValuePair<FluidLine, int> keyValuePair in fluidLineToPathIndexDictionary)
							{
								if(keyValuePair.Value == replacementIndex)
								{
									replacementFluidLine = keyValuePair.Key;
									break;
								}
							}

							if(replacementFluidLine != null)
							{

								cachedPolygonCollider2D.SetPath (0, cachedPolygonCollider2D.GetPath (replacementIndex));
								cachedPolygonCollider2D.SetPath (replacementIndex, null);

								// remove the fluidLine from the dictionary
								fluidLineToPathIndexDictionary.Remove(fluidLine);
								// free the replacement index
								freeIndices.Add (replacementIndex);
								// remove the replacementFluidLine from its old position
								fluidLineToPathIndexDictionary.Remove (replacementFluidLine);
								// insert it with index 0
								fluidLineToPathIndexDictionary.Add (replacementFluidLine, 0);

								replacemenetSuccessful = true;
							}
						}

						if(!replacemenetSuccessful)
						{
							// no other FluidLine to replace Left means there are no more FluidLines in this FluidPart
							fluidLineToPathIndexDictionary.Clear ();
							freeIndices.Clear ();
							this.pathCount = 0;
							cachedPolygonCollider2D.pathCount = pathCount;
						}
					}
					else
					{
						freeIndices.Add (index);
						fluidLineToPathIndexDictionary.Remove (fluidLine);
						cachedPolygonCollider2D.SetPath (index, null);
					}
				}
				else
				{
					Debug.Log ("Deleted FluidLine didnt have a collider. Check for bug or explanation! FluidLine = " + fluidLine.ToString ());
				}
			}

			public void UpdateFluidLineCollider(FluidLine fluidLine)
			{
				int index;

				//Debug.Log ("[DEBUG] Updating Collider of fluidLine = " + fluidLine.ToString ());

				// does the fluidLine already has a collider?
				if(fluidLineToPathIndexDictionary.TryGetValue(fluidLine, out index))
				{
					cachedPolygonCollider2D.SetPath (index, FluidLineToPath (fluidLine));
				}
				else
				{
					if(freeIndices.Count > 0)
					{
						index = freeIndices[0];
						freeIndices.RemoveAt (0);
						
						cachedPolygonCollider2D.SetPath (index, FluidLineToPath (fluidLine));
						fluidLineToPathIndexDictionary.Add (fluidLine, index);
					}
					else
					{
						pathCount++;
						cachedPolygonCollider2D.pathCount = pathCount;
						
						cachedPolygonCollider2D.SetPath (pathCount - 1, FluidLineToPath(fluidLine));
						fluidLineToPathIndexDictionary.Add (fluidLine, pathCount - 1);
					}
				}
			}

			private Vector2[] FluidLineToPath(FluidLine fluidLine)
			{
				Vector2[] path = new Vector2[4];
				
				float xf = (float)fluidLine.x * blockSize - cachedTransform.position.x;
				float yf = (float)fluidLine.y * blockSize - cachedTransform.position.y;
				float widthf = (float)fluidLine.length * blockSize;
				
				path[0] = new Vector2(xf, yf);
				path[1] = new Vector2(xf, yf + blockSize);
				path[2] = new Vector2(xf + widthf, yf + blockSize);
				path[3] = new Vector2(xf + widthf, yf);
				
				return path;
			}
		}

	}
}
