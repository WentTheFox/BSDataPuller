﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using BeatSaverSharp;
using DataPuller.Data;
using DataPuller.Harmony;
using HarmonyLib;
using IPA.Utilities;
using SongDetailsCache;
using SongDetailsCache.Structs;
using TMPro;
using UnityEngine;
using Zenject;
using static SliderController.Pool;

#nullable enable
namespace DataPuller.Core
{
    internal class MapEvents : IInitializable, IDisposable
    {
        //I think I need to fix my refrences as VS does not notice when I update them.
        private static readonly BeatSaver beatSaver = new(Plugin.PLUGIN_NAME, Assembly.GetExecutingAssembly().GetName().Version);
        private static SongDetails? songDetailsCache = null;
        private readonly Timer timer = new() { Interval = 250 };
        private string? previousHash = null;
        private string? previousBSRKey = null;

        //Required objects - Made [InjectOptional] and checked at Initialize()
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'
        [InjectOptional] private BeatmapObjectManager beatmapObjectManager;
        [InjectOptional] private GameplayCoreSceneSetupData gameplayCoreSceneSetupData;
        [InjectOptional] private AudioTimeSyncController audioTimeSyncController;
        [InjectOptional] private RelativeScoreAndImmediateRankCounter relativeScoreAndImmediateRankCounter;
        [InjectOptional] private GameEnergyCounter gameEnergyCounter;

        //Optional objects for different gamemodes - checked by each gamemode.
        [InjectOptional] private ScoreController? scoreController;
        [InjectOptional] private MultiplayerController? multiplayerController;
        [InjectOptional] private ScoreUIController? scoreUIController;
        [InjectOptional] private PauseController? pauseController;
        [InjectOptional] private StandardLevelGameplayManager? standardLevelGameplayManager;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MapEvents() { } //Injects made above now
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public void Initialize()
        {
            previousHash = MapData.Instance.Hash;
            previousBSRKey = MapData.Instance.BSRKey;
            MapData.Instance.Reset();
            LiveData.Instance.Reset();

            if (DoRequiredObjectsExist(out List<string> missingObjects))
            {
                if (scoreController is not null && multiplayerController is not null) //Multiplayer
                {
                    Plugin.Logger.Debug("In multiplayer.");

                    multiplayerController.stateChangedEvent += MultiplayerController_stateChangedEvent;
                    scoreController.scoreDidChangeEvent += ScoreDidChangeEvent;

                    MapData.Instance.IsMultiplayer = true;
                    MultiplayerSessionManagerPatch.UpdatePlayerCount();
                    LiveData.Instance.Send();
                }
                else if (IsLegacyReplay() && relativeScoreAndImmediateRankCounter is not null && scoreUIController is not null) //Legacy Replay
                {
                    Plugin.Logger.Debug("In legacy replay.");

                    LevelLoaded();

                    relativeScoreAndImmediateRankCounter.relativeScoreOrImmediateRankDidChangeEvent += RelativeScoreOrImmediateRankDidChangeEvent;
                }
                else if (scoreController is not null && pauseController is not null && standardLevelGameplayManager is not null) //Singleplayer or New Replay.
                {
                    Plugin.Logger.Debug("In singleplayer.");

                    LevelLoaded();

                    //In replay mode the scorecontroller does not work so 'RelativeScoreOrImmediateRankDidChangeEvent' will read from the UI.
                    scoreController.scoreDidChangeEvent += ScoreDidChangeEvent;

                    pauseController.didPauseEvent += LevelPausedEvent;
                    pauseController.didResumeEvent += LevelUnpausedEvent;
                    pauseController.didReturnToMenuEvent += LevelQuitEvent;

                    standardLevelGameplayManager.levelFailedEvent += LevelFailedEvent;
                    standardLevelGameplayManager.levelFinishedEvent += LevelFinishedEvent;
                }
                else
                {
                    Plugin.Logger.Debug("No gamemode detected.");
                    EarlyDispose("Couldn't find the required objects for any of the valid gamemodes.");
                }
            }
            else Plugin.Logger.Error($"Required objects not found. Missing: {string.Join(", ", missingObjects)}");
        }

