using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IceCreamGame : MonoBehaviour
{
    [SerializeField] private Sounds sounds;
    [SerializeField] private Image background;
    [SerializeField] private Sprite[] backs;
 
    [SerializeField] private GameObject startScreen;

    [SerializeField] private GameObject levelInfoScreen;
    [SerializeField] private TMP_Text levelText;

    [SerializeField] private GameObject playScreen;
    [SerializeField] private Image timeLine;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private RectTransform customerRectTr;
    [SerializeField] private GameObject[] skins;
    [SerializeField] private GameObject clouds;
    [SerializeField] private IceCream client;
    [SerializeField] private IceCream resultCream;
    [SerializeField] private IceCream player;
    [SerializeField] private CanvasGroup screenBlock;
    [SerializeField] private GameObject rozhokPanel;
    [SerializeField] private GameObject confirmPanel;

    [SerializeField] private TMP_Text resultTitleLable;
    [SerializeField] private TMP_Text resultDescLable;
    [SerializeField] private Animator resultAnimator;


    [SerializeField] private GameObject LoseScreen;
    [SerializeField] private GameObject WinScreen;
    [SerializeField] private TMP_Text winLevelLable;

    private int Level
    {
        get => PlayerPrefs.GetInt("Level", 1);
        set => PlayerPrefs.SetInt("Level", value);
    }

    private int lives = 3;
    private int customerId = 0;
    private int MaxCustomers => Level / 5 + 3;

    private void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;

        SetBack();
    }

    // Update is called once per frame
    void Update()
    {
        if (!playScreen.activeSelf) return;

        CustomerMove();

        TimeUpdate();
    }

    public void ShowLevel()
    {
        startScreen.SetActive(false);
        levelInfoScreen.SetActive(true);
        levelText.text = Level.ToString();

        Invoke("StartGame", 2f);

        sounds.PlayClickSound();
    }

    public void StartGame()
    {
        LoseScreen.SetActive(false);
        WinScreen.SetActive(false);

        levelInfoScreen.SetActive(false);
        playScreen.SetActive(true);
        rozhokPanel.SetActive(false);
        clouds.SetActive(false);

        screenBlock.interactable = false;

        lives = 3;
        customerId = 0;
        
        livesText.text = $"x{lives}";
        progressText.text = $"{customerId+1}/{MaxCustomers + 1}";

        SetCustomer();
    }

    public void BackToMenu()
    {
        startScreen.SetActive(true);
        playScreen.SetActive(false);

        sounds.PlayClickSound();
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void Accept()
    {
        bool right = client.types.Count == player.types.Count;
        
        for (int i = 0; i < client.types.Count && right; i++)
            if (client.types[i] != player.types[i]) right = false;

        resultTitleLable.text = right ? "GOOD!" : "WRONG!";
        resultDescLable.text = right ? "Score +1" : "Lives -1";
        resultAnimator.Play("Show");

        screenBlock.interactable = false;
        clouds.SetActive(false);

        for (var i = 0; i < player.types.Count; i++) resultCream.types.Add(player.types[i]);
        resultCream.UpdateView();
        PlayerCreamClear();

        if (right)
        {
            sounds.PlayRightSound();
            customerId++;
        }
        else
        {
            sounds.PlayWrongSound();
            lives--;
        }

        livesText.text = $"x{lives}";
        progressText.text = $"{customerId + 1}/{MaxCustomers + 1}";
        state = 2;
    }

    public void Refresh()
    {
        PlayerCreamClear();

        rozhokPanel.SetActive(true);
        confirmPanel.SetActive(false);

        sounds.PlayClickSound();
    }

    public void ChoiceRozhok(int i)
    {
        player.types.Add(i);
        player.UpdateView();

        rozhokPanel.SetActive(false);
        confirmPanel.SetActive(true);

        sounds.PlayClickSound();
    }

    public void AddCream(int i)
    {
        if (player.types.Count % 4 == 0) return;

        player.types.Add(i);
        player.UpdateView();

        sounds.PlayClickSound();
    }

    int state = 0; //0 stop //1 come //2 leave
    private void SetCustomer()
    {
        resultCream.types.Clear();
        resultCream.UpdateView();

        clouds.SetActive(false);
        customerRectTr.anchoredPosition = Vector2.left * Screen.width;
        state = 1;

        Refresh();

        int skin = Random.Range(0, skins.Length);
        for (var i = 0; i < skins.Length; i++) skins[i].SetActive(i == skin);
        client.SetRandom(Level, customerId);
    }

    float remainTime;
    private void TimeUpdate()
    {
        if(remainTime > 0 && state == 0)
        {
            remainTime -= Time.deltaTime;
            timeLine.fillAmount = remainTime / 7f;

            if(remainTime <= 0f)
            {
                Accept();
            }
        }
    }

    private void CustomerMove()
    {
        if(state == 1)
        {
            customerRectTr.anchoredPosition = Vector2.MoveTowards(customerRectTr.anchoredPosition, Vector2.zero, Time.deltaTime * 500f);

            if(customerRectTr.anchoredPosition == Vector2.zero)
            {
                state = 0;
                remainTime = 7f;
                screenBlock.interactable = true;
                clouds.SetActive(true);
                rozhokPanel.SetActive(true);
            }
        }
        if(state == 2)
        {
            customerRectTr.anchoredPosition = Vector2.MoveTowards(customerRectTr.anchoredPosition, Vector2.right * Screen.width, Time.deltaTime * 500f);

            if (customerRectTr.anchoredPosition == Vector2.right * Screen.width)
            {
                state = 0;
                if (lives == 0) GameOver();
                else if (customerId == MaxCustomers) Win();
                else SetCustomer();
            }
        }
    }

    private void GameOver()
    {
        playScreen.SetActive(false);
        LoseScreen.SetActive(true);

        sounds.PlayLvlLose();
    }

    private void Win()
    {
        playScreen.SetActive(false);
        WinScreen.SetActive(true);
        winLevelLable.text = $"{Level} LEVEL\nCOMPLETED!";

        Level++;

        sounds.PlayLvlWin();
        SetBack();
    }

    private void PlayerCreamClear()
    {
        player.types.Clear();
        player.UpdateView();
    }

    private void SetBack()
    {
        background.sprite = backs[Level % backs.Length];
    }
}
