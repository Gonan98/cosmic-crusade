using ExitGames.Client.Photon;
using Mirror;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
	public static LobbyManager Instance;
	public Text textRoom;
	public int countdown = 5;
	public GameObject loadingContent;
	public GameObject dialogContent;
	public GameObject mainRoom;
	public GameObject main;

	public GameObject startGameButton;

	public GameObject alert;
	public InputField roomNameInput;
	public InputField roomNameInputJoin;

	public Transform roomListContent;
	public GameObject roomListItemPrefab;

	public Transform playerListContent;
	public GameObject playerListItemPrefab;

	public GameObject errorPanel;

	private Text textAlert;
	private FunctionTimer functionTimer;



	public const byte StartGameEventCode = 1;
	public const byte PreStartGameEventCode = 3;

	public GameObject counterScreen;

	private bool _loadingScene = false;

	void Awake()
	{
		Instance = this;
		if (!PhotonNetwork.InLobby) PhotonNetwork.JoinLobby();
	}

	public void Start()
	{
		if (!PhotonNetwork.InLobby) PhotonNetwork.JoinLobby();
		textAlert = alert.GetComponentInChildren<Text>(true);
		if (PlayerStatusManager.Instance.playerDisconnect)
		{
			//errorPanel.SetActive(true);
			//main.SetActive(false);
			//PlayerStatusManager.Instance.playerDisconnect = false;
		}
	}

	public void Update()
	{
		if (PhotonNetwork.LevelLoadingProgress > 0 && !_loadingScene)
		{
			RPC_LoadLevel(true);
			_loadingScene = true;
		}
		if (functionTimer == null) return;
		functionTimer.Update();
	}

	public void CreateRoom()
	{
		if (functionTimer != null) return;
		if (string.IsNullOrEmpty(roomNameInput.text))
		{
			textAlert.text = "No ha ingresado un nombre para la sala";
			LeanTween.moveX(alert, 150,.5f);
			functionTimer = new FunctionTimer(ResetAlert, 3f);
			return;
		}

		RoomOptions options = new RoomOptions();
		options.MaxPlayers = 2;
		PhotonNetwork.CreateRoom(roomNameInput.text, options);
		DialogManager.Instance.Close();
	}

	public void JoinRoom()
	{
		if (functionTimer != null) return;
		if (string.IsNullOrEmpty(roomNameInputJoin.text))
		{
			textAlert.text = "No ha ingresado el nombre de la sala";
			LeanTween.moveX(alert, 150, .5f);
			functionTimer = new FunctionTimer(ResetAlert, 3f);
			return;
		}
		PhotonNetwork.JoinRoom(roomNameInputJoin.text);
		DialogManager.Instance.Close();
	}

	public void OnEnable()
	{
		PhotonNetwork.AddCallbackTarget(this);
	}

	public void OnDisable()
	{
		PhotonNetwork.RemoveCallbackTarget(this);
	}

	public void OnEvent(EventData photonEvent)
	{
		byte eventCode = photonEvent.Code;
		if (eventCode == PreStartGameEventCode)
		{
			counterScreen.SetActive(true);
			if (PhotonNetwork.IsMasterClient)
			{
				counterScreen.GetComponent<PreStartGameManager>().StartCountdown(countdown, RPC_LoadLevel);
			} else
			{
				counterScreen.GetComponent<PreStartGameManager>().StartCountdown(countdown, HiddenCounterScreen);
			}
		}
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		PlayerStatusManager.Instance.playerDisconnect = true;
		SceneManager.LoadScene(0);
	}

	public void HiddenCounterScreen(bool isOtherPlayters = false)
	{
		counterScreen.SetActive(false);
	}
	public void StartGame()
	{

		if (!PhotonNetwork.IsMasterClient) return;
		/*if (playerListContent.childCount != 2)
		{
			textAlert.text = "Deben haber dos Jugadores para iniciar el juego";
			LeanTween.moveX(alert, 150, .5f);
			functionTimer = new FunctionTimer(ResetAlert, 3f);
			return;
		}*/
		foreach (PlayerItem player in playerListContent.GetComponentsInChildren<PlayerItem>(true))
		{
			//Debug.Log(player.player.NickName);
			if (!player.player.IsMasterClient && !player.ready) 
			{
				textAlert.text = "No Todos los jugadores estan listos";
				LeanTween.moveX(alert, 150, .5f);
				functionTimer = new FunctionTimer(ResetAlert, 3f);
				return;
			}
		}
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
		PhotonNetwork.RaiseEvent(PreStartGameEventCode, null, raiseEventOptions, SendOptions.SendReliable);
	}

	/*private void LoadGame()
	{
		Debug.Log("Pre Init Game");
		Debug.Log("INIT");
		main.SetActive(false);
		mainRoom.SetActive(false);
		dialogContent.SetActive(false);
		Text textLoading = loading.GetComponentInChildren<Text>(true);

		Debug.Log("Load Level");
		PhotonNetwork.LoadLevel("Game");
		Debug.Log("Init While");
		while (PhotonNetwork.LevelLoadingProgress < 0.89f)
		{
			Debug.Log(PhotonNetwork.LevelLoadingProgress);
			textLoading.text = "Cargando: " + (int)((PhotonNetwork.LevelLoadingProgress + 0.1) * 100)+ "%";
			//loadAmount = async.progress;
			loading.fillAmount = PhotonNetwork.LevelLoadingProgress;
			//yield return null;
			
		}
		Debug.Log("Finish While");
		Debug.Log("Completed Game");
	}*/

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
		dialogContent.SetActive(false);
		mainRoom.SetActive(false);
		main.SetActive(true);
	}

	private void ResetAlert()
	{
		textAlert.text = "";
		LeanTween.moveX(alert, -200, .5f);
		functionTimer = null;
	}

	#region Callbacks

	public override void OnCreatedRoom()
	{
		Debug.Log("Creando una sala");
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		textAlert.text = "No se ha podido crear la sala";
		LeanTween.moveX(alert, 150, .5f);
		functionTimer = new FunctionTimer(ResetAlert, 3f);
	}

	public override void OnJoinedRoom()
	{
		//roomNameText.text = PhotonNetwork.CurrentRoom.Name;
		dialogContent.SetActive(false);
		mainRoom.SetActive(true);
		main.SetActive(false);
		//Player[] players = PhotonNetwork.PlayerList;
		//startGameButton.SetActive(PhotonNetwork.IsMasterClient);
		textRoom.text = PhotonNetwork.CurrentRoom.Name;
		UpdatePlayersInRoom();
	}

	public void UpdatePlayersInRoom()
	{
		foreach (Transform child in playerListContent)
		{
			Destroy(child.gameObject);
		}
		foreach (Player player in PhotonNetwork.PlayerList)
		{
			//Debug.Log(player.NickName);
			//Instantiate(playerListItemPrefab, playerListContent).GetComponentInChildren<Text>().text = player.NickName;
			Instantiate(playerListItemPrefab, playerListContent).GetComponentInChildren<PlayerItem>().SetUp(player);
		}
		startGameButton.SetActive(PhotonNetwork.IsMasterClient);
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		foreach (Transform trans in roomListContent)
		{
			Destroy(trans.gameObject);
		}

		foreach (RoomInfo room in roomList)
		{
			if (!room.IsOpen || !room.IsVisible || room.RemovedFromList) continue;
			Instantiate(roomListItemPrefab, roomListContent).GetComponentInChildren<RoomItem>().SetUp(room);
		}
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		startGameButton.SetActive(PhotonNetwork.IsMasterClient);
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		textAlert.text = "No se ha podido encontra dicha sala";
		LeanTween.moveX(alert, 150, .5f);
		functionTimer = new FunctionTimer(ResetAlert, 3f);
	}

	public override void OnLeftRoom()
	{
		if (!PhotonNetwork.InRoom) return;
		UpdatePlayersInRoom();
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		UpdatePlayersInRoom();
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		UpdatePlayersInRoom();
	}


	//[PunRPC]
	public void RPC_LoadLevel(bool isOtherPlayers = false)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			// Inhabilita la sala
			PhotonNetwork.CurrentRoom.IsOpen = false;
			PhotonNetwork.CurrentRoom.IsVisible = false;
		}
		mainRoom.SetActive(false);
		dialogContent.SetActive(false);
		main.SetActive(false);
		counterScreen.SetActive(false);
		loadingContent.SetActive(true);
		StartCoroutine(LevelLoader(isOtherPlayers));
	}


	private IEnumerator LevelLoader(bool isOtherPlayers)
	{
		Text textLoading = loadingContent.GetComponentInChildren<Text>(true);
		Image loadingImage = loadingContent.GetComponentsInChildren<Image>(true)[1];
		//PhotonNetwork.IsMessageQueueRunning = false;
		if(!isOtherPlayers) PhotonNetwork.LoadLevel("Game");
		while (PhotonNetwork.LevelLoadingProgress < 1)
		{
			//Debug.Log(PhotonNetwork.LevelLoadingProgress);
			textLoading.text = "Cargando: " + (int)((PhotonNetwork.LevelLoadingProgress) * 100) + "%";
			loadingImage.fillAmount = PhotonNetwork.LevelLoadingProgress;
			yield return new WaitForEndOfFrame();
		}
		main.SetActive(true);
		loadingContent.SetActive(false);
		//PhotonNetwork.IsMessageQueueRunning = true;
	}

	public void OnClickOK()
	{
		errorPanel.SetActive(false);
		main.SetActive(true);
	}
	#endregion
}