        /// <param name="missingObjects">Empty when returning true</param>
        /// <returns>True if the object was found, otherwise false.</returns>
        private bool DoRequiredObjectsExist(out List<string> missingObjects)
        {
            missingObjects = new();

            if (beatmapObjectManager is null) missingObjects.Add("BeatmapObjectManager not found");
            if (gameplayCoreSceneSetupData is null) missingObjects.Add("GameplayCoreSceneSetupData not found");
            if (audioTimeSyncController is null) missingObjects.Add("AudioTimeSyncController not found");
            if (relativeScoreAndImmediateRankCounter is null) missingObjects.Add("RelativeScoreAndImmediateRankCounter not found");
            if (gameEnergyCounter is null) missingObjects.Add("GameEnergyCounter not found");

            return missingObjects.Count == 0;
        }

        private bool IsLegacyReplay()
        {
            //Try ang get the legacy ScoreSaber replay class.
            Type legacyReplayPlayer = AccessTools.TypeByName("ScoreSaber.LegacyReplayPlayer");
            if (legacyReplayPlayer == null) return false;

            //Check if replay mode is active.
            PropertyInfo? playbackEnabled = legacyReplayPlayer.GetProperty("playbackEnabled", BindingFlags.Public | BindingFlags.Instance);

            //Check if an instance of the legacy replay player exists.
            UnityEngine.Object replayPlayer = Resources.FindObjectsOfTypeAll(legacyReplayPlayer).FirstOrDefault();

            //If all of the above objects aren't null, return the value of playbackEnabled, otherwise return false.
            if (legacyReplayPlayer != null && playbackEnabled != null && replayPlayer != null) return (bool)playbackEnabled.GetValue(replayPlayer);
            return false;
        }

        //This should be logged as an error as there is currently no reason as to why the script should stop early, unless required objects are not found.
        private void EarlyDispose(string reason)
        {
            Plugin.Logger.Error("MapEvents quit early. Reason: " + reason);
            Dispose();
        }

        public void Dispose()
        {
            #region Unsubscribe from events
            timer.Elapsed -= TimerElapsedEvent;

            beatmapObjectManager.noteWasMissedEvent -= NoteWasMissedEvent;

            gameEnergyCounter.gameEnergyDidChangeEvent -= EnergyDidChangeEvent;

            if (scoreController is not null && multiplayerController is not null) //In a multiplayer lobby
            {
                scoreController.scoreDidChangeEvent -= ScoreDidChangeEvent;

                multiplayerController.stateChangedEvent -= MultiplayerController_stateChangedEvent;
            }
            else if (IsLegacyReplay() && relativeScoreAndImmediateRankCounter is not null) //In a legacy replay.
            {
                relativeScoreAndImmediateRankCounter.relativeScoreOrImmediateRankDidChangeEvent -= RelativeScoreOrImmediateRankDidChangeEvent;
            }
            else if (scoreController is not null && pauseController is not null && standardLevelGameplayManager is not null) //Singleplayer/New replay.
            {
                scoreController.scoreDidChangeEvent -= ScoreDidChangeEvent; //In replay mode this does not fire so 'RelativeScoreOrImmediateRankDidChangeEvent' will read from the UI

                pauseController.didPauseEvent -= LevelPausedEvent;
                pauseController.didResumeEvent -= LevelUnpausedEvent;
                pauseController.didReturnToMenuEvent -= LevelQuitEvent;

                standardLevelGameplayManager.levelFailedEvent -= LevelFailedEvent;
                standardLevelGameplayManager.levelFinishedEvent -= LevelFinishedEvent;
            }
            #endregion

            timer.Stop();
            MapData.Instance.InLevel = false;
            MapData.Instance.Send();
        }

