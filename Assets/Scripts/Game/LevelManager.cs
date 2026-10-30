using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BalkanMines
{
    /// <summary>The minefield: grid generation, mine seeding, flood reveal, flags, win/lose.</summary>
    public class LevelManager : MonoBehaviour
    {
        public LevelDefinition Def;
        public GameBootstrap Game;
        [System.NonSerialized] public TileView[,] Tiles;
        public float FenceHalf;
        public float GridHalf;
        public int FlagCount { get; private set; }
        public bool GameOver { get; private set; }
        public bool Won { get; private set; }
        public float Elapsed { get; private set; }

        private int _revealedSafe;
        private bool _minesSeeded;
        private bool _timerRunning;
        private float _startTime;
        private float _uiTimer;
        private Texture2D _grassTex;

        public void Build(LevelDefinition def, WorldBuilder.Layout layout, GameBootstrap game)
        {
            Def = def;
            Game = game;
            FenceHalf = layout.FenceHalf;
            GridHalf = layout.GridHalf;
            _grassTex = LP.NoiseTexture("soil_" + def.CountryCode, 256,
                new Color(0.34f, 0.24f, 0.14f), new Color(0.62f, 0.47f, 0.29f), 0.95f, 37 + game.LevelIndex);

            int n = def.Size;
            Tiles = new TileView[n, n];
            float offset = (n - 1) * WorldBuilder.TileSpacing * 0.5f;
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    var go = new GameObject($"Tile {x},{z}");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(x * WorldBuilder.TileSpacing - offset, 0f,
                        z * WorldBuilder.TileSpacing - offset);
                    var moundMesh = LP.Mound(x * 31 + z * 17 + game.LevelIndex * 7, 10, 0.82f, 0.58f);
                    go.AddComponent<MeshFilter>().sharedMesh = moundMesh;
                    go.AddComponent<MeshRenderer>();
                    // A convex mesh collider follows the sloped dirt pile, so the player can
                    // casually walk up onto a mound instead of hitting an invisible box wall.
                    var moundCollider = go.AddComponent<MeshCollider>();
                    moundCollider.sharedMesh = moundMesh;
                    moundCollider.convex = true;
                    var tile = go.AddComponent<TileView>();
                    tile.Setup(this, x, z, def, _grassTex);
                    Tiles[x, z] = tile;
                }
            }

            Game.UI.SetLevel(def, game.LevelIndex);
            Game.UI.SetCounter(def.Mines, 0);
            Game.UI.SetTimer(0f);
        }

        public void BeginTimer()
        {
            _startTime = Time.time;
            _timerRunning = true;
        }

        private void Update()
        {
            if (_timerRunning)
            {
                Elapsed = Time.time - _startTime;
                _uiTimer -= Time.deltaTime;
                if (_uiTimer <= 0f)
                {
                    _uiTimer = 0.08f;
                    if (Game != null) Game.UI.SetTimer(Elapsed);
                }
            }
        }

        // ------------------------------------------------------------------
        //  Player interaction
        // ------------------------------------------------------------------

        /// <summary>Returns true when the player should bounce off (flagged tile).</summary>
        public bool HandleLanding(TileView tile)
        {
            if (tile == null || GameOver) return false;

            if (tile.IsFlagged)
            {
                Game.UI.ShowToast("ZASTAVA! UKLONI JE PA SKOČI.");
                Game.Audio.PlayError();
                Game.Fx.Dust(tile.transform.position + Vector3.up * 0.5f, new Color(0.7f, 0.65f, 0.5f), 6);
                return true;
            }

            if (tile.IsRevealed) return false;

            Game.Fx.Dust(tile.transform.position + Vector3.up * 0.6f, new Color(0.62f, 0.55f, 0.38f), 10);
            Game.Audio.PlayLand();

            if (!_minesSeeded) SeedMines(tile);

            if (tile.IsMine)
            {
                tile.ShowMine();
                StartCoroutine(tile.RevealRoutine(true));
                Defeat(tile);
                return false;
            }

            RevealFlood(tile);
            return false;
        }

        public void ToggleFlag(TileView tile)
        {
            if (tile == null || GameOver || tile.IsRevealed) return;

            if (tile.IsFlagged)
            {
                tile.SetFlagVisual(false);
                FlagCount = Mathf.Max(0, FlagCount - 1);
                Game.Audio.PlayClick();
                Game.UI.ShowToast("ZASTAVA UKLONJENA");
            }
            else
            {
                if (FlagCount >= Def.Mines)
                {
                    Game.UI.ShowToast("NEMAŠ VIŠE ZASTAVA!");
                    Game.Audio.PlayError();
                    return;
                }
                tile.SetFlagVisual(true);
                FlagCount++;
                Game.UI.ShowToast("ZASTAVA POSTAVLJENA  " + FlagCount + "/" + Def.Mines);
            }

            Game.UI.SetCounter(Def.Mines, FlagCount);
            Game.UI.PulseCounter();
        }

        public void PlayFlagSound()
        {
            if (Game != null) Game.Audio.PlayFlag();
        }

        // ------------------------------------------------------------------
        //  Minefield logic
        // ------------------------------------------------------------------

        private void SeedMines(TileView first)
        {
            _minesSeeded = true;
            var candidates = new List<TileView>();
            int n = Def.Size;
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    var t = Tiles[x, z];
                    if (t == first) continue;
                    if (Mathf.Abs(t.X - first.X) <= 1 && Mathf.Abs(t.Z - first.Z) <= 1) continue;
                    if (t.IsFlagged) continue;
                    candidates.Add(t);
                }
            }

            // Safety net for tiny/dense boards.
            if (candidates.Count < Def.Mines)
            {
                candidates.Clear();
                for (int z = 0; z < n; z++)
                    for (int x = 0; x < n; x++)
                    {
                        var t = Tiles[x, z];
                        if (t != first && !t.IsFlagged) candidates.Add(t);
                    }
            }

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = tmp;
            }

            int placed = 0;
            for (int i = 0; i < candidates.Count && placed < Def.Mines; i++)
            {
                candidates[i].IsMine = true;
                placed++;
            }

            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    var t = Tiles[x, z];
                    if (t.IsMine) continue;
                    t.Adjacent = CountAdjacentMines(x, z);
                }
            }
        }

        private int CountAdjacentMines(int x, int z)
        {
            int c = 0;
            int n = Def.Size;
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int nx = x + dx, nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= n || nz >= n) continue;
                    if (Tiles[nx, nz].IsMine) c++;
                }
            }
            return c;
        }

        /// <summary>Maps a world-space aim point to the nearest grid tile (or null outside the fence).</summary>
        public TileView TileAtWorld(Vector3 world)
        {
            if (Tiles == null || Def == null) return null;
            Vector3 local = transform.InverseTransformPoint(world);
            float spacing = WorldBuilder.TileSpacing;
            float offset = (Def.Size - 1) * spacing * 0.5f;
            int x = Mathf.RoundToInt((local.x + offset) / spacing);
            int z = Mathf.RoundToInt((local.z + offset) / spacing);
            if (x < 0 || z < 0 || x >= Def.Size || z >= Def.Size) return null;
            // Ignore aim points well outside the mound footprint.
            var tile = Tiles[x, z];
            Vector3 center = tile.transform.localPosition;
            float dx = local.x - center.x;
            float dz = local.z - center.z;
            if (dx * dx + dz * dz > 1.65f * 1.65f) return null;
            return tile;
        }

        private void RevealFlood(TileView first)
        {
            var stack = new Stack<TileView>();
            var seen = new HashSet<TileView>();
            stack.Push(first);
            int revealedThisTurn = 0;

            while (stack.Count > 0)
            {
                var t = stack.Pop();
                if (t == null || seen.Contains(t)) continue;
                seen.Add(t);
                if (t.IsMine || t.IsFlagged || t.IsRevealed) continue;

                RevealSafe(t);
                revealedThisTurn++;

                if (t.Adjacent == 0)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            int nx = t.X + dx, nz = t.Z + dz;
                            if (nx < 0 || nz < 0 || nx >= Def.Size || nz >= Def.Size) continue;
                            var nt = Tiles[nx, nz];
                            if (!nt.IsRevealed && !nt.IsFlagged && !nt.IsMine) stack.Push(nt);
                        }
                    }
                }
            }

            if (revealedThisTurn > 3)
                Game.UI.ShowToast("OTVORENO POLJA: " + revealedThisTurn);

            if (_revealedSafe >= Def.SafeCount) Victory();
        }

        private void RevealSafe(TileView t)
        {
            t.StartCoroutine(t.RevealRoutine(false));
            if (t.Adjacent > 0) t.ShowNumber(t.Adjacent, NumberColor(t.Adjacent));
            _revealedSafe++;
            Game.Audio.PlayReveal();
        }

        private static Color NumberColor(int n)
        {
            switch (n)
            {
                case 1: return new Color(0.25f, 0.55f, 0.95f);
                case 2: return new Color(0.22f, 0.70f, 0.30f);
                case 3: return new Color(0.95f, 0.25f, 0.20f);
                case 4: return new Color(0.55f, 0.25f, 0.85f);
                case 5: return new Color(0.65f, 0.20f, 0.12f);
                case 6: return new Color(0.10f, 0.65f, 0.65f);
                case 7: return new Color(0.97f, 0.96f, 0.90f);
                default: return new Color(0.20f, 0.20f, 0.22f);
            }
        }

        private void Victory()
        {
            if (GameOver) return;
            GameOver = true;
            Won = true;
            _timerRunning = false;
            Elapsed = Time.time - _startTime;

            for (int z = 0; z < Def.Size; z++)
            {
                for (int x = 0; x < Def.Size; x++)
                {
                    var t = Tiles[x, z];
                    if (t.IsMine && !t.IsFlagged) t.SetFlagVisual(true);
                }
            }
            FlagCount = Def.Mines;
            Game.UI.SetCounter(Def.Mines, FlagCount);
            Game.Audio.PlayVictory();
            Game.OnVictory(this);
        }

        private void Defeat(TileView exploded)
        {
            if (GameOver) return;
            GameOver = true;
            _timerRunning = false;
            Elapsed = Time.time - _startTime;

            Game.Audio.PlayExplosion();
            Game.Fx.Explosion(exploded.transform.position + Vector3.up * 0.4f);
            Game.Fx.Shake(0.55f, 1.1f);
            Game.OnDefeat(this);

            StartCoroutine(RevealRemainingMines(exploded));
        }

        private IEnumerator RevealRemainingMines(TileView exploded)
        {
            for (int z = 0; z < Def.Size; z++)
            {
                for (int x = 0; x < Def.Size; x++)
                {
                    var t = Tiles[x, z];
                    if (t == exploded) continue;
                    if (t.IsMine)
                    {
                        t.ShowMine();
                        StartCoroutine(t.RevealRoutine(true));
                        yield return new WaitForSeconds(0.035f);
                    }
                    else if (t.IsFlagged)
                    {
                        t.SetWrongFlag();
                    }
                }
            }
        }
    }
}
