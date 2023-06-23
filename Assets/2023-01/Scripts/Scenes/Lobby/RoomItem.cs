using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomItem : MonoBehaviourPunCallbacks
{
	public Text name;
	public Text players;

	private RoomInfo info;

	public void SetUp(RoomInfo _info)
	{
		info = _info;
		name.text = _info.Name;
		players.text = _info.PlayerCount + "/" + _info.MaxPlayers;
	}

	public void OnClick()
	{
		PhotonNetwork.JoinRoom(info.Name);
	}
}