        public void LevelLoaded()
        {
            PlayerData playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault().playerData;
            BeatmapLevel levelData = gameplayCoreSceneSetupData.beatmapLevel;
            var levelBasicData = gameplayCoreSceneSetupData.beatmapBasicData;
            bool isCustomLevel = true;
            string? mapHash = null;
            string? levelId = levelData.levelID;
            try { mapHash = levelData.levelID.Split('_')[2]; }
            catch { isCustomLevel = false; }
            isCustomLevel = isCustomLevel && mapHash != null && mapHash.Length == 40;

            var beatmapKey = gameplayCoreSceneSetupData.beatmapKey;
            SongCore.Data.ExtraSongData.DifficultyData? difficultyData = SongCore.Collections.RetrieveDifficultyData(levelData, beatmapKey);

            MapData.Instance.LevelID = isCustomLevel ? null : levelId;
            MapData.Instance.Hash = isCustomLevel ? mapHash : null;
            MapData.Instance.ContentRating = levelData.contentRating.ToString("g");
            MapData.Instance.SongName = levelData.songName;
            MapData.Instance.SongSubName = levelData.songSubName;
            MapData.Instance.SongAuthor = levelData.songAuthorName;

            List<string> mappersList = SelectLeastEmptyArryAsList(levelBasicData.mappers, levelData.allMappers);
            List<string> lightersList = SelectLeastEmptyArryAsList(levelBasicData.lighters, levelData.allLighters);
#pragma warning disable CS0618 // Type or member is obsolete
            MapData.Instance.Mapper = GetContributorsString(mappersList, lightersList);
#pragma warning restore CS0618 // Type or member is obsolete
            MapData.Instance.Mappers = mappersList;
            MapData.Instance.Lighters = lightersList;
            MapData.Instance.BPM = Convert.ToInt32(Math.Round(levelData.beatsPerMinute));
            MapData.Instance.Duration = Convert.ToInt32(Math.Round(audioTimeSyncController.songLength));
            PlayerLevelStatsData playerLevelStats = playerData.GetOrCreatePlayerLevelStatsData(beatmapKey);
            MapData.Instance.PreviousRecord = playerLevelStats.highScore;
            var mapTypeName = beatmapKey.beatmapCharacteristic.serializedName;
            MapData.Instance.MapType = mapTypeName;
            MapData.Instance.Environment = gameplayCoreSceneSetupData.targetEnvironmentInfo.serializedName;
            MapData.Instance.Difficulty = beatmapKey.difficulty.ToString("g");
            MapData.Instance.NJS = levelBasicData.noteJumpMovementSpeed;
            MapData.Instance.CustomDifficultyLabel = difficultyData?._difficultyLabel ?? null;
            MapData.Instance.ColorScheme = new SColorScheme
            {
                SaberAColor = new SRGBAColor(gameplayCoreSceneSetupData.colorScheme.saberAColor),
                SaberBColor = new SRGBAColor(gameplayCoreSceneSetupData.colorScheme.saberBColor),
                ObstaclesColor = new SRGBAColor(gameplayCoreSceneSetupData.colorScheme.obstaclesColor),
                EnvironmentColor0 = new SRGBAColor(gameplayCoreSceneSetupData.colorScheme.environmentColor0),
                EnvironmentColor1 = new SRGBAColor(gameplayCoreSceneSetupData.colorScheme.environmentColor1),
                EnvironmentColor0Boost = new SRGBAColor(gameplayCoreSceneSetupData.colorScheme.environmentColor0Boost),
                EnvironmentColor1Boost = new SRGBAColor(gameplayCoreSceneSetupData.colorScheme.environmentColor1Boost),
            };

            if (isCustomLevel)
            {
                void SetSongDetails()
                {
                    if (songDetailsCache!.songs.FindByHash(mapHash, out Song song))
                    {
                        MapCharacteristic mapType;
                        switch (mapTypeName)
                        {
                            case "Degree360":
                                mapType = MapCharacteristic.ThreeSixtyDegree;
                                break;
                            case "Degree90":
                                mapType = MapCharacteristic.NinetyDegree;
                                break;
                            default:
                                if (!Enum.TryParse(
                                    mapTypeName,
                                    out mapType
                                )) return;
                                break;
                        }

                        if (song.GetDifficulty(
                            out SongDifficulty difficulty,
                            (MapDifficulty)beatmapKey.difficulty,
                            mapType
                        ))
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            MapData.Instance.PP = difficulty.approximatePpValue;
                            MapData.Instance.Star = difficulty.stars;
#pragma warning restore CS0618 // Type or member is obsolete
                            MapData.Instance.RankedState = MapRankedState(difficulty);
                            MapData.Instance.Rating = difficulty.song.rating * 100;
                            MapData.Instance.Send();
                        }
                    }
                }

                if (songDetailsCache == null)
                {
                    SongDetails.Init().ContinueWith((task) =>
                    {
                        if (task.Result == null) { return; }
                        songDetailsCache = task.Result;
                        SetSongDetails();
                    });
                }
                else SetSongDetails();

                if (mapHash != null)
                {
                    beatSaver.BeatmapByHash(mapHash).ContinueWith((task) =>
                    {
                        if (task.Result != null)
                        {
                            MapData.Instance.BSRKey = task.Result.ID;
                            BeatSaverSharp.Models.BeatmapVersion? mapDetails = null;
                            try { mapDetails = task.Result.Versions.First(map => map.Hash.ToLower() == mapHash.ToLower()); }
                            catch (Exception ex) { Plugin.Logger.Error(ex); }
                            MapData.Instance.CoverImage = mapDetails?.CoverURL ?? null;
                        }
                        else
                        {
                            MapData.Instance.BSRKey = null;
                            MapData.Instance.CoverImage = null;
                        }
                        MapData.Instance.Send();
                    });
                }
            }
            if (MapData.Instance.CoverImage == null)
            {
                TrySetCoverImageFromLevelData(levelData);
            }

