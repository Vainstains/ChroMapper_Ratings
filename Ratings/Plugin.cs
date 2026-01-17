using beatleader_analyzer;
using beatleader_analyzer.BeatmapScanner.Data;
using beatleader_parser;
using Newtonsoft.Json;
using Parser.Map;
using Parser.Map.Difficulty.V3.Base;
using Ratings.AccAi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Beatmap.Base;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Ratings.AccAi.PerNote;
using Object = UnityEngine.Object;
using HarmonyLib;

namespace Ratings
{
    [Plugin("Ratings")]
    public class Plugin
    {
        private UI _ui;

        private const int EditorSceneBuildIndex = 3;
        private const int PollIntervalMilliseconds = 1000;
        private const int QueueReloadDebounceIntervalMilliseconds = 250;

        private static readonly Parse Parser = new();
        private static readonly Analyze Analyzer = new();
        private static readonly AccRating AccRating = new();
        private static readonly Curve Curve = new();

        private readonly PerNote PerNote = new();
        private readonly Full Full = new();

        private BeatSaberSongContainer? _beatSaberSongContainer;
        private NoteGridContainer? _noteGridContainer;
        private AudioTimeSyncController? _audioTimeSyncController;
        public SongTimelineController? _songTimeLineController;
        private MapEditorUI? _mapEditorUI;

        private List<beatleader_analyzer.BeatmapScanner.Data.Ratings> AnalyzerData = new();
        private List<NoteAcc> AccAiData = new();

        public double PredictedAcc = 0f;
        public double Acc = 0f;
        public double Tech = 0f;
        public double Pass = 0f;
        public double Star = 0f;

        public Config Config = new();

        private bool _initialized = false;
        private Scene _currentScene;
        private TriangleVisualizer m_triangleVisualizer;

        private static Plugin m_instance;
        private static bool _reloadQueued;
        private static DateTime _lastSaveUtc;
        public static async void QueueReload()
        {
            if (m_instance == null)
                return;
            
            if (m_instance._currentScene.buildIndex != EditorSceneBuildIndex)
                return;

            _lastSaveUtc = DateTime.UtcNow;

            if (_reloadQueued)
                return;

            _reloadQueued = true;

            await Task.Delay(QueueReloadDebounceIntervalMilliseconds);
            
            if ((DateTime.UtcNow - _lastSaveUtc).TotalMilliseconds < QueueReloadDebounceIntervalMilliseconds)
            {
                _reloadQueued = false;
                return;
            }
            
            if (!m_instance._initialized ||
                m_instance._noteGridContainer == null ||
                m_instance._beatSaberSongContainer == null)
            {
                _reloadQueued = false;
                return;
            }

            try
            {
                m_instance.Reload();
            }
            catch (Exception ex)
            {
                // lmao we dont care
            }
            finally
            {
                _reloadQueued = false;
            }
        }
        
        [Init]
        private void Init()
        {
            var harmony = new Harmony("com.vainstains.ratings");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            
            m_instance = this;
            
            SceneManager.sceneLoaded += SceneLoaded;
            LoadedDifficultySelectController.LoadedDifficultyChangedEvent += LoadedDifficultyChanged;
            _ui = new UI(this);
            LoadConfigFile();
        }

