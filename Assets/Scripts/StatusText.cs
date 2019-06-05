using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusText : MonoBehaviour
{
    TextMesh[] statusTexts;

    // Start is called before the first frame update
    void Start()
    {
        statusTexts = GetComponentsInChildren<TextMesh>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateStatusTexts();
    }

    private void UpdateStatusTexts()
    {
        string score = GameManager.Instance.PlayerScore.ToString();
        string darts = GameManager.Instance.PlayerDarts.ToString();
        statusTexts[0].text = "Score: " + score;
        statusTexts[1].text = "Darts: " + darts;
    }
}
