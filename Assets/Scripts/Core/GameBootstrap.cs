using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BalkanMines
{
    public enum GameState
    {
        Menu,
        Intro,
        Playing,
        Paused,
        Victory,
        Defeat
    }

    /// <summary>Entry point. Builds the menu, levels, player and glues all systems together.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        public Camera Cam;
        public GameUI UI;
        public AudioSynth Audio;
        public Effects Fx;
        public int LevelIndex { get; private set; } = -1;
        public GameState State { get; private set; } = GameState.Menu;

        private GameObject _worldRoot;
        private LevelManager _level;
        private PlayerController _player;
        private MenuOrbit _orbit;
        private bool _victoryBest;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            // Standalone defaults to the "PC" quality level; force it for builds too.
            int pcLevel = System.Array.IndexOf(QualitySettings.names, "PC");
            if (pcLevel >= 0) QualitySettings.SetQualityLevel(pcLevel, true);
            QualitySettings.antiAliasing = 4;
            Debug.Log("BalkanMines quality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] +
                      " pipeline=" + (GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "none") +
                      " shadows=" + QualitySettings.shadows + " shadowDistance=" + QualitySettings.shadowDistance +
                      " renderScale=" + (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp ? urp.renderScale : 0f));
            Time.timeScale = 1f;
            SetupCamera();
            SetupPostProcessing();

            var audioGo = new GameObject("Audio");
            audioGo.transform.SetParent(transform, false);
            Audio = audioGo.AddComponent<AudioSynth>();
            Audio.Init();

            var fxGo = new GameObject("Effects");
            fxGo.transform.SetParent(transform, false);
            Fx = fxGo.AddComponent<Effects>();
            Fx.Init(this);

            var uiGo = new GameObject("Game UI");
            uiGo.transform.SetParent(transform, false);
            UI = uiGo.AddComponent<GameUI>();
            UI.Init(this);

            ShowMainMenu();
        }

        private void SetupCamera()
        {
            Cam = Camera.main;
            if (Cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                Cam = go.AddComponent<Camera>();
            }
            Cam.transform.SetParent(null, true);
            Cam.clearFlags = CameraClearFlags.Skybox;
            Cam.nearClipPlane = 0.05f;
            Cam.farClipPlane = 900f;
            Cam.fieldOfView = 68f;
            Cam.allowHDR = true;
            Cam.allowMSAA = true;
            if (Cam.GetComponent<AudioListener>() == null) Cam.gameObject.AddComponent<AudioListener>();
        }

        private void SetupPostProcessing()
        {
            var data = Cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;

            // A small, serialized-style global volume. Bloom and tonemapping were
            // deliberately left out: those passes triggered a native URP crash in
            // the player build on this machine.
            var volGo = new GameObject("Global Volume");
            volGo.transform.SetParent(transform, false);
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.weight = 1f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.16f;
            vignette.smoothness.value = 0.5f;

            var color = profile.Add<ColorAdjustments>(true);
            color.contrast.value = 6f;
            color.saturation.value = 4f;
            color.postExposure.value = 0.02f;

            volume.profile = profile;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (UI != null && UI.HelpOpen)
                {
                    UI.ShowHelp(false);
                    return;
                }
                if (UI != null && UI.ContactOpen)
                {
                    UI.ShowContact(false);
                    return;
                }
                if (State == GameState.Playing) PauseGame();
                else if (State == GameState.Paused) ResumeGame();
                else if (State == GameState.Victory || State == GameState.Defeat) ReturnToMenu();
            }

            if (State == GameState.Menu)
            {
                if (UI != null && (UI.HelpOpen || UI.ContactOpen)) return;
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) StartLevel(0);
                else if (kb.digit1Key.wasPressedThisFrame) StartLevel(0);
                else if (kb.digit2Key.wasPressedThisFrame) StartLevel(1);
                else if (kb.digit3Key.wasPressedThisFrame) StartLevel(2);
            }
        }

        // ------------------------------------------------------------------
        //  Menu
        // ------------------------------------------------------------------

        public void ShowMainMenu()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            State = GameState.Menu;
            LevelIndex = -1;
            CleanupWorld();
            UI.HideBanner();
            UI.ShowMainMenu();
            UI.HideHud();
            Audio.SetMenuMusic();
            Audio.SetMusicIntensity(1f);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _worldRoot = new GameObject("Main Menu World");
            WorldBuilder.BuildMenu(_worldRoot.transform);
            Cam.transform.SetParent(null, true);
            Cam.transform.position = new Vector3(0f, 8f, -18f);
            Cam.transform.rotation = Quaternion.identity;
            _orbit = Cam.gameObject.AddComponent<MenuOrbit>();
            _orbit.Target = new Vector3(0f, 2.5f, 0f);
            _orbit.Radius = 17f;
            _orbit.Height = 7f;
            _orbit.Speed = 46f;
            _orbit.LookHeight = 3.2f;
        }

        // ------------------------------------------------------------------
        //  Levels
        // ------------------------------------------------------------------

        public void StartLevel(int index)
        {
            if (index < 0 || index >= LevelDefinition.All.Length) return;
            StopAllCoroutines();
            Time.timeScale = 1f;
            LevelIndex = index;
            State = GameState.Intro;
            CleanupWorld();
            UI.HideBanner();

            var def = LevelDefinition.Get(index);
            _worldRoot = new GameObject("Level " + (index + 1) + " - " + def.Country);
            var layout = WorldBuilder.BuildLevel(_worldRoot.transform, def);

            _level = _worldRoot.AddComponent<LevelManager>();
            _level.Build(def, layout, this);

            _player = CreatePlayer(layout.PlayerSpawn, 0f);
            _player.Init(this, _level, Cam, 0f);

            UI.ShowHud();
            UI.ShowBanner(def, index, 1.8f);
            UI.SetCounter(def.Mines, 0);
            UI.SetTimer(0f);
            Audio.SetLevelMusic(index);
            Audio.SetMusicIntensity(0.65f);

            StartCoroutine(IntroRoutine(def, index, layout));
        }

        private PlayerController CreatePlayer(Vector3 spawn, float yaw)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(_worldRoot.transform, false);
            go.transform.position = spawn;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.78f;
            cc.radius = 0.36f;
            cc.center = new Vector3(0f, 0.92f, 0f);
            cc.stepOffset = 0.28f;
            cc.slopeLimit = 52f;
            cc.skinWidth = 0.04f;
            cc.minMoveDistance = 0f;

            var pc = go.AddComponent<PlayerController>();
            Cam.transform.SetParent(go.transform, false);
            Cam.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Cam.transform.localRotation = Quaternion.identity;
            return pc;
        }

        private IEnumerator IntroRoutine(LevelDefinition def, int index, WorldBuilder.Layout layout)
        {
            // Detach the camera for the establishing shot.
            Cam.transform.SetParent(null, true);
            Vector3 start = new Vector3(layout.GridHalf * 0.95f, layout.GridHalf * 1.05f + 7.5f,
                -layout.GridHalf * 1.55f - 4f);
            if (def.Size >= 12) start += new Vector3(0f, 4f, -3f);
            Vector3 lookTarget = new Vector3(0f, 1.5f, 0f);
            Quaternion startRot = Quaternion.LookRotation(lookTarget - start, Vector3.up);

            Cam.transform.position = start;
            Cam.transform.rotation = startRot;

            var playerCam = _player.transform;
            Vector3 end = playerCam.position + Vector3.up * 1.62f;
            Quaternion endRot = Quaternion.LookRotation(playerCam.forward, Vector3.up) * Quaternion.Euler(-21f, 0f, 0f);

            float dur = 2.5f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                Cam.transform.position = Vector3.Lerp(start, end, e);
                Cam.transform.rotation = Quaternion.Slerp(startRot, endRot, e);
                yield return null;
            }

            Cam.transform.SetParent(_player.transform, false);
            _player.SnapCamera();
            State = GameState.Playing;
            _level.BeginTimer();
            _player.SetControl(true);
            UI.ShowToast("SKOČI NA POLJE DA GA OTKRIJEŠ  •  LMB ZA ZASTAVU", 3.4f);
        }

        private void CleanupWorld()
        {
            if (_orbit != null)
            {
                Destroy(_orbit);
                _orbit = null;
            }
            if (Cam != null) Cam.transform.SetParent(null, true);
            if (_worldRoot != null)
            {
                Destroy(_worldRoot);
                _worldRoot = null;
            }
            _level = null;
            _player = null;
        }

        // ------------------------------------------------------------------
        //  Pause & navigation
        // ------------------------------------------------------------------

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            _player.SetControl(false);
            UI.ShowPause(true);
            Audio.SetMusicIntensity(0.25f);
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            UI.ShowPause(false);
            UI.ShowHelp(false);
            Time.timeScale = 1f;
            State = GameState.Playing;
            _player.SetControl(true);
            Audio.SetMusicIntensity(0.65f);
        }

        public void RestartLevel()
        {
            if (LevelIndex >= 0) StartLevel(LevelIndex);
            else ShowMainMenu();
        }

        public void NextLevel()
        {
            if (LevelIndex < 0) { StartLevel(0); return; }
            if (LevelIndex < LevelDefinition.All.Length - 1) StartLevel(LevelIndex + 1);
            else StartLevel(0);
        }

        public void ReturnToMenu()
        {
            ShowMainMenu();
        }

        // ------------------------------------------------------------------
        //  Results
        // ------------------------------------------------------------------

        public void OnVictory(LevelManager level)
        {
            State = GameState.Victory;
            if (_player != null) _player.SetControl(false);
            float time = level.Elapsed;
            string key = "best_time_" + LevelIndex;
            float best = PlayerPrefs.GetFloat(key, -1f);
            _victoryBest = best < 0f || time < best;
            if (_victoryBest)
            {
                PlayerPrefs.SetFloat(key, time);
                PlayerPrefs.Save();
            }

            bool final = LevelIndex >= LevelDefinition.All.Length - 1;
            Audio.SetMusicIntensity(1f);
            Fx.Fireworks(Vector3.zero, final ? 8 : 5);
            Fx.Confetti(new Vector3(0f, 2f, 0f));
            UI.Flash(new Color(1f, 0.9f, 0.5f, 0.45f), 0.8f);
            StartCoroutine(VictorySequence(level, final));
        }

        private IEnumerator VictorySequence(LevelManager level, bool final)
        {
            StartCoroutine(VictoryCamera(level));
            yield return new WaitForSeconds(1.5f);
            UI.ShowVictory(level.Def, LevelIndex, final, level.FlagCount, level.Def.Mines, level.Elapsed, _victoryBest);
        }

        private IEnumerator VictoryCamera(LevelManager level)
        {
            if (Cam.transform.parent != null) Cam.transform.SetParent(null, true);
            Vector3 center = level.transform.position;
            float radius = Mathf.Max(9f, level.GridHalf * 0.75f);
            Vector3 start = Cam.transform.position;
            float angle = Mathf.Atan2(start.z - center.z, start.x - center.x);
            float t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                float e = Mathf.SmoothStep(0f, 1f, t / 0.8f);
                Vector3 target = center + new Vector3(Mathf.Cos(angle) * radius, 8.5f, Mathf.Sin(angle) * radius);
                Cam.transform.position = Vector3.Lerp(start, target, e);
                Cam.transform.LookAt(center + Vector3.up * 2f);
                yield return null;
            }
            float a = angle;
            while (State == GameState.Victory)
            {
                a += Time.deltaTime * 0.16f;
                Cam.transform.position = center + new Vector3(Mathf.Cos(a) * radius, 8.5f + Mathf.Sin(a * 1.7f) * 0.8f,
                    Mathf.Sin(a) * radius);
                Cam.transform.LookAt(center + Vector3.up * 2f);
                yield return null;
            }
        }

        public void OnDefeat(LevelManager level)
        {
            State = GameState.Defeat;
            if (_player != null) _player.SetControl(false);
            UI.Flash(new Color(0.8f, 0.05f, 0.03f, 0.65f), 0.9f);
            StartCoroutine(DefeatSequence(level));
        }

        private IEnumerator DefeatSequence(LevelManager level)
        {
            yield return new WaitForSeconds(1.55f);
            UI.ShowDefeat(level.Def, LevelIndex, level.FlagCount, level.Def.Mines);
        }
    }
}
