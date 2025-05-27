using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PuzzleUIManager : MonoBehaviour
{
    [Header("Arraste os 16 Buttons na ordem da grade")]
    public List<Button> buttons;

    private List<Sprite> correctSprites;
    private List<Sprite> workingSprites;

    private int selectedIndex = -1;

    [Header("Cores de seleção")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    private Stack<SwapCommand> commandHistory = new Stack<SwapCommand>();
    private List<SwapCommand> replayHistory = new List<SwapCommand>();
    
    public GameObject winScreen;
    public Button replayButton, restartButton;

    private Coroutine replayCoroutine;

    void Start()
    {
        correctSprites = new List<Sprite>(buttons.Count);
        foreach (var btn in buttons)
            correctSprites.Add(btn.image.sprite);

        workingSprites = new List<Sprite>(correctSprites);
        ShuffleSprites();

        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i; 
            buttons[i].image.sprite = workingSprites[i];
            buttons[i].image.color = normalColor;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => OnPieceClicked(idx));
        }
    }

    void ShuffleSprites()
    {
        int n = workingSprites.Count;
        for (int i = 0; i < n; i++)
        {
            int j = Random.Range(i, n);
            var tmp = workingSprites[i];
            workingSprites[i] = workingSprites[j];
            workingSprites[j] = tmp;
        }
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

        var command = new SwapCommand(a, b);
        command.Execute(workingSprites, buttons);
        commandHistory.Push(command);
        replayHistory.Add(command);

        if (IsPuzzleSolved())
            ShowWinScreen();
    }

    bool IsPuzzleSolved()
    {
        for (int i = 0; i < workingSprites.Count; i++)
            if (workingSprites[i] != correctSprites[i])
                return false;
        return true;
    }

    public class SwapCommand
    {
        private int indexA, indexB;

        public SwapCommand(int a, int b)
        {
            indexA = a;
            indexB = b;
        }

        public void Execute(List<Sprite> sprites, List<Button> buttons){
        }

        public void Undo(List<Sprite> sprites, List<Button> buttons)
        {
            var tmp = sprites[indexA];
            sprites[indexA] = sprites[indexB];
            sprites[indexB] = tmp;

            buttons[indexA].image.sprite = sprites[indexA];
            buttons[indexB].image.sprite = sprites[indexB];
        }
    }
    
    public void OnUndoClicked()
    {
        if (selectedIndex == -1 && commandHistory.Count > 0)
        {
            var cmd = commandHistory.Pop();
            cmd.Undo(workingSprites, buttons);
        }
    }

    void ShowWinScreen()
    {
        winScreen.SetActive(true);
    }
}