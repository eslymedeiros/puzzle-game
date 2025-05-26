using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PuzzleUIButtonShuffler : MonoBehaviour
{
    public List<Button> buttons;
    private List<Sprite> workingSprites;

    private List<Sprite> originalSprites;
    private int selectedIndex = -1;

    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    void Start()
    {
        originalSprites = new List<Sprite>(buttons.Count);
        foreach (var btn in buttons)
            originalSprites.Add(btn.image.sprite);

        ShuffleSprites();
    }

    void ShuffleSprites()
    {
        int n = buttons.Count;
        List<Sprite> spritesToShuffle = new List<Sprite>(originalSprites);

        for (int i = 0; i < n; i++)
        {
            int j = Random.Range(i, n);
            Sprite tmp = spritesToShuffle[i];
            spritesToShuffle[i] = spritesToShuffle[j];
            spritesToShuffle[j] = tmp;
        }

        for (int i = 0; i < n; i++)
            buttons[i].image.sprite = spritesToShuffle[i];
    }

     void OnPieceClicked(int index)
    {
        if (selectedIndex == -1)
        {
            selectedIndex = index;
            buttons[index].image.color = highlightColor;
        }
        else if (selectedIndex == index)
        {
            buttons[index].image.color = normalColor;
            selectedIndex = -1;
        }
        else
        {
            SwapPieces(selectedIndex, index);

            buttons[selectedIndex].image.color = normalColor;
            selectedIndex = -1;
        }
    }

    void SwapPieces(int a, int b)
    {
        var tmp = workingSprites[a];
        workingSprites[a] = workingSprites[b];
        workingSprites[b] = tmp;

        buttons[a].image.sprite = workingSprites[a];
        buttons[b].image.sprite = workingSprites[b];
    }
}
