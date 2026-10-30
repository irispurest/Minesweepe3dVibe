using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BalkanMines
{
    /// <summary>All HUD, menus, overlays and UI animation. Built entirely in code.</summary>
    public class GameUI : MonoBehaviour
    {
        private GameBootstrap _game;
        private Font _font;

        private Canvas _canvas;
        private GameObject _menuGroup, _hudGroup, _victoryGroup, _defeatGroup, _pauseGroup, _helpGroup, _contactGroup;
        private CanvasGroup _menuCg, _hudCg, _victoryCg, _defeatCg, _pauseCg, _helpCg, _contactCg;
        private RectTransform _menuPanel, _victoryPanel, _defeatPanel, _pausePanel, _helpPanel, _contactPanel;
        private RectTransform _bannerRoot, _toastRoot, _counterPanel;
        private CanvasGroup _bannerCg, _toastCg, _promptCg;
        private Text _hudLevel, _hudSub, _hudCounter, _hudTimer, _hudPrompt, _toastText, _bannerTitle, _bannerSub;
        private Text _victoryTitle, _victorySub, _victoryStats, _defeatTitle, _defeatSub, _menuBest;
        private Text _nextButtonText;
        private Image _flash, _star;
        private GameObject _levelSelect;
        private bool _levelSelectOpen;

        private Coroutine _bannerRoutine, _toastRoutine, _flashRoutine, _menuRoutine, _counterPulse;
        private float _bannerHold;

        private static readonly Color Navy = new Color(0.035f, 0.06f, 0.12f, 0.94f);
        private static readonly Color NavySoft = new Color(0.06f, 0.11f, 0.20f, 0.90f);
        private static readonly Color OffWhite = new Color(0.96f, 0.94f, 0.86f, 1f);
        private static readonly Color Muted = new Color(0.72f, 0.75f, 0.76f, 1f);
        private static readonly Color Gold = new Color(0.95f, 0.78f, 0.24f, 1f);
        private static readonly Color Red = new Color(0.72f, 0.12f, 0.14f, 1f);
        private static readonly Color Blue = new Color(0.10f, 0.24f, 0.55f, 1f);

        public void Init(GameBootstrap game)
        {
            _game = game;
            _font = LP.Font;
            BuildEventSystem();
            BuildCanvas();
            BuildMenu();
            BuildHud();
            BuildVictory();
            BuildDefeat();
            BuildPause();
            BuildHelp();
            BuildContact();
            BuildFlash();
        }

        // ------------------------------------------------------------------
        //  Construction helpers
        // ------------------------------------------------------------------

        private static RectTransform R(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static void Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private Image Img(RectTransform rt, Color color, Texture2D tex = null)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (tex != null)
            {
                img.sprite = LP.MakeSprite(tex);
                img.type = Image.Type.Simple;
            }
            return img;
        }

        private Text Txt(Transform parent, string name, string text, int size, Color color, TextAnchor align,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 box)
        {
            var rt = R(name, parent, anchorMin, anchorMax, pivot, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.55f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        private Button Btn(Transform parent, string label, Vector2 pos, Vector2 size, Color color, Color textColor,
            UnityEngine.Events.UnityAction action, int fontSize = 28)
        {
            var rt = R("Btn_" + label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            btn.onClick.AddListener(() =>
            {
                if (_game != null && _game.Audio != null) _game.Audio.PlayClick();
                action?.Invoke();
            });

            var accent = R("Accent", rt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(7f, 0f));
            Stretch(accent, 0f, 0f, 0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.sizeDelta = new Vector2(7f, 0f);
            var ai = accent.gameObject.AddComponent<Image>();
            ai.color = new Color(Red.r + 0.18f, Red.g + 0.10f, Red.b + 0.10f, 1f);
            ai.raycastTarget = false;

            var t = Txt(rt, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch((RectTransform)t.transform, 22f, 0f, 14f, 0f);
            return btn;
        }

        private void BuildEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(transform, false);
        }

        private void BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private GameObject Group(string name, CanvasGroup cg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(_canvas.transform, false);
            Stretch((RectTransform)go.transform);
            var c = go.GetComponent<CanvasGroup>();
            c.alpha = cg == null ? 1f : 0f;
            return go;
        }

        // ------------------------------------------------------------------
        //  Main menu
        // ------------------------------------------------------------------

        private void BuildMenu()
        {
            _menuGroup = Group("Menu", null);
            _menuCg = _menuGroup.GetComponent<CanvasGroup>();

            var backdrop = R("Backdrop", _menuGroup.transform, Vector2.zero, Vector2.one,
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(backdrop);
            backdrop.anchorMax = new Vector2(0.46f, 1f);
            backdrop.offsetMax = Vector2.zero;
            var bgTex = LP.GradientTexture("menu_gradient", 16, 256,
                new Color(0.015f, 0.030f, 0.060f, 0.95f), new Color(0.035f, 0.085f, 0.16f, 0.72f));
            Img(backdrop, Color.white, bgTex);

            var panel = R("MenuPanel", _menuGroup.transform, new Vector2(0f, 0f), new Vector2(0.46f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            _menuPanel = panel;

            // Yugoslav tricolour strip down the right edge.
            var strip = R("Tricolour", panel, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), Vector2.zero, new Vector2(14f, 0f));
            Stretch(strip);
            strip.anchorMin = new Vector2(1f, 0f);
            strip.anchorMax = new Vector2(1f, 1f);
            strip.sizeDelta = new Vector2(14f, 0f);
            AddBand(strip, 0f, 1f / 3f, Blue);
            AddBand(strip, 1f / 3f, 2f / 3f, new Color(0.96f, 0.95f, 0.90f));
            AddBand(strip, 2f / 3f, 1f, Red);

            // Red star mark.
            var starRt = R("Star", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(76f, -54f), new Vector2(96f, 96f));
            _star = Img(starRt, Color.white, LP.StarTexture("menu_star", 256, Red, Gold));
            _star.preserveAspect = true;

            var title = Txt(panel, "Title", "BALKANSKE\nMINE", 88, OffWhite, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(188f, -58f),
                new Vector2(650f, 230f));
            title.lineSpacing = 0.86f;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.09f, 0.18f, 0.9f);
            outline.effectDistance = new Vector2(4f, -4f);

            Txt(panel, "Subtitle", "3D MINOLOVAC   •   SFRJ 1945—1991", 24, Gold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(188f, -300f),
                new Vector2(640f, 40f));

            var line = R("Rule", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(188f, -346f), new Vector2(130f, 4f));
            Img(line, Red);

            Txt(panel, "Flavor", "Balkanski svet u malom. Otvori sva bezbedna polja,\na ne zgazi na minu. Srećno, druže!",
                22, Muted, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(188f, -368f), new Vector2(560f, 80f));

            var start = Btn(panel, "ZAPOČNI", new Vector2(0f, 118f), new Vector2(620f, 78f),
                new Color(0.62f, 0.10f, 0.13f, 0.96f), OffWhite, () => _game.StartLevel(0), 32);
            start.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            start.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            start.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            start.GetComponent<RectTransform>().anchoredPosition = new Vector2(82f, 54f);

            var levels = Btn(panel, "IZABERI NIVO", new Vector2(0f, 30f), new Vector2(620f, 62f),
                NavySoft, OffWhite, ToggleLevelSelect, 26);
            levels.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            levels.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            levels.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            levels.GetComponent<RectTransform>().anchoredPosition = new Vector2(82f, -14f);

            var help = Btn(panel, "UPUTSTVO", new Vector2(0f, -46f), new Vector2(620f, 62f),
                NavySoft, OffWhite, () => ShowHelp(true), 26);
            help.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            help.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            help.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            help.GetComponent<RectTransform>().anchoredPosition = new Vector2(82f, -82f);

            var contact = Btn(panel, "KONTAKT", new Vector2(0f, -132f), new Vector2(620f, 62f),
                NavySoft, OffWhite, () => ShowContact(true), 26);
            contact.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            contact.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            contact.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            contact.GetComponent<RectTransform>().anchoredPosition = new Vector2(82f, -150f);

            _levelSelect = new GameObject("LevelSelect", typeof(RectTransform));
            _levelSelect.transform.SetParent(panel, false);
            var lsrt = (RectTransform)_levelSelect.transform;
            lsrt.anchorMin = new Vector2(0f, 0.5f);
            lsrt.anchorMax = new Vector2(0f, 0.5f);
            lsrt.pivot = new Vector2(0f, 0.5f);
            lsrt.anchoredPosition = new Vector2(82f, -218f);
            lsrt.sizeDelta = new Vector2(620f, 60f);
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var def = LevelDefinition.Get(i);
                var b = Btn(_levelSelect.transform, (i + 1) + "  " + ShortCountry(def.Country), new Vector2(i * 207f, 0f),
                    new Vector2(196f, 60f), new Color(0.10f, 0.18f, 0.32f, 0.95f), OffWhite,
                    () => _game.StartLevel(idx), 18);
                b.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
                b.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
                b.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
                b.GetComponent<RectTransform>().anchoredPosition = new Vector2(i * 207f, 0f);
            }
            _levelSelect.SetActive(false);

            _menuBest = Txt(panel, "Best", "", 18, new Color(0.82f, 0.80f, 0.68f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(82f, 112f),
                new Vector2(660f, 120f));

            var footer = Txt(panel, "Footer", "BRATSTVO I JEDINSTVO  •  BALKAN VIBE", 18,
                new Color(0.58f, 0.62f, 0.62f), TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(82f, 54f), new Vector2(660f, 30f));

            var hint = Txt(_menuGroup.transform, "Hint", "MIŠEM BIRAJ  •  ENTER ZAPOČNI", 16,
                new Color(0.82f, 0.80f, 0.70f, 0.9f), TextAnchor.MiddleRight, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-38f, 24f), new Vector2(460f, 30f));
        }

        private static string ShortCountry(string c)
        {
            if (c == "BOSNA I HERCEGOVINA") return "BOSNA";
            return c;
        }

        private void AddBand(RectTransform parent, float minY, float maxY, Color color)
        {
            var rt = R("Band", parent, new Vector2(0f, minY), new Vector2(1f, maxY),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Img(rt, color);
        }

        private void ToggleLevelSelect()
        {
            _levelSelectOpen = !_levelSelectOpen;
            _levelSelect.SetActive(_levelSelectOpen);
        }

        public void ShowMainMenu(bool instant = false)
        {
            StopMenuAnimations();
            _menuGroup.SetActive(true);
            _hudGroup.SetActive(false);
            _victoryGroup.SetActive(false);
            _defeatGroup.SetActive(false);
            _pauseGroup.SetActive(false);
            ShowHelp(false);
            ShowContact(false);
            _levelSelect.SetActive(false);
            _levelSelectOpen = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshBestTimes();
            _menuRoutine = StartCoroutine(MenuFadeIn(instant));
        }

        private void StopMenuAnimations()
        {
            if (_menuRoutine != null) { StopCoroutine(_menuRoutine); _menuRoutine = null; }
        }

        private IEnumerator MenuFadeIn(bool instant)
        {
            _menuCg.alpha = instant ? 1f : 0f;
            var rt = _menuPanel;
            Vector2 target = rt.anchoredPosition;
            if (!instant) rt.anchoredPosition = target + new Vector2(-90f, 0f);
            else rt.anchoredPosition = target;
            float t = 0f;
            while (t < 0.75f)
            {
                t += Time.unscaledDeltaTime;
                float e = Mathf.SmoothStep(0f, 1f, t / 0.75f);
                _menuCg.alpha = instant ? 1f : e;
                rt.anchoredPosition = Vector2.Lerp(target + new Vector2(-90f, 0f), target, e);
                yield return null;
            }
            _menuCg.alpha = 1f;
            rt.anchoredPosition = target;
        }

        private void RefreshBestTimes()
        {
            string s = "";
            for (int i = 0; i < 3; i++)
            {
                float best = PlayerPrefs.GetFloat("best_time_" + i, -1f);
                s += (i + 1) + ". " + ShortCountry(LevelDefinition.Get(i).Country) + ": " + (best > 0f ? FormatTime(best) : "—");
                if (i < 2) s += "     ";
            }
            if (_menuBest != null) _menuBest.text = "NAJBOLJA VREMENA\n" + s;
        }

        // ------------------------------------------------------------------
        //  HUD
        // ------------------------------------------------------------------

        private void BuildHud()
        {
            _hudGroup = Group("HUD", null);
            _hudCg = _hudGroup.GetComponent<CanvasGroup>();
            _hudCg.alpha = 1f;

            var vignette = R("Vignette", _hudGroup.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            Stretch(vignette);
            var vi = Img(vignette, Color.white, LP.RadialTexture("vignette", 256,
                new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.55f), 2.7f));
            vi.raycastTarget = false;

            // Top-left level card.
            var left = R("LevelCard", _hudGroup.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(500f, 112f));
            Img(left, Navy);
            var leftAccent = R("Accent", left, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(7f, 0f));
            Stretch(leftAccent);
            leftAccent.anchorMax = new Vector2(0f, 1f);
            leftAccent.sizeDelta = new Vector2(7f, 0f);
            Img(leftAccent, Red);
            _hudLevel = Txt(left, "Country", "SRBIJA", 34, OffWhite, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f),
                new Vector2(450f, 42f));
            _hudSub = Txt(left, "Sub", "NIVO 1 / 3  •  VOJVODINA", 18, Gold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -62f),
                new Vector2(450f, 26f));

            // Top-right counter.
            _counterPanel = R("CounterCard", _hudGroup.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(480f, 112f));
            Img(_counterPanel, Navy);
            var rightAccent = R("Accent", _counterPanel, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), Vector2.zero, new Vector2(7f, 0f));
            Stretch(rightAccent);
            rightAccent.anchorMin = new Vector2(1f, 0f);
            rightAccent.anchorMax = new Vector2(1f, 1f);
            rightAccent.sizeDelta = new Vector2(7f, 0f);
            Img(rightAccent, Blue);
            var starRt = R("Star", _counterPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(58f, 58f));
            var st = Img(starRt, Color.white, LP.StarTexture("hud_star", 256, Red, Gold));
            st.preserveAspect = true;
            _hudCounter = Txt(_counterPanel, "Counter", "MINE  10      ZASTAVE  0", 28, OffWhite,
                TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                new Vector2(96f, 0f), new Vector2(360f, 60f));

            // Top-centre timer.
            var timer = R("TimerCard", _hudGroup.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(260f, 62f));
            Img(timer, new Color(0.03f, 0.06f, 0.12f, 0.78f));
            _hudTimer = Txt(timer, "Timer", "VREME  00:00", 24, OffWhite, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch((RectTransform)_hudTimer.transform, 10f, 6f, 10f, 6f);

            // Crosshair.
            var cross = R("Crosshair", _hudGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
            var ci = Img(cross, new Color(1f, 1f, 1f, 0.85f),
                LP.StarTexture("crosshair", 128, new Color(1f, 1f, 1f, 0.95f), new Color(0.82f, 0.10f, 0.12f, 0.95f)));
            ci.preserveAspect = true;

            // Prompt under the crosshair.
            var promptRt = R("Prompt", _hudGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -74f), new Vector2(520f, 46f));
            _promptCg = promptRt.gameObject.AddComponent<CanvasGroup>();
            _promptCg.alpha = 0f;
            var pbg = Img(promptRt, new Color(0.02f, 0.05f, 0.10f, 0.72f));
            _hudPrompt = Txt(promptRt, "PromptText", "LMB  —  POSTAVI ZASTAVU", 20, Gold,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch((RectTransform)_hudPrompt.transform, 8f, 4f, 8f, 4f);

            // Bottom-left controls.
            Txt(_hudGroup.transform, "Controls",
                "WASD  KRETANJE     SPACE  SKOK     LMB  ZASTAVA     ESC  PAUZA", 17,
                new Color(0.90f, 0.88f, 0.78f, 0.85f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 28f),
                new Vector2(1000f, 30f));

            // Toast.
            _toastRoot = R("Toast", _hudGroup.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 116f), new Vector2(760f, 54f));
            _toastCg = _toastRoot.gameObject.AddComponent<CanvasGroup>();
            _toastCg.alpha = 0f;
            Img(_toastRoot, new Color(0.02f, 0.05f, 0.10f, 0.82f));
            _toastText = Txt(_toastRoot, "ToastText", "", 22, OffWhite, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch((RectTransform)_toastText.transform, 10f, 4f, 10f, 4f);

            // Level intro banner.
            _bannerRoot = R("Banner", _hudGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1000f, 180f));
            _bannerCg = _bannerRoot.gameObject.AddComponent<CanvasGroup>();
            _bannerCg.alpha = 0f;
            var bbg = Img(_bannerRoot, new Color(0.02f, 0.05f, 0.11f, 0.80f));
            var bar = R("Bar", _bannerRoot, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 8f));
            Img(bar, Red);
            _bannerTitle = Txt(_bannerRoot, "BannerTitle", "SRBIJA", 64, OffWhite, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f),
                new Vector2(0f, 70f));
            _bannerSub = Txt(_bannerRoot, "BannerSub", "NIVO 1  •  VOJVODINA", 24, Gold, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f),
                new Vector2(0f, 36f));

            _hudGroup.SetActive(false);
        }

        public void SetLevel(LevelDefinition def, int index)
        {
            _hudLevel.text = def.Country;
            _hudSub.text = "NIVO " + (index + 1) + " / 3  •  " + def.Subtitle;
        }

        public void SetCounter(int mines, int flags)
        {
            _hudCounter.text = "MINE  " + mines.ToString("00") + "        ZASTAVE  " + flags.ToString("00");
            _hudCounter.color = flags > mines ? new Color(1f, 0.45f, 0.35f) : OffWhite;
        }

        public void PulseCounter()
        {
            if (_counterPulse != null) StopCoroutine(_counterPulse);
            _counterPulse = StartCoroutine(PulseRect(_counterPanel, 1.06f, 0.16f));
        }

        private IEnumerator PulseRect(RectTransform rt, float scale, float dur)
        {
            float t = 0f;
            while (t < dur * 2f)
            {
                t += Time.unscaledDeltaTime;
                float k = t < dur ? t / dur : 1f - (t - dur) / dur;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, scale, k);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        public void SetTimer(float seconds)
        {
            _hudTimer.text = "VREME  " + FormatTime(seconds);
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        public void SetPrompt(bool show)
        {
            if (_promptCg == null) return;
            _promptCg.alpha = show ? 0.95f : 0f;
        }

        public void ShowToast(string message, float duration = 2.2f)
        {
            if (_toastRoot == null) return;
            _toastText.text = message;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine(duration));
        }

        private IEnumerator ToastRoutine(float duration)
        {
            float t = 0f;
            _toastCg.alpha = 0f;
            var rt = _toastRoot;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = t < 0.18f ? t / 0.18f : (t > duration - 0.35f ? (duration - t) / 0.35f : 1f);
                _toastCg.alpha = Mathf.Clamp01(a);
                rt.anchoredPosition = new Vector2(0f, 116f + (1f - Mathf.Clamp01(t / 0.18f)) * -18f);
                yield return null;
            }
            _toastCg.alpha = 0f;
        }

        public void ShowBanner(LevelDefinition def, int index, float hold = 1.75f)
        {
            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            _bannerTitle.text = def.Country;
            _bannerSub.text = "NIVO " + (index + 1) + "  •  " + def.Subtitle + "  •  " + def.Difficulty;
            _bannerRoutine = StartCoroutine(BannerRoutine(hold));
        }

        private IEnumerator BannerRoutine(float hold)
        {
            _bannerRoot.gameObject.SetActive(true);
            Vector2 target = new Vector2(0f, 120f);
            float t = 0f;
            _bannerCg.alpha = 0f;
            _bannerRoot.anchoredPosition = target + new Vector2(-120f, 0f);
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float e = Mathf.SmoothStep(0f, 1f, t / 0.35f);
                _bannerCg.alpha = e;
                _bannerRoot.anchoredPosition = Vector2.Lerp(target + new Vector2(-120f, 0f), target, e);
                yield return null;
            }
            _bannerCg.alpha = 1f;
            _bannerRoot.anchoredPosition = target;
            yield return new WaitForSecondsRealtime(hold);
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                float e = t / 0.4f;
                _bannerCg.alpha = 1f - e;
                _bannerRoot.anchoredPosition = Vector2.Lerp(target, target + new Vector2(120f, 0f), e);
                yield return null;
            }
            _bannerCg.alpha = 0f;
            _bannerRoot.gameObject.SetActive(false);
        }

        public void HideBanner()
        {
            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            _bannerCg.alpha = 0f;
        }

        // ------------------------------------------------------------------
        //  HUD visibility
        // ------------------------------------------------------------------

        public void ShowHud()
        {
            ShowContact(false);
            ShowHelp(false);
            _menuGroup.SetActive(false);
            _hudGroup.SetActive(true);
            _victoryGroup.SetActive(false);
            _defeatGroup.SetActive(false);
            _pauseGroup.SetActive(false);
        }

        public void HideHud()
        {
            _hudGroup.SetActive(false);
        }

        // ------------------------------------------------------------------
        //  Victory
        // ------------------------------------------------------------------

        private void BuildVictory()
        {
            _victoryGroup = Group("Victory", null);
            _victoryCg = _victoryGroup.GetComponent<CanvasGroup>();
            Img(R("Dim", _victoryGroup.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero), new Color(0f, 0f, 0f, 0.62f));

            _victoryPanel = R("VictoryPanel", _victoryGroup.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 540f));
            Img(_victoryPanel, new Color(0.025f, 0.05f, 0.10f, 0.97f));
            var top = R("TopBand", _victoryPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));
            Img(top, Gold);

            var starRt = R("Star", _victoryPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, 30f), new Vector2(84f, 84f));
            var st = Img(starRt, Color.white, LP.StarTexture("victory_star", 256, Red, Gold));
            st.preserveAspect = true;

            _victoryTitle = Txt(_victoryPanel, "Title", "POBEDA!", 78, Gold, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f),
                new Vector2(0f, 100f));
            _victorySub = Txt(_victoryPanel, "Sub", "", 28, OffWhite, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f),
                new Vector2(0f, 44f));
            _victoryStats = Txt(_victoryPanel, "Stats", "", 24, Muted, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -260f),
                new Vector2(0f, 90f));

            var next = Btn(_victoryPanel, "SLEDEĆI NIVO", new Vector2(-170f, -176f), new Vector2(330f, 70f),
                new Color(0.62f, 0.10f, 0.13f, 0.96f), OffWhite, () => _game.NextLevel(), 25);
            next.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            next.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            next.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            next.GetComponent<RectTransform>().anchoredPosition = new Vector2(-170f, 54f);
            _nextButtonText = next.GetComponentInChildren<Text>();

            var menu = Btn(_victoryPanel, "GLAVNI MENI", new Vector2(170f, -176f), new Vector2(330f, 70f),
                NavySoft, OffWhite, () => _game.ReturnToMenu(), 25);
            menu.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            menu.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            menu.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            menu.GetComponent<RectTransform>().anchoredPosition = new Vector2(170f, 54f);

            _victoryGroup.SetActive(false);
        }

        public void ShowVictory(LevelDefinition def, int index, bool final, int flags, int mines,
            float time, bool newBest)
        {
            _hudGroup.SetActive(false);
            _victoryGroup.SetActive(true);
            _victoryCg.alpha = 0f;
            _victoryPanel.localScale = Vector3.one * 0.75f;
            _victoryTitle.text = final ? "SFRJ SPASENA!" : "POBEDA!";
            _victorySub.text = def.Country + "  •  " + (final ? "SVE TRI ZEMLJE SU OČIŠĆENE" : "MINOLOVAC JE OČISTIO POLJE");
            _victoryStats.text = "VREME  " + FormatTime(time) + "     •     ZASTAVE  " + flags + " / " + mines +
                                 (newBest ? "\n<color=#F2C93D>NOVI NAJBOLJI REZULTAT!</color>" : "");
            if (_nextButtonText != null)
                _nextButtonText.text = final ? "NOVI POČETAK" : "SLEDEĆI NIVO";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StartCoroutine(OverlayIn(_victoryCg, _victoryPanel, 0.7f));
        }

        // ------------------------------------------------------------------
        //  Defeat
        // ------------------------------------------------------------------

        private void BuildDefeat()
        {
            _defeatGroup = Group("Defeat", null);
            _defeatCg = _defeatGroup.GetComponent<CanvasGroup>();
            Img(R("Dim", _defeatGroup.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero), new Color(0.12f, 0f, 0f, 0.68f));

            _defeatPanel = R("DefeatPanel", _defeatGroup.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 500f));
            Img(_defeatPanel, new Color(0.075f, 0.025f, 0.035f, 0.97f));
            var top = R("TopBand", _defeatPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));
            Img(top, Red);

            _defeatTitle = Txt(_defeatPanel, "Title", "PORAZ", 88, new Color(0.95f, 0.30f, 0.25f),
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(0f, 110f));
            _defeatSub = Txt(_defeatPanel, "Sub", "", 26, Muted, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -208f),
                new Vector2(0f, 90f));

            var retry = Btn(_defeatPanel, "POKUŠAJ PONOVO", new Vector2(-170f, -176f), new Vector2(330f, 70f),
                new Color(0.62f, 0.10f, 0.13f, 0.96f), OffWhite, () => _game.RestartLevel(), 24);
            retry.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            retry.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            retry.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            retry.GetComponent<RectTransform>().anchoredPosition = new Vector2(-170f, 54f);

            var menu = Btn(_defeatPanel, "GLAVNI MENI", new Vector2(170f, -176f), new Vector2(330f, 70f),
                NavySoft, OffWhite, () => _game.ReturnToMenu(), 24);
            menu.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            menu.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            menu.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            menu.GetComponent<RectTransform>().anchoredPosition = new Vector2(170f, 54f);

            _defeatGroup.SetActive(false);
        }

        public void ShowDefeat(LevelDefinition def, int index, int flags, int mines)
        {
            _hudGroup.SetActive(false);
            _defeatGroup.SetActive(true);
            _defeatCg.alpha = 0f;
            _defeatPanel.localScale = Vector3.one * 0.9f;
            _defeatSub.text = def.Country + "  •  " + flags + " / " + mines + " ZASTAVA\nMINA TE JE DOČEKALA.";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StartCoroutine(OverlayIn(_defeatCg, _defeatPanel, 0.55f));
        }

        private IEnumerator OverlayIn(CanvasGroup cg, RectTransform panel, float dur)
        {
            float t = 0f;
            Vector3 target = Vector3.one;
            Vector3 start = Vector3.one * (panel == _defeatPanel ? 0.9f : 0.72f);
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float e = Mathf.SmoothStep(0f, 1f, t / dur);
                cg.alpha = e;
                float s = Mathf.LerpUnclamped(start.x, target.x, e);
                // Small overshoot for the victory panel.
                if (t / dur > 0.65f) s = Mathf.Lerp(s, target.x + 0.015f, (t / dur - 0.65f) * 2.5f);
                panel.localScale = Vector3.one * s;
                yield return null;
            }
            cg.alpha = 1f;
            panel.localScale = target;
        }

        // ------------------------------------------------------------------
        //  Pause & help
        // ------------------------------------------------------------------

        private void BuildPause()
        {
            _pauseGroup = Group("Pause", null);
            _pauseCg = _pauseGroup.GetComponent<CanvasGroup>();
            Img(R("Dim", _pauseGroup.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero), new Color(0f, 0f, 0f, 0.6f));
            _pausePanel = R("PausePanel", _pauseGroup.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 520f));
            Img(_pausePanel, new Color(0.025f, 0.05f, 0.10f, 0.97f));
            Txt(_pausePanel, "Title", "PAUZA", 64, OffWhite, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f),
                new Vector2(0f, 76f));
            Txt(_pausePanel, "Sub", "SFRJ ČEKA...", 20, Gold, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f),
                new Vector2(0f, 30f));

            var resume = Btn(_pausePanel, "NASTAVI", new Vector2(0f, 40f), new Vector2(440f, 66f),
                new Color(0.62f, 0.10f, 0.13f, 0.96f), OffWhite, () => _game.ResumeGame(), 26);
            PlaceCenter(resume.GetComponent<RectTransform>(), new Vector2(0f, 68f));
            var help = Btn(_pausePanel, "UPUTSTVO", new Vector2(0f, -38f), new Vector2(440f, 60f),
                NavySoft, OffWhite, () => ShowHelp(true), 24);
            PlaceCenter(help.GetComponent<RectTransform>(), new Vector2(0f, 2f));
            var contact = Btn(_pausePanel, "KONTAKT", new Vector2(0f, -38f), new Vector2(440f, 60f),
                NavySoft, OffWhite, () => ShowContact(true), 24);
            PlaceCenter(contact.GetComponent<RectTransform>(), new Vector2(0f, -64f));
            var menu = Btn(_pausePanel, "GLAVNI MENI", new Vector2(0f, -112f), new Vector2(440f, 60f),
                NavySoft, OffWhite, () => _game.ReturnToMenu(), 24);
            PlaceCenter(menu.GetComponent<RectTransform>(), new Vector2(0f, -130f));
            _pauseGroup.SetActive(false);
        }

        private static void PlaceCenter(RectTransform rt, Vector2 pos)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }

        public void ShowPause(bool show)
        {
            if (show)
            {
                _pauseGroup.SetActive(true);
                _pauseCg.alpha = 0f;
                _pausePanel.localScale = Vector3.one * 0.92f;
                StartCoroutine(OverlayIn(_pauseCg, _pausePanel, 0.22f));
            }
            else
            {
                _pauseGroup.SetActive(false);
            }
        }

        private void BuildHelp()
        {
            _helpGroup = Group("Help", null);
            _helpCg = _helpGroup.GetComponent<CanvasGroup>();
            Img(R("Dim", _helpGroup.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero), new Color(0f, 0f, 0f, 0.72f));
            _helpPanel = R("HelpPanel", _helpGroup.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1020f, 720f));
            Img(_helpPanel, new Color(0.025f, 0.05f, 0.10f, 0.98f));
            var top = R("TopBand", _helpPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 8f));
            top.sizeDelta = new Vector2(0f, 8f);
            Img(top, Gold);

            Txt(_helpPanel, "Title", "UPUTSTVO  /  KAKO SE IGRA", 44, Gold, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(54f, -28f),
                new Vector2(900f, 62f));

            string body =
                "<b>CILJ</b>\nOtvori sva bezbedna polja, a da ne skočiš na minu. Polja se otvaraju skokom.\n\n" +
                "<b>KONTROLE</b>\n" +
                "WASD / strelice — kretanje\n" +
                "MIŠ — gledanje   •   SHIFT — trčanje\n" +
                "SPACE — skok   •   LMB — postavi / ukloni zastavu\n" +
                "ESC — pauza\n\n" +
                "<b>BROJEVI</b>\n" +
                "Broj na otvorenom polju kaže koliko je mina u susednih 8 polja.\n" +
                "Polje sa 0 otvara ceo prazan predeo automatski.\n\n" +
                "<b>ZASTAVE</b>\n" +
                "Označi sumnjiva polja zastavom. Brojač gore desno prati mine i postavljene zastave.\n" +
                "Ne možeš otvoriti polje na kome stoji zastava — prvo je ukloni.\n\n" +
                "<b>BALKAN</b>\n" +
                "Tri nivoa: Srbija, Hrvatska i Bosna i Hercegovina. Svaki je veći i opasniji.\n" +
                "Srećno, druže!";
            Txt(_helpPanel, "Body", body, 22, OffWhite, TextAnchor.UpperLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(54f, -100f),
                new Vector2(910f, 520f));

            var back = Btn(_helpPanel, "NAZAD", new Vector2(0f, 40f), new Vector2(260f, 60f),
                Red, OffWhite, () => ShowHelp(false), 24);
            back.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            back.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            back.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            back.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 34f);

            _helpGroup.SetActive(false);
        }

        public void ShowHelp(bool show)
        {
            if (show)
            {
                _helpGroup.SetActive(true);
                _helpCg.alpha = 0f;
                _helpPanel.localScale = Vector3.one * 0.94f;
                StartCoroutine(OverlayIn(_helpCg, _helpPanel, 0.2f));
            }
            else
            {
                _helpGroup.SetActive(false);
            }
        }

        public bool HelpOpen => _helpGroup != null && _helpGroup.activeSelf;

        // ------------------------------------------------------------------
        //  Contact
        // ------------------------------------------------------------------

        private void BuildContact()
        {
            _contactGroup = Group("Contact", null);
            _contactCg = _contactGroup.GetComponent<CanvasGroup>();
            Img(R("Dim", _contactGroup.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero), new Color(0f, 0f, 0f, 0.72f));

            _contactPanel = R("ContactPanel", _contactGroup.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 620f));
            Img(_contactPanel, new Color(0.025f, 0.05f, 0.10f, 0.98f));
            var top = R("TopBand", _contactPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 8f));
            Img(top, Gold);

            Txt(_contactPanel, "Title", "KONTAKT  /  ŽALBE I PREDLOZI", 44, Gold, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(54f, -30f),
                new Vector2(880f, 62f));

            Txt(_contactPanel, "Intro",
                "Imaš žalbu, predlog ili ideju za novo polje?\nPiši nam — svaka poruka stiže do pravog čoveka.",
                24, OffWhite, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 1f), new Vector2(54f, -110f), new Vector2(880f, 90f));

            // Highlighted e-mail address.
            var mailChip = R("MailChip", _contactPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -216f), new Vector2(760f, 88f));
            Img(mailChip, new Color(0.09f, 0.16f, 0.30f, 0.96f));
            var chipAccent = R("Accent", mailChip, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(7f, 0f));
            Stretch(chipAccent);
            chipAccent.anchorMax = new Vector2(0f, 1f);
            chipAccent.sizeDelta = new Vector2(7f, 0f);
            Img(chipAccent, Red);
            Txt(mailChip, "MailLabel", "PIŠI NA", 16, Muted, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(28f, 0f),
                new Vector2(120f, 0f));
            Txt(mailChip, "Mail", "glebzubkov705@gmail.com", 32, OffWhite, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 0f),
                new Vector2(0f, 0f));

            Txt(_contactPanel, "Hint",
                "Dugme ispod otvara tvoju mejl aplikaciju sa već upisanom adresom.\nMožeš i ručno poslati poruku na adresu iznad.",
                19, Muted, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -328f), new Vector2(820f, 70f));

            var send = Btn(_contactPanel, "POŠALJI MEJL", new Vector2(-210f, -176f), new Vector2(380f, 66f),
                new Color(0.62f, 0.10f, 0.13f, 0.96f), OffWhite, OpenMail, 24);
            send.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            send.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            send.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            send.GetComponent<RectTransform>().anchoredPosition = new Vector2(-210f, 54f);

            var back = Btn(_contactPanel, "NAZAD", new Vector2(210f, -176f), new Vector2(380f, 66f),
                NavySoft, OffWhite, () => ShowContact(false), 24);
            back.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            back.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            back.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            back.GetComponent<RectTransform>().anchoredPosition = new Vector2(210f, 54f);

            _contactGroup.SetActive(false);
        }

        private void OpenMail()
        {
            const string address = "glebzubkov705@gmail.com";
            string subject = System.Uri.EscapeDataString("Balkanske Mine 3D - zalba ili predlog");
            string body = System.Uri.EscapeDataString("Zdravo,\n\nMoja zalba / predlog:\n\n\n");
            Application.OpenURL("mailto:" + address + "?subject=" + subject + "&body=" + body);
        }

        public void ShowContact(bool show)
        {
            if (show)
            {
                ShowHelp(false);
                _contactGroup.SetActive(true);
                _contactCg.alpha = 0f;
                _contactPanel.localScale = Vector3.one * 0.94f;
                StartCoroutine(OverlayIn(_contactCg, _contactPanel, 0.2f));
            }
            else
            {
                _contactGroup.SetActive(false);
            }
        }

        public bool ContactOpen => _contactGroup != null && _contactGroup.activeSelf;

        // ------------------------------------------------------------------
        //  Flash
        // ------------------------------------------------------------------

        private void BuildFlash()
        {
            var rt = R("Flash", _canvas.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            Stretch(rt);
            _flash = Img(rt, new Color(0f, 0f, 0f, 0f));
            _flash.raycastTarget = false;
        }

        public void Flash(Color color, float duration)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(color, duration));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            float t = 0f;
            _flash.transform.SetAsLastSibling();
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float e = 1f - t / duration;
                var c = color;
                c.a = Mathf.Clamp01(color.a * e);
                _flash.color = c;
                yield return null;
            }
            _flash.color = new Color(0f, 0f, 0f, 0f);
        }

        private void Update()
        {
            if (_star != null && _menuGroup != null && _menuGroup.activeSelf)
            {
                _star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 0.7f) * 5f - 3f);
            }
        }
    }
}