        private void LoadConfigFile()
        {
            string path = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), "Ratings.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                Config = JsonConvert.DeserializeObject<Config>(json);
            }
        }

        public void SaveConfigFile()
        {
            string path = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), "Ratings.json");
            string json = JsonConvert.SerializeObject(Config);
            File.WriteAllText(path, json);
        }

        private void LoadedDifficultyChanged()
        {
            SceneLoaded(_currentScene, LoadSceneMode.Single);
        }

        private async void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            _initialized = false;
            _currentScene = arg0;

            if (_currentScene.buildIndex == EditorSceneBuildIndex)
            {
                _mapEditorUI = null;
                _noteGridContainer = null;
                _beatSaberSongContainer = null;
                _audioTimeSyncController = null;
                _songTimeLineController = null;

                await FindObject();

                BeatmapV3 map = Parser.TryLoadPath(_beatSaberSongContainer.Info.Directory);
                string characteristic = _beatSaberSongContainer.MapDifficultyInfo.Characteristic;
                string difficulty = _beatSaberSongContainer.MapDifficultyInfo.Difficulty;
                DifficultyV3 diff = map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

                _Difficultybeatmaps difficultyBeatmap = map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == characteristic)._difficultyBeatmaps.FirstOrDefault(x => x._difficulty == difficulty);
                int diffCount = map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == characteristic)._difficultyBeatmaps.Count();
                
                if (_noteGridContainer.MapObjects.Count >= 20)
                {
                    AnalyzerData = Analyzer.GetRating(diff, characteristic, difficulty, map.Info._beatsPerMinute, Config.Timescale);
                    var data = AnalyzerData.FirstOrDefault();
                    if (data != null)
                    {
                        Tech = data.Tech * 10;
                        Pass = data.Pass;
                        PredictedAcc = Full.GetAIAcc(diff, _beatSaberSongContainer.Info.BeatsPerMinute, Config.Timescale);
                        Acc = AccRating.GetRating(PredictedAcc, Pass, Tech);
                        Acc *= data.Nerf;
                        List<Point> pointList = Curve.GetCurve(PredictedAcc, Acc);
                        Star = Curve.ToStars(Config.StarAccuracy, Acc, Pass, Tech, pointList);
                    }
                    AccAiData = PerNote.PredictHitsForMapNotes(diff, _beatSaberSongContainer.Info.BeatsPerMinute, _beatSaberSongContainer.MapDifficultyInfo.NoteJumpSpeed, Config.Timescale);
                    _ui.ApplyNewValues();
                    _initialized = true;
                }
                else
                {
                    Debug.LogError("Ratings require 20 or more notes to analyze the map. Current note count: " + _noteGridContainer.MapObjects.Count);
                }

                _audioTimeSyncController.TimeChanged += OnTimeChanged;

                _ui.AddMenu(_mapEditorUI);
                
                if (_mapEditorUI != null)
                {
                    m_triangleVisualizer = _ui.AddTriangleVisualizer(_mapEditorUI);
                    m_triangleVisualizer.SetupLabels();
                }
            }
        }

        public void Reload()
        {
            _initialized = false;

            BeatmapV3 map = Parser.TryLoadPath(_beatSaberSongContainer.Info.Directory);
            string characteristic = _beatSaberSongContainer.MapDifficultyInfo.Characteristic;
            string difficulty = _beatSaberSongContainer.MapDifficultyInfo.Difficulty;
            DifficultyV3 diff = map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

            _Difficultybeatmaps difficultyBeatmap = map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == characteristic)._difficultyBeatmaps.FirstOrDefault(x => x._difficulty == difficulty);
            int diffCount = map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == characteristic)._difficultyBeatmaps.Count();

            if (_noteGridContainer.MapObjects.Count >= 20)
            {
                AnalyzerData = Analyzer.GetRating(diff, characteristic, difficulty, map.Info._beatsPerMinute, Config.Timescale);
                var data = AnalyzerData.FirstOrDefault();
                if (data != null)
                {
                    Tech = data.Tech * 10;
                    Pass = data.Pass;
                    PredictedAcc = Full.GetAIAcc(diff, _beatSaberSongContainer.Info.BeatsPerMinute, Config.Timescale);
                    Acc = AccRating.GetRating(PredictedAcc, Pass, Tech);
                    Acc *= data.Nerf;
                    List<Point> pointList = Curve.GetCurve(PredictedAcc, Acc);
                    Star = Curve.ToStars(Config.StarAccuracy, Acc, Pass, Tech, pointList);
                }
                AccAiData = PerNote.PredictHitsForMapNotes(diff, _beatSaberSongContainer.Info.BeatsPerMinute, _beatSaberSongContainer.MapDifficultyInfo.NoteJumpSpeed, Config.Timescale);
                _ui.ApplyNewValues();
                _initialized = true;
                OnTimeChanged();
            }
            else
            {
                Debug.LogError("Ratings require 20 or more notes to analyze the map. Current note count: " + _noteGridContainer.MapObjects.Count);
            }
        }

        private async Task FindObject()
        {
            while (_noteGridContainer == null || _beatSaberSongContainer == null || _audioTimeSyncController == null || _songTimeLineController == null || _mapEditorUI == null)
            {
                await Task.Delay(PollIntervalMilliseconds);
                _noteGridContainer = _noteGridContainer ?? Object.FindObjectOfType<NoteGridContainer>();
                _beatSaberSongContainer = _beatSaberSongContainer ?? Object.FindObjectOfType<BeatSaberSongContainer>();
                _audioTimeSyncController = _audioTimeSyncController ?? Object.FindObjectOfType<AudioTimeSyncController>();
                _songTimeLineController = _songTimeLineController ?? Object.FindObjectOfType<SongTimelineController>();
                _mapEditorUI = _mapEditorUI ?? Object.FindObjectOfType<MapEditorUI>();
            }
        }
        
        public struct InterpolatedRatings
        {
            public double Pass;
            public double Tech;
            public double Acc;
            public double AccRating;
            public double Stars;
        }
        
        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }
        
        private static double Interpolate(
            double x0, double y0,
            double x1, double y1,
            double x)
        {
            if (Math.Abs(x1 - x0) < 1e-6)
                return y0;

            double t = (x - x0) / (x1 - x0);
            return Lerp(y0, y1, t);
        }
        
        private static (T before, T after) FindNeighbors<T>(
            IReadOnlyList<T> list,
            Func<T, double> timeSelector,
            double time)
        {
            if (list == null || list.Count == 0)
                return (default, default);
            
            double firstTime = timeSelector(list[0]);
            if (time <= firstTime)
                return (list[0], list[0]);
            
            double lastTime = timeSelector(list[list.Count - 1]);
            if (time >= lastTime)
                return (list[list.Count - 1], list[list.Count - 1]);
            
            for (int i = 1; i < list.Count; i++)
            {
                double t = timeSelector(list[i]);

                if (t >= time)
                    return (list[i - 1], list[i]);
            }
            
            return (list[list.Count - 1], list[list.Count - 1]);
        }
        
        private InterpolatedRatings GetRatingsAtTime(
            double bpmTime,
            double secondsTime,
            beatleader_analyzer.BeatmapScanner.Data.Ratings analyzerData,
            IReadOnlyList<NoteAcc> accData)
        {
            if (analyzerData?.PerSwing == null || accData == null)
                return default;

            var swings = analyzerData.PerSwing;
            if (swings.Count < 2 || accData.Count < 2)
                return default;
            
            var (s0, s1) = FindNeighbors(swings, x => x.Time, bpmTime);
            var (a0, a1) = FindNeighbors(accData, x => x.time, secondsTime);

            if (s0 == null || s1 == null || a0 == null || a1 == null)
                return default;
            
            double pass = Interpolate(s0.Time, s0.Pass, s1.Time, s1.Pass, bpmTime);
            double tech = Interpolate(s0.Time, s0.Tech, s1.Time, s1.Tech, bpmTime);
            double acc  = Interpolate(a0.time, a0.acc,  a1.time, a1.acc,  secondsTime);
            
            double accRating = AccRating.GetRating(acc, pass, tech);
            var curve = Curve.GetCurve(acc, accRating);
            double stars = Curve.ToStars(
                Config.StarAccuracy,
                accRating,
                pass,
                tech,
                curve
            );

            return new InterpolatedRatings
            {
                Pass = pass,
                Tech = tech,
                Acc = acc,
                AccRating = accRating,
                Stars = stars
            };
        }
        
        private static double Gaussian(double x, double sigma)
        {
            return Math.Exp(-(x * x) / (2.0 * sigma * sigma));
        }


        private void OnTimeChanged()
        {
            if (!Config.Enabled || !_initialized)
                return;

            double centerBpmTime = _audioTimeSyncController.CurrentSongBpmTime;
            double centerSeconds = _audioTimeSyncController.CurrentSeconds;

            var analyzerData = AnalyzerData.FirstOrDefault();
            if (analyzerData == null)
                return;

            const double windowRadius = 8.0;
            const double sampleStep = 0.1;
            const double sigma = 2.5;

            double weightedPass = 0.0;
            double weightedTech = 0.0;
            double weightedAcc = 0.0;
            double weightSum = 0.0;

            for (double offset = -windowRadius; offset <= windowRadius; offset += sampleStep)
            {
                double sampleSeconds = centerSeconds + offset;
                double sampleBpmTime = centerBpmTime + offset;

                var ratings = GetRatingsAtTime(
                    sampleBpmTime,
                    sampleSeconds,
                    analyzerData,
                    AccAiData
                );

                if (ratings.Pass == 0 && ratings.Tech == 0 && ratings.Acc == 0)
                    continue;

                double w = Gaussian(offset, sigma);

                weightedPass += ratings.Pass * w;
                weightedTech += ratings.Tech * w;
                weightedAcc += ratings.Acc * w;
                weightSum += w;
            }

            if (weightSum <= 1e-6)
            {
                m_triangleVisualizer.UpdateRatings(0, 0, 0, 0);
                return;
            }

            double avgPass = weightedPass / weightSum;
            double avgTech = weightedTech / weightSum;
            double avgAcc = weightedAcc / weightSum;

            double accRating = AccRating.GetRating(avgAcc, avgPass, avgTech);
            var curve = Curve.GetCurve(avgAcc, accRating);
            double stars = Curve.ToStars(
                Config.StarAccuracy,
                accRating,
                avgPass,
                avgTech,
                curve
            );

            m_triangleVisualizer.UpdateRatings((float)avgTech, (float)avgPass, (float)accRating, (float)stars);
        }
    }
    
    [HarmonyPatch(typeof(BaseDifficulty), nameof(BaseDifficulty.Save))]
    class SavingPatch
    {
        static void Postfix()
        {
            Plugin.QueueReload();
        }
    }
}