//#define DEBUG

using UnityEngine;
using System.Collections.Generic;

namespace Terrain
{
	namespace Fluid
	{

		public class Fluid : MonoBehaviour 
		{
			// depth value of the quads
			public float zValue;

			public Color fluidColor;
			
			public FluidPart fluidPartPrefab;

			public int xMapPartCount;
			public int yMapPartCount;

			private List<FluidLine> fluidLines;
			private List<FluidLine> changedFluidLines = new List<FluidLine>();

			// cached Components
			private MeshFilter cachedMeshFilter;
			private Transform cachedTransform;

			// collider parts:
			private Dictionary<FluidPartKey, FluidPart> fluidPartsByCoords = new Dictionary<FluidPartKey, FluidPart>();
			private Dictionary<FluidLine, FluidPart> fluidPartsByLines = new Dictionary<FluidLine, FluidPart>();

			// Mesh data
			private List<Vector3> vertices = new List<Vector3>();
			private List<Color> colors = new List<Color>();
			private List<int> triangles = new List<int>();
			
			// number of quads in the mesh
			private int quadCount = 0;
			private float blockSize;
			private float mapPartSize;

			private bool fluidChanged = false;

			void Awake()
			{
				cachedTransform = GetComponent(typeof(Transform)) as Transform;
				cachedMeshFilter = GetComponent (typeof(MeshFilter)) as MeshFilter;
			}

			void Start()
			{
				blockSize = DataManager.instance.BlockSize;
				mapPartSize = DataManager.instance.MapPartSize;

				FluidPart fluidPart;
				Vector3 worldPosition;

				for(int i = -xMapPartCount; i < xMapPartCount; i++)
				{
					for(int j = -yMapPartCount; j < yMapPartCount; j++)
					{
						worldPosition = new Vector3((float)(i * mapPartSize), (float)(j * mapPartSize));
						fluidPart = fluidPartPrefab.Spawn (cachedTransform, worldPosition);
						fluidPart.gameObject.layer = LayerMask.NameToLayer ("Fluids");
						
						fluidPartsByCoords.Add (new FluidPartKey(i, j), fluidPart);
					}
				}
			}

			void Update ()
			{
				if(fluidChanged)
				{
					ProcessFluidLines();
					UpdateMesh ();
					
					//BuildCollider();
					//Debug.Log ("FluidLines: " + fluidLines.Count);

					fluidChanged = false;
				}
			}

			// change
			public void ShowFluidUpdate(ICollection<FluidLine> fluidLines, ICollection<FluidLine> deletedLines)
			{
				this.fluidLines = new List<FluidLine>(fluidLines);
				FreeDeletedLines (deletedLines);
				//Debug.Log ("Deleted lines = " + deletedLines.Count.ToString ());
				fluidChanged = true;
			}

			private FluidPart GetFluidPart(FluidLine fluidLine)
			{
				FluidPart fluidPart;

				if(fluidPartsByLines.TryGetValue (fluidLine, out fluidPart))
				{
					return fluidPart;
				}
				else
				{
					float xf = (float)fluidLine.x * blockSize;
					float yf = (float)fluidLine.y * blockSize;

					fluidPart = GetFluidPart (xf, yf);
					fluidPartsByLines.Add (fluidLine, fluidPart);

					return fluidPart;
				}
			}

			private FluidPart GetFluidPart(float x, float y)
			{
				int i = Mathf.FloorToInt(x / mapPartSize);
				int j = Mathf.FloorToInt(y / mapPartSize);
				
				return GetFluidPart(i,j);
			}

			private FluidPart GetFluidPart(int x, int y)
			{
				FluidPart fluidPart;
				
				if(fluidPartsByCoords.TryGetValue (new FluidPartKey(x,y), out fluidPart))
					return fluidPart;
				else 
					return null;
			}

			private void FreeDeletedLines(ICollection<FluidLine> deletedLines)
			{
				FluidPart fluidPart;

				foreach(FluidLine fluidLine in deletedLines)
				{
					fluidPart = GetFluidPart(fluidLine);
					if(fluidPart != null)
						fluidPart.FreeFluidLine(fluidLine);
					else
						Debug.Log ("[ERROR] Couldnt find the FluidPart to remove " + fluidLine.ToString ());
				}
			}