            if (MapData.Instance.Hash != previousHash) MapData.Instance.PreviousBSR = previousBSRKey;

            #region Modifiers
            //I ideally really need to come back to this and rewrite it all over as it currently is just really repetitive and I know I could clean it up.
            MapData.Instance.Modifiers.NoFailOn0Energy = gameplayCoreSceneSetupData.gameplayModifiers.noFailOn0Energy;
            MapData.Instance.Modifiers.OneLife = gameplayCoreSceneSetupData.gameplayModifiers.instaFail;
            MapData.Instance.Modifiers.FourLives = gameplayCoreSceneSetupData.gameplayModifiers.energyType == GameplayModifiers.EnergyType.Battery;
            MapData.Instance.Modifiers.NoBombs = gameplayCoreSceneSetupData.gameplayModifiers.noBombs;
            MapData.Instance.Modifiers.NoWalls = gameplayCoreSceneSetupData.gameplayModifiers.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles;
            MapData.Instance.Modifiers.NoArrows = gameplayCoreSceneSetupData.gameplayModifiers.noArrows;
            MapData.Instance.Modifiers.GhostNotes = gameplayCoreSceneSetupData.gameplayModifiers.ghostNotes;
            MapData.Instance.Modifiers.DisappearingArrows = gameplayCoreSceneSetupData.gameplayModifiers.disappearingArrows;
            MapData.Instance.Modifiers.SmallNotes = gameplayCoreSceneSetupData.gameplayModifiers.smallCubes;
            MapData.Instance.Modifiers.ProMode = gameplayCoreSceneSetupData.gameplayModifiers.proMode;
            MapData.Instance.Modifiers.StrictAngles = gameplayCoreSceneSetupData.gameplayModifiers.strictAngles;
            MapData.Instance.Modifiers.ZenMode = gameplayCoreSceneSetupData.gameplayModifiers.zenMode;
            MapData.Instance.Modifiers.SlowerSong = gameplayCoreSceneSetupData.gameplayModifiers.songSpeedMul == 0.85f;
            MapData.Instance.Modifiers.FasterSong = gameplayCoreSceneSetupData.gameplayModifiers.songSpeedMul == 1.2f;
            MapData.Instance.Modifiers.SuperFastSong = gameplayCoreSceneSetupData.gameplayModifiers.songSpeedMul == 1.5f;

