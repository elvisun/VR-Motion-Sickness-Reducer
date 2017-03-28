using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GenerateTerrain : MonoBehaviour {

	int heightScale = 1;
	float detailScale = 5.0f;
	List<GameObject> myTrees = new List<GameObject> ();

	// Use this for initialization
	void Start () {
		Mesh mesh = this.GetComponent<MeshFilter>().mesh;
		Vector3[] vertices = mesh.vertices;
		for (int v = 0; v < vertices.Length; v++) {
			vertices [v].y = Mathf.PerlinNoise ((vertices [v].x + this.transform.position.x) / detailScale,
				(vertices [v].z + this.transform.position.z) / detailScale) * heightScale;

			if (vertices[v].y < heightScale * 0.2 && Random.Range(0,100) < 5) {
				GameObject newTree = TreePool.getTree ();
				if (newTree != null) {
					Vector3 treePos = new Vector3 (vertices [v].x + this.transform.position.x, vertices [v].y - 0.5f, vertices [v].z + this.transform.position.z);
																							// lower tree into ground

					newTree.transform.position = treePos;
					newTree.SetActive (true);
					myTrees.Add (newTree);
				}
			}

		}
		mesh.vertices = vertices;
		mesh.RecalculateBounds ();
		mesh.RecalculateNormals ();
		this.gameObject.AddComponent<MeshCollider> ();
	}

	void OnDestroy(){
		for (int i = 0; i < myTrees.Count; i++) {
			if (myTrees [i] != null)
				myTrees [i].SetActive (false);
		}
		myTrees.Clear ();
	}

	// Update is called once per frame
	void Update () {
	
	}
}
