using UnityEngine;
using System.Collections;

public class TreePool : MonoBehaviour {

	static int numTrees = 3000;
	public GameObject treePrefab;
	public GameObject treePrefab2;
	public GameObject treePrefab3;
	public GameObject treePrefab4;
	static GameObject[] trees;

	// Use this for initialization
	void Start () {
		trees = new GameObject[numTrees];
		for (int i = 0; i < numTrees; i++) {
			int r = 0;
			r = Random.Range (0, 3);
			if (r == 0)
				trees [i] = (GameObject) Instantiate (treePrefab, Vector3.zero, Quaternion.identity);
			else if (r ==1)
				trees [i] = (GameObject) Instantiate (treePrefab2, Vector3.zero, Quaternion.identity);
			else if (r==2)
				trees [i] = (GameObject) Instantiate (treePrefab3, Vector3.zero, Quaternion.identity);
			else 
				trees [i] = (GameObject) Instantiate (treePrefab4, Vector3.zero, Quaternion.identity);
			//trees [i] = (GameObject) Instantiate (treePrefab, Vector3.zero, Quaternion.identity);
			trees [i].SetActive(false);
		}
	}

	static public GameObject getTree(){
		for (int i = 0; i < numTrees; i++) {
			if (!trees [i].activeSelf) {
				return trees [i];
			}
		}
		return null;
	}

	// Update is called once per frame
	void Update () {
	
	}
}
