using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class LauncherManager : MonoBehaviour
{
	public InputField userNameInput;

	void Awake()
	{
		PhotonNetwork.AutomaticallySyncScene = true;
	}

	public void JoinLobby()
	{
		string userName = userNameInput.text;
		if (!string.IsNullOrEmpty(userName))
		{
			PhotonNetwork.LocalPlayer.NickName = userName;
			PhotonNetwork.ConnectUsingSettings();
			// Navegar a otra escena
		}
		else
		{
			Debug.Log("El UserName es Vacio");
		}
	}

	public void onCreateRoom()
	{
		//ActiveMyPanel(CreateRoomPanel.name);
	}
}