            //if (MapData.Instance.Modifiers.NoFailOn0Energy) MapData.Instance.ModifiersMultiplier += (int)EModifiers.NoFailOn0Energy / 100.0f;
            if (MapData.Instance.Modifiers.OneLife) MapData.Instance.ModifiersMultiplier += (int)EModifiers.OneLife / 100.0f;
            if (MapData.Instance.Modifiers.FourLives) MapData.Instance.ModifiersMultiplier += (int)EModifiers.FourLives / 100.0f;
            if (MapData.Instance.Modifiers.NoBombs) MapData.Instance.ModifiersMultiplier += (int)EModifiers.NoBombs / 100.0f;
            if (MapData.Instance.Modifiers.NoWalls) MapData.Instance.ModifiersMultiplier += (int)EModifiers.NoWalls / 100.0f;
            if (MapData.Instance.Modifiers.NoArrows) MapData.Instance.ModifiersMultiplier += (int)EModifiers.NoArrows / 100.0f;
            if (MapData.Instance.Modifiers.GhostNotes) MapData.Instance.ModifiersMultiplier += (int)EModifiers.GhostNotes / 100.0f;
            if (MapData.Instance.Modifiers.DisappearingArrows) MapData.Instance.ModifiersMultiplier += (int)EModifiers.DisappearingArrows / 100.0f;
            if (MapData.Instance.Modifiers.SmallNotes) MapData.Instance.ModifiersMultiplier += (int)EModifiers.SmallNotes / 100.0f;
            if (MapData.Instance.Modifiers.ProMode) MapData.Instance.ModifiersMultiplier += (int)EModifiers.ProMode / 100.0f;
            if (MapData.Instance.Modifiers.StrictAngles) MapData.Instance.ModifiersMultiplier += (int)EModifiers.StrictAngles / 100.0f;
            if (MapData.Instance.Modifiers.ZenMode) MapData.Instance.ModifiersMultiplier += (int)EModifiers.ZenMode / 100.0f;
            if (MapData.Instance.Modifiers.SlowerSong) MapData.Instance.ModifiersMultiplier += (int)EModifiers.SlowerSong / 100.0f;
            if (MapData.Instance.Modifiers.FasterSong) MapData.Instance.ModifiersMultiplier += (int)EModifiers.FasterSong / 100.0f;
            if (MapData.Instance.Modifiers.SuperFastSong) MapData.Instance.ModifiersMultiplier += (int)EModifiers.SuperFastSong / 100.0f;
            #endregion

            MapData.Instance.PracticeMode = gameplayCoreSceneSetupData.practiceSettings != null;
            MapData.Instance.PracticeModeModifiers.SongSpeedMul = MapData.Instance.PracticeMode ? gameplayCoreSceneSetupData!.practiceSettings!.songSpeedMul : 1.0f;
            MapData.Instance.PracticeModeModifiers.StartInAdvanceAndClearNotes = MapData.Instance.PracticeMode && gameplayCoreSceneSetupData!.practiceSettings!.startInAdvanceAndClearNotes;
            MapData.Instance.PracticeModeModifiers.SongStartTime = MapData.Instance.PracticeMode ? gameplayCoreSceneSetupData!.practiceSettings!.startSongTime : 0.0f;

            timer.Elapsed += TimerElapsedEvent;
            beatmapObjectManager.noteWasCutEvent += NoteWasCutEvent;
            beatmapObjectManager.noteWasMissedEvent += NoteWasMissedEvent;
            gameEnergyCounter.gameEnergyDidChangeEvent += EnergyDidChangeEvent;

