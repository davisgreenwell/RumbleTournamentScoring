using MelonLoader;
using RumbleModdingAPI;
using Il2CppRUMBLE.Managers;
using UnityEngine;
using Il2CppTMPro;

namespace tournamentScore
{
    public class main : MelonMod
    {
        private int localPlayerMatchWins = 0;
        private int remotePlayerMatchWins = 0;
        private int localRoundsInLosses = 0;
        private int remoteRoundsInLosses = 0;
        private int localRoundWinsThisMatch = 0;
        private int remoteRoundWinsThisMatch = 0;

        private GameObject scoreboardGO;
        private TextMeshPro scoreboardText;

        private bool spawnCountdownActive = false;
        private float spawnTimer = 0f;
        private Vector3 pendingPosition;

        public override void OnLateInitializeMelon()
        {
            Calls.onMatchStarted += OnMatchStarted;
            Calls.onRoundEnded += OnRoundEnded;
            Calls.onMatchEnded += OnMatchEnded;
        }

        public override void OnUpdate()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.R))
            {
                ResetScore();
            }

            if (spawnCountdownActive)
            {
                spawnTimer += Time.deltaTime;

                if (spawnTimer >= 5f && scoreboardGO == null)
                {
                    SpawnScoreboard();
                }
            }

            if (scoreboardGO != null && scoreboardText != null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    scoreboardGO.transform.rotation = Quaternion.LookRotation(scoreboardGO.transform.position - mainCam.transform.position);
                }
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Gym")
            {
                ResetScore();
                if (scoreboardGO != null)
                {
                    scoreboardGO.SetActive(false);
                }
                spawnCountdownActive = false;
            }
            else if (sceneName == "Map0" || sceneName == "Map1")
            {
                pendingPosition = (sceneName == "Map0") ? new Vector3(-10f, 5f, 0f) : new Vector3(12f, 5f, 0f);
                spawnTimer = 0f;
                spawnCountdownActive = true;
            }
        }

        private void SpawnScoreboard()
        {
            if (scoreboardGO == null)
            {
                scoreboardGO = Calls.Create.NewText("0(0)-0(0)", 3f, Color.white, Vector3.zero, Quaternion.identity);

                scoreboardText = scoreboardGO?.GetComponent<TextMeshPro>();
                if (scoreboardText != null)
                {
                    scoreboardText.alignment = TextAlignmentOptions.Center;
                }

                scoreboardGO.transform.position = pendingPosition;
                scoreboardGO.transform.localScale = Vector3.one * 5f;

                UpdateScoreboardText();
            }
            else
            {
                scoreboardGO.transform.position = pendingPosition;
                scoreboardGO.SetActive(true);
            }

            spawnCountdownActive = false;
        }

        private void OnMatchStarted()
        {
            localRoundWinsThisMatch = 0;
            remoteRoundWinsThisMatch = 0;
        }

        private void OnRoundEnded()
        {
            try
            {
                var localPlayer = PlayerManager.instance?.localPlayer;
                var data = localPlayer?.Data;

                if (localPlayer == null || data == null)
                {
                    return;
                }

                bool localWon = data.HealthPoints > 0;

                if (localWon)
                {
                    localRoundWinsThisMatch++;
                }
                else
                {
                    remoteRoundWinsThisMatch++;
                }
            }
            catch (System.Exception e)
            {
                MelonLogger.Error("Error in OnRoundEnded: " + e);
            }
        }

        private void OnMatchEnded()
        {
            try
            {
                bool localWonMatch = localRoundWinsThisMatch > remoteRoundWinsThisMatch;
                bool localIsHost = Calls.Players.IsHost();

                if (localWonMatch)
                {
                    localPlayerMatchWins++;
                    if (localIsHost)
                    {
                        remoteRoundsInLosses += remoteRoundWinsThisMatch;
                    }
                }
                else
                {
                    remotePlayerMatchWins++;
                    if (!localIsHost)
                    {
                        localRoundsInLosses += localRoundWinsThisMatch;
                    }
                }

                UpdateScoreboardText();
            }
            catch (System.Exception e)
            {
                MelonLogger.Error("Error in OnMatchEnded: " + e);
            }
        }

        private void UpdateScoreboardText()
        {
            if (scoreboardText != null)
            {
                scoreboardText.text = $"{localPlayerMatchWins}({localRoundsInLosses})-{remotePlayerMatchWins}({remoteRoundsInLosses})";
            }
        }

        private void ResetScore()
        {
            localPlayerMatchWins = 0;
            remotePlayerMatchWins = 0;
            localRoundsInLosses = 0;
            remoteRoundsInLosses = 0;
            localRoundWinsThisMatch = 0;
            remoteRoundWinsThisMatch = 0;
            UpdateScoreboardText();
        }
    }
}