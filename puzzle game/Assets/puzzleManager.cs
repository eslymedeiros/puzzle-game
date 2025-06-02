using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PuzzleController : MonoBehaviour
{
    [Header("Configuração dos 16 Buttons (em ordem de grade 1,1→4,4)")]
    public List<Button> buttons;

    [Header("Cores de realce")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    [Header("Elementos de UI para vitória/replay")]
    public GameObject noReplay;
    public GameObject winScreen;           // Painel de vitória (ativa quando o puzzle é completado)
    public Button replayButton;            // Botão “Replay” no winScreen
    public Button restartButton;           // Botão “Jogar Novamente” no winScreen
    public Button cancelReplayButton;      // Botão para cancelar o replay (visível durante replay)

    private List<Sprite> correctSprites;           // Sprites na ordem correta (posição alvo)
    private List<Sprite> workingSprites;           // Sprites atuais em disputa/estado do jogo
    private List<Sprite> initialShuffledSprites;   // Estado embaralhado inicial (para replay)

    private int selectedIndex = -1;                // Índice da peça atualmente selecionada (–1 = nenhuma)
    private Stack<SwapCommand> commandHistory = new Stack<SwapCommand>();    // Histórico para desfazer
    private List<SwapCommand> replayHistory = new List<SwapCommand>();       // Histórico para replay

    private Coroutine replayCoroutine;
    private bool replayCancelled;

    void Start()
    {
        // 1) Salva os sprites corretos (posição alvo) a partir dos Buttons já configurados no Editor
        correctSprites = new List<Sprite>(buttons.Count);
        foreach (var btn in buttons)
            correctSprites.Add(btn.image.sprite);

        // 2) Cria workingSprites como cópia e embaralha
        workingSprites = new List<Sprite>(correctSprites);
        ShuffleSprites(workingSprites);

        // 3) Salva o estado embaralhado inicial para usar no replay
        initialShuffledSprites = new List<Sprite>(workingSprites);

        // 4) Inicializa cada Button com o sprite embaralhado e configura o listener
        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i; // captura local para o listener
            buttons[i].image.sprite = workingSprites[i];
            buttons[i].image.color = normalColor;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => OnPieceClicked(idx));
        }

        // 5) Configura botões de UI (inativar elementos de win/replay no início)
        winScreen.SetActive(false);
        cancelReplayButton.gameObject.SetActive(false);

        // 6) Botões no winScreen
        replayButton.onClick.RemoveAllListeners();
        replayButton.onClick.AddListener(OnReplayClicked);

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(OnRestartClicked);

        cancelReplayButton.onClick.RemoveAllListeners();
        cancelReplayButton.onClick.AddListener(OnCancelReplay);
    }

    // Embaralha **in place** a lista de sprites
    void ShuffleSprites(List<Sprite> list)
    {
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            int j = Random.Range(i, n);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    // Chamado sempre que um Button/peça é clicado
    void OnPieceClicked(int index)
    {
        // Se já estivermos em replay, ignora cliques
        if (replayCoroutine != null) return;

        if (selectedIndex == -1)
        {
            // i. Seleciona a primeira peça
            selectedIndex = index;
            buttons[index].image.color = highlightColor;
        }
        else if (selectedIndex == index)
        {
            // ii. Se clicar novamente na mesma, cancela seleção
            buttons[index].image.color = normalColor;
            selectedIndex = -1;
        }
        else
        {
            // iii. Clicou em uma segunda peça: faz swap e registra comando
            var cmd = new SwapCommand(selectedIndex, index);
            cmd.Execute(workingSprites, buttons);
            commandHistory.Push(cmd);
            replayHistory.Add(cmd);

            // Remove o destaque da primeira e reseta seleção
            buttons[selectedIndex].image.color = normalColor;
            selectedIndex = -1;

            // 6. Verifica vitória
            if (IsPuzzleSolved())
                ShowWinScreen();
        }
    }

    // Verifica se workingSprites == correctSprites
    bool IsPuzzleSolved()
    {
        for (int i = 0; i < workingSprites.Count; i++)
            if (workingSprites[i] != correctSprites[i])
                return false;
        return true;
    }

    // Exibe a tela de vitória
    void ShowWinScreen()
    {
        winScreen.SetActive(true);
    }

    // “Desfazer” último movimento
    public void OnUndoClicked()
    {
        // Só desfaz se não houver peça selecionada e houver comandos disponíveis e NÃO estivermos em replay
        if (selectedIndex == -1 && commandHistory.Count > 0 && replayCoroutine == null)
        {
            var cmd = commandHistory.Pop();
            cmd.Undo(workingSprites, buttons);

            // Remove o último comando também do replayHistory
            if (replayHistory.Count > 0)
                replayHistory.RemoveAt(replayHistory.Count - 1);
        }
    }

    // Reinicia a cena para jogar novamente
    public void OnRestartClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Inicia o replay
    public void OnReplayClicked()
    {
        // Oculta a tela de vitória e exibe o botão de cancelar
        winScreen.SetActive(false);
        noReplay.gameObject.SetActive(true);
        cancelReplayButton.gameObject.SetActive(true);

        // Reseta o puzzle para o estado inicial embaralhado
        ResetPuzzleToStart();

        // Inicia a coroutine de replay
        replayCancelled = false;
        replayCoroutine = StartCoroutine(PlayReplay());
    }

    // Cancela o replay (executa tudo de uma vez)
    public void OnCancelReplay()
    {
        replayCancelled = true;
        noReplay.gameObject.SetActive(false);
    }

    // Restaura workingSprites e Buttons ao estado embaralhado inicial
    void ResetPuzzleToStart()
    {
        // Cancela eventual seleção pendente
        if (selectedIndex != -1)
        {
            buttons[selectedIndex].image.color = normalColor;
            selectedIndex = -1;
        }

        // Restaura a lista de sprites ao estado embaralhado inicial
        workingSprites = new List<Sprite>(initialShuffledSprites);

        // Atualiza visual dos Buttons
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].image.sprite = workingSprites[i];

        // Limpa o histórico de desfazer (o replayHistory permanece
        // pois mostra a sequência completa até vitória)
        commandHistory.Clear();
    }

    // Coroutine que executa o replay em etapas de 1s
    IEnumerator PlayReplay()
    {
        // Percorre todos os comandos gravados
        for (int i = 0; i < replayHistory.Count; i++)
        {
            // Se o usuário cancelou, para o loop e executa o restante instantaneamente
            if (replayCancelled)
            {
                for (int j = i; j < replayHistory.Count; j++)
                    replayHistory[j].Execute(workingSprites, buttons);
                break;
            }

            // Executa um comando e espera 1 segundo
            replayHistory[i].Execute(workingSprites, buttons);
            yield return new WaitForSeconds(1f);
        }

        // Replay concluído: esconde botão “Cancelar” e mostra tela de vitória novamente
        cancelReplayButton.gameObject.SetActive(false);
        replayCoroutine = null;
        ShowWinScreen();
    }

    // Classe interna que implementa o padrão Command para troca de peças
    private class SwapCommand
    {
        private int indexA, indexB;

        public SwapCommand(int a, int b)
        {
            indexA = a;
            indexB = b;
        }

        // Executa o swap (usado tanto na jogada original quanto no replay)
        public void Execute(List<Sprite> sprites, List<Button> buttons)
        {
            var tmp = sprites[indexA];
            sprites[indexA] = sprites[indexB];
            sprites[indexB] = tmp;

            buttons[indexA].image.sprite = sprites[indexA];
            buttons[indexB].image.sprite = sprites[indexB];
        }

        // Desfaz o swap (usado pelo Undo)
        public void Undo(List<Sprite> sprites, List<Button> buttons)
        {
            // Basta inverter os índices novamente
            var tmp = sprites[indexA];
            sprites[indexA] = sprites[indexB];
            sprites[indexB] = tmp;

            buttons[indexA].image.sprite = sprites[indexA];
            buttons[indexB].image.sprite = sprites[indexB];
        }
    }
}