            MapData.Instance.InLevel = true;
            timer.Start();

            MapData.Instance.Send();
            LiveData.Instance.Send();
        }

        private string GetContributorsString(List<string> mappersList, List<string> lightersList)
        {
            // Approach shamelessly copied from SongCore
            // https://github.com/Kylemc1413/SongCore/blob/03d5a708b959107190442778043ba3653bce09ff/source/SongCore/HarmonyPatches/LevelSelectionPatch.cs#L16
            return mappersList.Concat(lightersList).Join();
        }

        private List<string> SelectLeastEmptyArryAsList(string[] arr1, string[] arr2)
        {
            string[] dataSource = arr1.Length > 0 ? arr1 : arr2;
            return dataSource.ToList();
        }

        private SRankedState MapRankedState(SongDifficulty difficulty)
        {

            var rankedStates = difficulty.song.rankedStates;
            var ssRanked = (rankedStates & RankedStates.ScoresaberRanked) != 0;
            var ssQualified = (rankedStates & RankedStates.ScoresaberQualified) != 0;
            var blRanked = (rankedStates & RankedStates.BeatleaderRanked) != 0;
            var blQualified = (rankedStates & RankedStates.BeatleaderQualified) != 0;
            return new SRankedState
            {
                ScoresaberQualified = ssQualified,
                BeatleaderQualified = blQualified,
                ScoresaberRanked = ssRanked,
                BeatleaderRanked = blRanked,
                ScoresaberStars = difficulty.stars,
                BeatleaderStars = difficulty.starsBeatleader,
            };
        }

