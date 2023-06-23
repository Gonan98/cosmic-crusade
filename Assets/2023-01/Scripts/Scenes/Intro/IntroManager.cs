using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroManager : MonoBehaviourPunCallbacks
{
	private bool IsConnected;
	public GameObject panelInputName;
	public Image loading;
	
	public GameObject loadingPanel;

	public Image introLoading;
	public InputField nameInput;
    public GameObject alert;

	public GameObject errorPanel;


	private FunctionTimer functionTimer;
	private Text textAlert;

	void Awake()
	{
		this.IsConnected = false;
	}

	private void Start()
	{
		if (PlayerStatusManager.Instance.playerDisconnect)
		{
			errorPanel.SetActive(true);
			loadingPanel.SetActive(false);
			errorPanel.GetComponentInChildren<Text>().text = "Se ha desconectado inesperadamente del servidor.";
			PlayerStatusManager.Instance.playerDisconnect = false;
		} 
		else
		{
			errorPanel.GetComponentInChildren<Text>().text = "No se pudo conectar con el servidor. Intente de nuevo.";
			textAlert = alert.GetComponentInChildren<Text>(true);
			StartCoroutine(waitFor(1));
		}
	}
	public void Update()
	{
        introLoading.gameObject.transform.Rotate(0f, 0f, 10f * Time.deltaTime * 10);
		if (functionTimer == null) return;
		functionTimer.Update();
	}

	public void StartGame()
    {
		if (functionTimer != null) return;
		if (string.IsNullOrEmpty(nameInput.text) && functionTimer == null)
        {
			textAlert.text = "No ha ingresado un nombre de usuario";
			LeanTween.moveX(alert, 150, .5f);
			functionTimer = new FunctionTimer(ResetAlert, 3f);
			return;
		}
		PhotonNetwork.NickName = nameInput.text;
		panelInputName.SetActive(false);
		loadingPanel.SetActive(true);
		StartCoroutine(LoadSceneAsync(1));
		//SceneManager.LoadScene
    }

    IEnumerator waitFor(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        //loadingPanel.SetActive(false);
        PhotonNetwork.ConnectUsingSettings();
		//Debug.Log($"Esta conectado: {IsConnected}");
        //loadingPanel.SetActive(false);
        //if (!IsConnected) errorPanel.SetActive(true);
    }

    IEnumerator LoadSceneAsync(int sceneId) 
	{
		yield return new WaitForSeconds(2);
		SceneManager.LoadScene(sceneId);
	}

	private void ResetAlert()
	{
		textAlert.text = "";
		LeanTween.moveX(alert, -200, .5f);
		functionTimer = null;
	}

	public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected from server: Cause " + cause.ToString());
		loadingPanel.SetActive(false);
		errorPanel.SetActive(true);
    }

	public override void OnConnectedToMaster()
	{
		Debug.Log("Se conectó al master");
		PhotonNetwork.AutomaticallySyncScene = true;
		PhotonNetwork.JoinLobby();
		loadingPanel.SetActive(false);
		panelInputName.SetActive(true);
	}

	public void OnRetry()
	{
		errorPanel.SetActive(false);
		loadingPanel.SetActive(true);
		Start();
	}

	public void OnExitGame()
	{
		Application.Quit();
	}

}
