//Keyboard Model and manual scripts made by Yurika Mulase using the Midi plugin "MidiJack" by Keijiro Takahashi.
//Made for Mac OS and Unity ver. 5.3.2

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MidiJack;

public class Keyboard : MonoBehaviour {

	const float unitLineLength = 0.008f;
	const float lineWidth = 0.02f;
	const float updateDuration = 0.05f;
	const int maxCachedPositions = 100;
	const float positionTimeToLive = 2f;

	public GameObject KeyboardParent;
	GameObject keyboard;
	public List<GameObject> keys;

	public GameObject blackKey;
	Vector3 blackKeyScale = new Vector3(0.8f, 0.7f, 6f);

	public GameObject whiteKey;
	Vector3 whiteKeyScale = new Vector3 (1.25f, 1f, 10f);

	Vector3 keyPosition = new Vector3(-44.5f, 4f, 0f);

	Vector3 whiteToBlackDist = new Vector3(0.75f, 1f, 1.82f);
	Vector3 blackToWhiteDist = new Vector3(0.75f, -1f, -1.82f);
	Vector3 whiteToWhiteDist = new Vector3(1.5f, 0f, 0f);

	private static bool finishedSetup = false;
	private static bool modified = false;

	private List<float> keyStartPos = new List<float>();
	private List<float> keyPressPos = new List<float>();
	private List<float> keyVelocities = new List<float>();
	private List<List<MidiLine>> lines;
	private List<MidiLine> tempLineRef;
	private List<Vector3> temp = new List<Vector3>();
	private Vector3 tempVec;

	private GameObject currKey;
	static GameObject container;

	protected static float lastUpdated = -1f;

	public class Position {
		public float timeSpawned;
		public Vector3 rawPosition;
		public bool remove;

		public Position(float x, float y, float z) {
			rawPosition = new Vector3(x, y, z);
			timeSpawned = Time.time;
			remove = false;
		}

		public void Set(float x, float y, float z) {
			rawPosition = new Vector3(x, y, z);
		}
	}

	static bool willRemove(Position p) {
		return p.remove;
	}

	static bool willRemove(MidiLine m) {
		return m.remove;
	}

	public class MidiLine {
		public GameObject lineContainer;
		public LineRenderer line;
		public List<Position> cachedPositions;
		public bool isPressed;
		public bool remove = false;

		public MidiLine(string keyname) {
			isPressed = true;
			lineContainer = new GameObject("lineContainer");
			lineContainer.transform.SetParent(container.transform);

			line = lineContainer.AddComponent<LineRenderer>();
			cachedPositions = new List<Position>();

			Color c = new Color(0f, 0f, 0f);
			switch (keyname.ToCharArray()[0]) {
				case 'C':
				if (keyname.Contains("#")) {
					line.SetColors(Color.blue, Color.blue);
				} else {
					line.SetColors(Color.red, Color.red);
				}
				break;
				case 'D':
				if (keyname.Contains("#")) {
					c.r = 0.3529f;
					c.g = 0.0392f;
					c.b = 1f;
					line.SetColors(c, c); // 90, 10, 255, Purple
				} else {
					c.r = 1f;
					c.g = 0.6863f;
					c.b = 0.43137f;
					line.SetColors(Color.yellow, c); // 255, 175, 110, Orange
				}
				break;
				case 'E':
					c.r = 0.4314f;
					c.g = 1f;
					c.b = 0.4314f;
					line.SetColors(c, c); // 110, 255, 110, Light Green
				break;
				case 'F': 
				if (keyname.Contains("#")) {
					line.SetColors(Color.cyan, Color.cyan);
				} else {
					c.r = 1f;
					c.g = 0.4314f;
					c.b = 0.5882f;
					line.SetColors(c,c);
				}
				break;
				case 'G': 
				if (keyname.Contains("#")) {
					c.r = 0.3922f;
					c.g = 0.0392f;
					c.b = 1f;
					line.SetColors(Color.blue, c); // 100, 10, 255, purple
				} else {
					c.r = 1f;
					c.g = 0.4902f;
					c.b = 0.0392f;
					line.SetColors(c, c);
				}
				break;
				case 'A':
				if (keyname.Contains("#")) {
					line.SetColors(Color.magenta, Color.magenta);
				} else {
					line.SetColors(Color.yellow, Color.yellow);
				}
				break;
				case 'B': {
					line.SetColors(Color.green, Color.green);
				}
				break;
				default:
					Debug.Log("Invalid key: " + keyname + ", setting to white");
					line.SetColors(Color.white, Color.white);
					break;
			}
			line.material = new Material(Shader.Find("Particles/Additive"));
			line.SetWidth(lineWidth, lineWidth);
		}

		public void addStartAndEndPoints(Vector3 vec) {
			cachedPositions.Add (new Position(vec.x, vec.y + unitLineLength, vec.z));
			cachedPositions.Add (new Position(vec.x, vec.y, vec.z));
		}

		public void extendLine(Vector3 vec) {
			if (Time.time - lastUpdated > updateDuration) {
				updateCachedPositions ();
				cachedPositions.Add (new Position (vec.x, vec.y, vec.z));
				modified = true;
			}
		}

		public void updateCachedPositions() {
			for (int i = 0; i < cachedPositions.Count; ++i) {
				Vector3 orig = cachedPositions [i].rawPosition;
				
				cachedPositions [i].Set(
					orig.x,			 
					orig.y + unitLineLength,
					orig.z
				);

				if (Time.time - cachedPositions [i].timeSpawned > positionTimeToLive) {
					cachedPositions [i].remove = true;
				}
			}
			cachedPositions.RemoveAll (new System.Predicate<Position> (willRemove));
		}
	}