        /// <summary>
        /// Creates the cover image data URL for songs whose cover image cannot be loaded from BeatSaver
        ///
        /// Credit to https://github.com/ReadieFur/BSDataPuller/issues/28 which was further based on
        /// https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
        /// Modified to correctly handle texture atlases
        /// </summary>
        /// <param name="levelData"></param>
        private async void TrySetCoverImageFromLevelData(BeatmapLevel levelData)
        {
            try
            {
                var coverImageSprite = await levelData.previewMediaData.GetCoverSpriteAsync();
                if (coverImageSprite != null)
                {
                    var activeRenderTexture = RenderTexture.active;
                    var texture = coverImageSprite.texture;
                    var temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
                    try
                    {
                        Graphics.Blit(texture, temporary);
                        RenderTexture.active = temporary;

                        try
                        {
                            var textureRect = coverImageSprite.textureRect;

                            var cover = new Texture2D((int)textureRect.width, (int)textureRect.height);
                            cover.ReadPixels(
                                textureRect,
                                0,
                                0
                            );
                            cover.Apply();

                            MapData.Instance.CoverImage = "data:image/png;base64," + Convert.ToBase64String(ImageConversion.EncodeToPNG(cover));
                        }
                        finally
                        {
                            RenderTexture.active = activeRenderTexture;
                        }
                    }
                    finally
                    {
                        RenderTexture.ReleaseTemporary(temporary);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.Error(e.Message + "\n" + e.StackTrace);
                MapData.Instance.CoverImage = null;
            }
        }

        private void TimerElapsedEvent(object sender, ElapsedEventArgs ev)
        {
            LiveData.Instance.TimeElapsed = (int)Math.Round(audioTimeSyncController.songTime);
            if (Math.Truncate(DateTime.Now.Subtract(LiveData.Instance.lastSendTime).TotalMilliseconds) > 950 / MapData.Instance.PracticeModeModifiers.SongSpeedMul)
                LiveData.Instance.Send(ELiveDataEventTriggers.TimerElapsed);
        }

        private void LevelPausedEvent()
        {
            timer.Stop();
            MapData.Instance.LevelPaused = true;
            MapData.Instance.Send();
        }

        private void LevelUnpausedEvent()
        {
            timer.Start();
            MapData.Instance.LevelPaused = false;
            MapData.Instance.Send();
        }

        private void MultiplayerController_stateChangedEvent(MultiplayerController.State multiplayerState)
        {
            if (multiplayerState == MultiplayerController.State.Gameplay) LevelLoaded();
            else if (multiplayerState == MultiplayerController.State.Finished) LevelFinishedEvent();
        }

        private void EnergyDidChangeEvent(float health)
        {
            health *= 100;
            if (!MapData.Instance.LevelFailed && MapData.Instance.Modifiers.NoFailOn0Energy && health <= 0)
            {
                //This will only ever be reached at most once per level.
                MapData.Instance.LevelFailed = true;
                MapData.Instance.ModifiersMultiplier += (float)EModifiers.NoFailOn0Energy / 100.0f;
                MapData.Instance.Send();
            }
            if (health < LiveData.Instance.PlayerHealth) LiveData.Instance.Combo = 0;
            LiveData.Instance.PlayerHealth = health;
            LiveData.Instance.Send(ELiveDataEventTriggers.EnergyChange);
        }

        private void NoteWasMissedEvent(NoteController noteController)
        {
            if (noteController.noteData.colorType != ColorType.None)
            {
                LiveData.Instance.Combo = 0;
                LiveData.Instance.FullCombo = false;
                LiveData.Instance.NotesSpawned++;
                LiveData.Instance.Misses++;
                LiveData.Instance.ColorType = noteController.noteData.colorType;
                LiveData.Instance.CutDirection = noteController.noteData.cutDirection;
                LiveData.Instance.Send(ELiveDataEventTriggers.NoteMissed);
            }
        }

        private void LevelQuitEvent() => MapData.Instance.LevelQuit = true;

        private void LevelFailedEvent() => MapData.Instance.LevelFailed = true;

        private void LevelFinishedEvent() => MapData.Instance.LevelFinished = true;

        //For legacy replay mode.
        private void RelativeScoreOrImmediateRankDidChangeEvent()
        {
            TextMeshProUGUI textMeshProUGUI = scoreUIController!.GetField<TextMeshProUGUI, ScoreUIController>("_scoreText");
            LiveData.Instance.Score = int.Parse(textMeshProUGUI.text.Replace(" ", ""));
            LiveData.Instance.ScoreWithMultipliers = ScoreModel.GetModifiedScoreForGameplayModifiersScoreMultiplier(LiveData.Instance.Score, MapData.Instance.ModifiersMultiplier);
            LiveData.Instance.MaxScoreWithMultipliers = ScoreModel.GetModifiedScoreForGameplayModifiersScoreMultiplier(LiveData.Instance.MaxScore, MapData.Instance.ModifiersMultiplier);
            SetRankAndAccuracy();
        }

        //For all other modes.
        private void ScoreDidChangeEvent(int score, int scoreWithMultipliers)
        {
            LiveData.Instance.Score = score;
            LiveData.Instance.ScoreWithMultipliers = scoreWithMultipliers;
            SetRankAndAccuracy();
        }

        private void SetRankAndAccuracy()
        {
            //Figure out why the accuracy is behind by 1 note every time.
            LiveData.Instance.Accuracy = relativeScoreAndImmediateRankCounter.relativeScore * 100;
            LiveData.Instance.Rank = relativeScoreAndImmediateRankCounter.immediateRank.ToString();
            LiveData.Instance.Send(ELiveDataEventTriggers.ScoreChange);
        }

        private void NoteWasCutEvent(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            LiveData.Instance.ColorType = noteController.noteData.colorType;
            if (!noteCutInfo.allIsOK)
            {
                LiveData.Instance.FullCombo = false;
                LiveData.Instance.Combo = 0;
                if (noteCutInfo.noteData.colorType != ColorType.None)
                {
                    LiveData.Instance.Misses++;
                    LiveData.Instance.NotesSpawned++;
                }
                return;
            }
            LiveData.Instance.NotesSpawned++;
            //Score is updated by the Harmony patch.
            //Data is sent in SetRankAndAccuracy().
        }
    }
}
