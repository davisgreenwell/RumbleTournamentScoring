using System;
using MelonLoader;
using Il2CppRUMBLE.Managers;
using UnityEngine;
using Il2CppTMPro;
using Il2CppPhoton.Pun;
using RumbleModdingAPI;


[assembly: MelonInfo(typeof(tournamentScore.main), "tournamentScoring", "1.0.0", "davisg")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

namespace tournamentScore
{
    public class main : MelonMod
    {
        public static main Instance; // for Harmony access

        private int localMatchWins = 0;
        private int remoteMatchWins = 0;
        private int localRoundsInLosses = 0;
        private int remoteRoundsInLosses = 0;
        private int localRoundWinsThisMatch = 0;
        private int remoteRoundWinsThisMatch = 0;

        private GameObject scoreboardGO;
        private TextMeshPro scoreboardText;

        private string currentScene = "";
        private bool scoreboardSpawned = false;
        private Vector3 pendingPosition;
        private float spawnTimer = 0f;
        private bool spawnCountdownActive = false;

        public static bool IsHost()
        {
            return PhotonNetwork.IsMasterClient;
        }

        public override void OnInitializeMelon()
        {
            Instance = this;
            HarmonyInstance.PatchAll(); // hook into game methods
            Calls.onMatchStarted += OnMatchStarted;
            Calls.onRoundEnded += OnRoundEnded;
            Calls.onMatchEnded += OnMatchEnded;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            currentScene = sceneName;
            scoreboardSpawned = false;
            spawnCountdownActive = false;
            spawnTimer = 0f;

            MelonLogger.Msg($"[DEBUG] Scene loaded: {sceneName}");

            if (sceneName == "Gym")
            {
                MelonLogger.Msg("[DEBUG] Gym loaded. Resetting scores and hiding scoreboard.");
                ResetScore();

                if (scoreboardGO != null)
                    scoreboardGO.SetActive(false);

                return;
            }

            if (sceneName == "Map0" || sceneName == "Map1")
            {
                pendingPosition = sceneName == "Map0" ? new Vector3(-10f, 5f, 0f) : new Vector3(12f, 5f, 0f);

                MelonLogger.Msg("[DEBUG] Preparing to spawn scoreboard after delay...");
                spawnCountdownActive = true;
            }
        }

        public override void OnUpdate()
        {
            if (spawnCountdownActive)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= 5f && !scoreboardSpawned)
                {
                    SpawnScoreboard();
                }
            }

            if (scoreboardSpawned && scoreboardGO != null && scoreboardText != null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    scoreboardGO.transform.rotation = Quaternion.LookRotation(scoreboardGO.transform.position - cam.transform.position);
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                MelonLogger.Msg("[DEBUG] Reset key pressed. Resetting score manually!");
                ResetScore();
            }
        }

        private void ResetScore()
        {
            localMatchWins = 0;
            remoteMatchWins = 0;
            localRoundsInLosses = 0;
            remoteRoundsInLosses = 0;

            UpdateScoreboardText();
            MelonLogger.Msg("[DEBUG] Scoreboard and tournament scores reset to 0(0)-0(0).");
        }

        private void SpawnScoreboard()
        {
            if (scoreboardGO == null)
            {
                scoreboardGO = new GameObject("ScoreboardText");
                scoreboardText = scoreboardGO.AddComponent<TextMeshPro>();

                if (scoreboardText != null)
                {
                    scoreboardText.alignment = TextAlignmentOptions.Center;
                    scoreboardText.enableWordWrapping = false;
                    scoreboardText.fontSize = 3f;
                    scoreboardText.text = "0(0)-0(0)";
                    scoreboardText.color = Color.white;

                    scoreboardGO.transform.position = pendingPosition;
                    scoreboardGO.transform.rotation = Quaternion.identity;
                    scoreboardGO.transform.localScale = Vector3.one * 5f;

                    MelonLogger.Msg($"[DEBUG] Scoreboard spawned at {scoreboardGO.transform.position}");
                }
                else
                {
                    MelonLogger.Error("[ERROR] Failed to create scoreboard.");
                }
            }
            else
            {
                scoreboardGO.transform.position = pendingPosition;
                scoreboardGO.transform.localScale = Vector3.one * 5f;
                scoreboardGO.SetActive(true);

                MelonLogger.Msg($"[DEBUG] Reusing existing scoreboard. New position: {scoreboardGO.transform.position}");
            }

            scoreboardSpawned = true;
            UpdateScoreboardText();
        }

        public void OnMatchStarted()
        {
            MelonLogger.Msg("[DEBUG] Match started. Resetting round wins.");
            localRoundWinsThisMatch = 0;
            remoteRoundWinsThisMatch = 0;
        }

        public void OnRoundEnded()
        {
            try
            {
                var localPlayer = PlayerManager.instance?.localPlayer;
                var data = localPlayer?.Data;

                if (localPlayer == null || data == null)
                {
                    MelonLogger.Warning("[WARNING] Missing local player data. Skipping round tracking.");
                    return;
                }

                bool localWon = data.HealthPoints > 0;

                if (localWon)
                    localRoundWinsThisMatch++;
                else
                    remoteRoundWinsThisMatch++;

                MelonLogger.Msg($"[DEBUG] Round ended. LocalRounds: {localRoundWinsThisMatch}, RemoteRounds: {remoteRoundWinsThisMatch}");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[ERROR] Exception in OnRoundEnded: " + e);
            }
        }

        public void OnMatchEnded()
        {
            try
            {
                bool isHost = IsHost(); // Replace Calls.Players.IsHost()

                MelonLogger.Msg($"[DEBUG] IsHost() returned: {isHost}");

                bool localWonMatch = localRoundWinsThisMatch > remoteRoundWinsThisMatch;

                if (localWonMatch)
                {
                    localMatchWins++;

                    if (isHost && remoteRoundWinsThisMatch > 0)
                    {
                        remoteRoundsInLosses += 1;
                        MelonLogger.Msg("[DEBUG] Opponent was client and won rounds. Incrementing opponent parentheses.");
                    }
                }
                else
                {
                    remoteMatchWins++;

                    if (!isHost && localRoundWinsThisMatch > 0)
                    {
                        localRoundsInLosses += 1;
                        MelonLogger.Msg("[DEBUG] Local was client and won rounds. Incrementing local parentheses.");
                    }
                }

                MelonLogger.Msg($"[DEBUG] Match ended. Score: {localMatchWins}({localRoundsInLosses})-{remoteMatchWins}({remoteRoundsInLosses})");
                UpdateScoreboardText();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[ERROR] Exception in OnMatchEnded: " + e);
            }
        }

        private void UpdateScoreboardText()
        {
            string text = $"{localMatchWins}({localRoundsInLosses})-{remoteMatchWins}({remoteRoundsInLosses})";

            if (scoreboardText != null)
                scoreboardText.text = text;
        }
    }
}
