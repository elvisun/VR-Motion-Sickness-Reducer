using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class fading : MonoBehaviour {

	public static fading Instance {set;get;}

	public Image fadeImage;
	private bool isInTransition;
	private float transition;
	private bool isShowing;
	private float duration;
	private float timer;
	public float secondsToWait = 3f;
	private bool fadeBool;

	private void Awake(){
		Instance = this;
	}

	private void Start(){
		Fade (false, 5.0f);
		timer = 0f;
		secondsToWait = 5f;
		fadeBool = false;
	}

	public void Fade(bool showing, float duration){
		isShowing = showing;
		isInTransition = true;
		this.duration = duration;
		transition = (isShowing) ? 0 : 1;
	}

	private void Update(){
		if (timer < secondsToWait) {
			timer += Time.deltaTime;
		} else {
			fadeBool = true;
		}
		if (fadeBool) {
			if (!isInTransition) {			// if not in transition
				return;
			} else {
				transition += (isShowing) ? Time.deltaTime * (1 / duration) : -Time.deltaTime * (1 / duration);
				fadeImage.color = Color.Lerp (new Color (0, 0, 0, 0), Color.black, transition);

				if (transition > 1 || transition < 0)
					isInTransition = false;
			}
		}

	}
}
