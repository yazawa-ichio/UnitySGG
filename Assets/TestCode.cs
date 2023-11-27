using System;
using UnityEngine;
using UnitySGGTest;

[FastEnum]
public enum TestEnum
{
	A,
	B,
	C,
	D,
	E,
	F,
	G,
	H,
	I,
}

public class TestCode : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		foreach (TestEnum e in Enum.GetValues(typeof(TestEnum)))
		{
			Debug.Log(e.ToString() + ":" + e.ToFastString());
		}

	}

}