			private void ProcessFluidLines()
			{
				changedFluidLines.Clear ();

				int emptyFluidLines = 0;

				foreach(FluidLine fluidLine in fluidLines)
				{
					if(fluidLine.length == 0)
						emptyFluidLines++;

					if(fluidLine.Changed)
					{
						//Debug.Log ("[DEBUG] Registered change in FluidLine " + fluidLine.ToString () );
						changedFluidLines.Add (fluidLine);
						fluidLine.ChangeProcessed ();
					}
					
					if(fluidLine.length == 0)
						Debug.Log ("Empty line detected");
					
					AddQuad (fluidLine.x, fluidLine.y, fluidLine.length);
				}

				if(emptyFluidLines > 0)
					Debug.Log ("Empty fluid Lines = " + emptyFluidLines.ToString ());

				FluidPart fluidPart;

				foreach(FluidLine fluidLine in changedFluidLines)
				{
					fluidPart = GetFluidPart (fluidLine);
					if(fluidPart != null)
						fluidPart.UpdateFluidLineCollider(fluidLine);
				}

			}

			private void UpdateMesh()
			{
				Mesh mesh = cachedMeshFilter.mesh;
				mesh.Clear();
				
				mesh.vertices = vertices.ToArray ();
				mesh.colors = colors.ToArray ();
				mesh.triangles = triangles.ToArray ();
				
				mesh.Optimize ();
				mesh.RecalculateNormals ();
				
				quadCount = 0;
				
				vertices.Clear ();
				colors.Clear ();
				triangles.Clear ();
			}

			private Vector2[] FluidLineToPath(FluidLine fluidLine)
			{
				Vector2[] path = new Vector2[4];

				float xf = (float)fluidLine.x * blockSize;
				float yf = (float)fluidLine.y * blockSize;
				float widthf = (float)fluidLine.length * blockSize;

				path[0] = new Vector2(xf, yf);
				path[1] = new Vector2(xf, yf + blockSize);
				path[2] = new Vector2(xf + widthf, yf + blockSize);
				path[3] = new Vector2(xf + widthf, yf);

				return path;
			}

			private void AddQuad(int x, int y, int width)
			{
				float xf = (float)x * blockSize;
				float yf = (float)y * blockSize;
				float widthf = (float)width * blockSize;

				AddQuad (xf,yf, widthf);
			}

			private void AddQuad(float xPosition, float yPosition, float width)
			{				
				vertices.Add (new Vector3(xPosition, yPosition, zValue));
				vertices.Add (new Vector3(xPosition, yPosition + blockSize, zValue));
				vertices.Add (new Vector3(xPosition + width, yPosition + blockSize, zValue));
				vertices.Add (new Vector3(xPosition + width, yPosition, zValue));
				
				colors.Add (fluidColor);
				colors.Add (fluidColor);
				colors.Add (fluidColor);
				colors.Add (fluidColor);
				
				triangles.Add (4 * quadCount);
				triangles.Add (4 * quadCount + 1);
				triangles.Add (4 * quadCount + 2);
				triangles.Add (4 * quadCount);
				triangles.Add (4 * quadCount + 2);
				triangles.Add (4 * quadCount + 3);
				
				quadCount++;
			}

			private class FluidPartKey
			{
				public readonly int x;
				public readonly int y;
				
				public FluidPartKey() : this(0, 0) {}
				public FluidPartKey(int x, int y)
				{
					this.x = x;
					this.y = y;
				}
				
				public override bool Equals (object obj)
				{
					return Equals ((FluidPartKey)obj);
				}
				
				public bool Equals(FluidPartKey fluidPartKey)
				{
					if(fluidPartKey == null)
						return false;
					
					if(fluidPartKey.x == this.x && fluidPartKey.y == this.y)
						return true;
					else
						return false;
				}
				
				public override int GetHashCode ()
				{
					return ((17 * 31) + x) * 31 + y;
				}
			}
		}
	}
}
