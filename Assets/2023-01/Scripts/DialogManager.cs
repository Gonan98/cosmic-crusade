using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogManager : MonoBehaviour
{
	public static DialogManager Instance = null;
	public GameObject content;
	public GameObject create;
	public GameObject join;
	public GameObject search;

	public void Awake()
	{
		if (Instance == null)
			Instance = this;
	}

	public void OpenCreate()
	{
		content.SetActive(true);
		create.SetActive(true);
	}

	public void OpenJoin()
	{
		content.SetActive(true);
		join.SetActive(true);
	}

	public void OpenSearch()
	{
		content.SetActive(true);
		search.SetActive(true);
		PhotonNetwork.JoinLobby();
	}

	public void Close()
	{
		content.SetActive(false);
		create.SetActive(false);
		join.SetActive(false);
		search.SetActive(false);

	}
}
