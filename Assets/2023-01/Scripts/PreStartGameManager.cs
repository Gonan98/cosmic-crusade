using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreStartGameManager : MonoBehaviour
{
    public Image image;
    public Text textSecond;
	public delegate void FunctionCallBack(bool param = false);
	public FunctionCallBack onEndCountdown;

	public void StartCountdown(int seconds, FunctionCallBack callBack)
	{
		onEndCountdown = callBack;
		StartCoroutine(Countdown(seconds));
	}
	IEnumerator Countdown(int seconds)
	{
		int count = seconds;
		textSecond.text = count.ToString();
		while (count > 0)
		{
			// display something...
			yield return new WaitForSeconds(1);
			count--;
			textSecond.text = count.ToString();
		}
		onEndCountdown();
	}
	// Update is called once per frame
	void Update()
    {
        image.gameObject.transform.Rotate(0f, 0f, 10f * Time.deltaTime * 10);
    }
}