	void setupKeyboard() {
		keyboard = new GameObject ("Keyboard");
		keyboard.transform.SetParent (KeyboardParent.transform);
		keyboard.transform.localPosition = Vector3.zero;
		keyboard.transform.localScale = new Vector3 (0.0158f, 0.02f, 0.016f);
		keyboard.transform.rotation = new Quaternion (0f, 0f, 0f, 0f);

		createKeys ();
		setKeyProperties ();

		finishedSetup = true;
	}

	void createKeys() {
		createKey ("A0");
		createKey ("A#0");
		createKey ("B0");

		for (int i = 1; i < 8; ++i) {
			createKey ("C" + i);
			createKey ("C#" + i);
			createKey ("D" + i);
			createKey ("D#" + i);
			createKey ("E" + i);
			createKey ("F" + i);
			createKey ("F#" + i);
			createKey ("G" + i);
			createKey ("G#" + i);
			createKey ("A" + i);
			createKey ("A#" + i);
			createKey ("B" + i);
		}

		createKey ("C" + 8);
	}

	void setKeyProperties() {
		GameObject prevKey;
		currKey = keys [0];
		currKey.transform.localScale = whiteKeyScale;
		currKey.transform.localPosition = keyPosition;
		currKey.transform.rotation = new Quaternion (0, 0, 0, 0);
		for (int i = 1; i < keys.Count; ++i) {
			currKey = keys [i];
			prevKey = keys [i - 1];
			currKey.transform.localScale = currKey.name.Contains ("#") ? blackKeyScale : whiteKeyScale;
			keyPosition += prevKey.name.Contains ("#") ? blackToWhiteDist : 
				           (currKey.name.Contains ("#") ? whiteToBlackDist : whiteToWhiteDist);
			currKey.transform.localPosition = new Vector3(keyPosition.x , keyPosition.y, keyPosition.z);
			currKey.transform.rotation = new Quaternion (0, 0, 0, 0);

		}
	}

	void createKey(string keyname) {
		GameObject key = new GameObject (keyname);
		GameObject type = keyname.Contains("#") ? blackKey : whiteKey;
		key.transform.SetParent (keyboard.transform);
		Instantiate (type, Vector3.zero, Quaternion.identity, key.transform);
		keys.Add(key);
		keyVelocities.Add (0f);

		keyStartPos.Add(key.transform.position.y);
		keyPressPos.Add(key.transform.position.y - 0.015f);

		lines.Add(new List<MidiLine>());
	}

	void Start (){

		keys = new List<GameObject> ();
		lines = new List<List<MidiLine>> ();
		container = new GameObject ("linesContainer");
		currKey = null;

		setupKeyboard ();

	}

	void handleKeyPressUpdate(int keyIndex) {
		tempLineRef = lines [keyIndex];
		int numLines = tempLineRef.Count;
		if (numLines > 0 && tempLineRef [numLines - 1].isPressed) {
			//Update Line draw
			tempVec = keys[keyIndex].transform.position;
			tempLineRef [numLines - 1].extendLine (tempVec);
		} else {
			//Creates new line at start of press
			tempLineRef.Add (new MidiLine (keys[keyIndex].name));
			tempVec = keys [keyIndex].transform.position;
			tempLineRef [numLines].addStartAndEndPoints (tempVec);
		}
	}

	void handleKeyUpUpdate(int keyIndex) {
		int numLines = lines [keyIndex].Count;
		if (numLines > 0 && lines [keyIndex] [numLines - 1].isPressed) {
			lines [keyIndex] [numLines - 1].isPressed = false;
		}
	}

	void handleKeyUpdate() {
		foreach (List<MidiLine> midilines in lines) {
			foreach (MidiLine midiline in midilines) {
				midiline.updateCachedPositions ();
				midiline.line.SetVertexCount (midiline.cachedPositions.Count);
				foreach (Position p in midiline.cachedPositions) {
					Vector3 c;
					if (Time.time - p.timeSpawned < 1f) {
						c = p.rawPosition;
					} else {
						c = new Vector3 (
							Mathf.Sin (2 * p.rawPosition.x * Mathf.PI / 3f) * -1 - 2f,
							Mathf.Sin (p.rawPosition.y * Time.time * Mathf.PI / 3f) + 0.75f,
							Mathf.Sin (Mathf.PI * Time.time * (p.rawPosition.x + p.rawPosition.y) / 3f) + 0.5f);
					}
					temp.Add(c);
				}
				midiline.line.SetPositions (temp.ToArray());
				temp.Clear ();
				if (midiline.cachedPositions.Count == 0) {
					Destroy (midiline.lineContainer);
					midiline.remove = true;
				}
			}
			midilines.RemoveAll (new System.Predicate<MidiLine> (willRemove));
		}
	}

	// Update is called once per frame
	void Update () {

		if (!finishedSetup) {
			return;
		}

		modified = false;

		for (int i = 0; i < keys.Count; i++) {
			keyVelocities[i] = (MidiMaster.GetKey (i + 21));
			currKey = keys[i];

			if (keyVelocities [i] > 0) {
				//animate key press
				currKey.transform.position.Set(currKey.transform.position.x, keyPressPos [i], currKey.transform.position.z);

				//do something while key is pressed
				handleKeyPressUpdate(i);

			} else {
				//Reset key to original position
				currKey.transform.position.Set(currKey.transform.position.x, keyStartPos [i], currKey.transform.position.z);

				//do something while key is up
				handleKeyUpUpdate(i);
            }
		}

		if (modified) {
			lastUpdated = Time.time;
		}
		handleKeyUpdate ();
		currKey = null;
	}
}
