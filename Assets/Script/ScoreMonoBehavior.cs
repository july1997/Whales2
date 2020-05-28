using UnityEngine;
using UnityEngine.UI;

public class ScoreMonoBehavior : MonoBehaviour
{
    private Text _countText;

    private void Awake()
    {
        _countText = this.GetComponent<Text>();
    }

    public void SetScore(int count)
    {
        _countText.text = "Score : " + count.ToString();
    }
}
