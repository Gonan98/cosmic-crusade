using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
	public void PointerEnter()
	{
		//transform.localScale = new Vector2(1.15f, 1.15f);
		LeanTween.scale(transform.gameObject, new Vector2(1.15f, 1.15f), .5f);
	}

	public void PointerExit()
	{
		LeanTween.scale(transform.gameObject, new Vector2(1f, 1f), .5f);
	}

}
