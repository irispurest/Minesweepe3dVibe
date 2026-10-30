using UnityEngine;

namespace BalkanMines
{
    /// <summary>Data for one of the three Balkan levels.</summary>
    [System.Serializable]
    public class LevelDefinition
    {
        public string Country;
        public string CountryCode;
        public string Subtitle;
        public string Flavor;
        public string Difficulty;
        public int Size;
        public int Mines;
        public Color Grass;
        public Color GrassAlt;
        public Color SkyTint;
        public Color FogColor;
        public Color Accent;
        public float FogDensity;
        public bool Coastal;
        public bool HighMountains;
        public bool Snow;

        public int SafeCount => Size * Size - Mines;

        public static readonly LevelDefinition[] All =
        {
            new LevelDefinition
            {
                Country = "SRBIJA", CountryCode = "serbia",
                Subtitle = "VOJVODINA • RAVNICA",
                Flavor = "Zlatna polja, bagremi i tišina ravnice.\nPazi gde slećeš — mina je svuda.",
                Difficulty = "LAKO",
                Size = 8, Mines = 10,
                Grass = new Color(0.40f, 0.55f, 0.22f),
                GrassAlt = new Color(0.56f, 0.66f, 0.28f),
                SkyTint = new Color(0.44f, 0.64f, 0.90f),
                FogColor = new Color(0.72f, 0.82f, 0.85f),
                Accent = new Color(0.80f, 0.12f, 0.14f),
                FogDensity = 0.0034f,
                Coastal = false, HighMountains = false
            },
            new LevelDefinition
            {
                Country = "HRVATSKA", CountryCode = "croatia",
                Subtitle = "DALMACIJA • MORE",
                Flavor = "Kamen, borovi i modro more.\nSvaki skok je korak bliže ivici.",
                Difficulty = "SREDNJE",
                Size = 10, Mines = 18,
                Grass = new Color(0.45f, 0.52f, 0.24f),
                GrassAlt = new Color(0.62f, 0.60f, 0.34f),
                SkyTint = new Color(0.30f, 0.62f, 0.88f),
                FogColor = new Color(0.66f, 0.83f, 0.90f),
                Accent = new Color(0.80f, 0.12f, 0.14f),
                FogDensity = 0.0028f,
                Coastal = true, HighMountains = false
            },
            new LevelDefinition
            {
                Country = "BOSNA I HERCEGOVINA", CountryCode = "bosnia",
                Subtitle = "BOSANSKE PLANINE",
                Flavor = "Magla među planinama krije najviše mina.\nNe veruj tišini.",
                Difficulty = "TEŠKO",
                Size = 12, Mines = 30,
                Grass = new Color(0.27f, 0.43f, 0.24f),
                GrassAlt = new Color(0.39f, 0.48f, 0.29f),
                SkyTint = new Color(0.39f, 0.51f, 0.70f),
                FogColor = new Color(0.62f, 0.70f, 0.72f),
                Accent = new Color(0.82f, 0.62f, 0.13f),
                FogDensity = 0.0058f,
                Coastal = false, HighMountains = true, Snow = true
            }
        };

        public static LevelDefinition Get(int index)
        {
            return All[Mathf.Clamp(index, 0, All.Length - 1)];
        }
    }
}
