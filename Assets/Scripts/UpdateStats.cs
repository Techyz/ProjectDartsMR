using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateStats : MonoBehaviour
{
    TextMesh[] statusTexts;

    void Start()
    {
        statusTexts = GetComponentsInChildren<TextMesh>();
    }

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
