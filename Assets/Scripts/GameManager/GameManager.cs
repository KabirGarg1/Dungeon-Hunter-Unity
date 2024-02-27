using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
public class GameManager : SingletonMonobehaviour<GameManager>
{
    [Tooltip("Populate with dungeonlevel scriptable opbjects")]
    [SerializeField] private List<DungeonLevelSO> dungeonLevelList;

    [Tooltip("Populate with the starting dungeon level for testing,first level = 0")]
    [SerializeField] private int currentDungeonLevelListIndex = 0;
    [HideInInspector] public GameState gameState;

    private void Start()
    {
        gameState = GameState.gameStarted;
    }

    private void Update()
    {
        HandleGameState();

    }

    private void HandleGameState()
    {
        switch(gameState)
        {
            case GameState.gameStarted:
                PlayDungeonLevel(currentDungeonLevelListIndex);
                gameState = GameState.playingLevel;
                break;
        }
    }
    private void PlayDungeonLevel(int dungeonLevelListIndex)
    {
        bool dungeonBuiltSucessfully = DungeonBuilder.Instance.GenerateDungeon(dungeonLevelList[dungeonLevelListIndex]);
        if (dungeonBuiltSucessfully)
        {
            Debug.Log("COuldnt Build Dungeon From specified graphs and tilemaps");
        }
    }

    #region VALIDATION
#if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckEnumerableValues(this,nameof(dungeonLevelList),dungeonLevelList);
    }

#endif

    #endregion

}
