using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItem : MonoBehaviourPunCallbacks, IOnEventCallback
{
	public Text text;
	public Text otherText;
	public Text readyText;
	public Player player;
	public bool ready = false;
	public const byte ReadyGameEventCode = 2;

	private void Awake()
	{

	}

	public void SetUp(Player _player)
	{
		player = _player;
		text.text = _player.NickName;
		otherText.text = PhotonNetwork.GetPing().ToString() + " ms";
		readyText.text = _player.IsMasterClient ? " Master" : (ready ? "Listo" : "No Listo");
		readyText.color = _player.IsMasterClient ? Color.magenta : (ready ? Color.green : Color.red);
		readyText.fontStyle = ready ? FontStyle.Bold : FontStyle.Normal;
		readyText.GetComponent<Button>().interactable = player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
		
	}

	public void onClickReady()
	{

		if (player.UserId != PhotonNetwork.LocalPlayer.UserId) return;
		if (PhotonNetwork.IsMasterClient) return;
		ready = !ready;
		readyText.text = ready ? "Listo" : "No Listo";
		readyText.color = ready ? Color.green : Color.red;
		readyText.fontStyle = ready ? FontStyle.Bold : FontStyle.Normal;
		object[] content = new object[] { ready, player.ActorNumber };
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
		PhotonNetwork.RaiseEvent(ReadyGameEventCode, content, raiseEventOptions, SendOptions.SendReliable);
	}

	public void OnEvent(EventData photonEvent)
	{
		byte eventCode = photonEvent.Code;
		if (eventCode == ReadyGameEventCode)
		{
			object[] data = (object[])photonEvent.CustomData;
			bool ready = (bool)data[0];
			int userIdUpdate = (int)data[1];
			if (player.ActorNumber == userIdUpdate)
			RPC_ChangeReady(ready);
		}
	}

	private void OnEnable()
	{
		PhotonNetwork.AddCallbackTarget(this);
	}

	private void OnDisable()
	{
		PhotonNetwork.RemoveCallbackTarget(this);
	}

	//[PunRPC]
	protected virtual void RPC_ChangeReady(bool readyRPC)
	{
		ready = readyRPC;
		readyText.text = ready ? "Listo" : "No Listo";
		readyText.color = ready ? Color.green : Color.red;
		readyText.fontStyle = ready ?  FontStyle.Bold : FontStyle.Normal;
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		if(player == otherPlayer)
		{
			Destroy(gameObject);
		}
	}

	public override void OnLeftRoom()
	{
		Destroy(gameObject);
	}
}