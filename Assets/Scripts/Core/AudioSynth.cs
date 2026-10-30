using UnityEngine;

namespace BalkanMines
{
    /// <summary>All audio is synthesized at runtime so the project stays asset-free and low-poly.</summary>
    public class AudioSynth : MonoBehaviour
    {
        private const int Rate = 44100;

        private AudioSource _sfx;
        private AudioSource _ambient;
        private AudioSource _music;

        private AudioClip _click;
        private AudioClip _flag;
        private AudioClip _blip;
        private AudioClip _jump;
        private AudioClip _land;
        private AudioClip _footstep;
        private AudioClip _explosion;
        private AudioClip _victory;
        private AudioClip _defeat;
        private AudioClip _error;

        private float _stepTimer;
        private readonly AudioClip[] _levelMusic = new AudioClip[3];
        private int _levelIndex;

        public void Init()
        {
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;

            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.playOnAwake = false;
            _ambient.loop = true;
            _ambient.spatialBlend = 0f;
            _ambient.volume = 0.16f;

            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;
            _music.volume = 0.10f;

            _click = MakeTone("click", 0.06f, 900f, 1150f, 0.30f, 0.20f);
            _flag = MakeClick("flag", 0.10f, 0.32f);
            _blip = MakeTone("blip", 0.09f, 620f, 880f, 0.30f, 0.30f);
            _jump = MakeTone("jump", 0.16f, 260f, 520f, 0.25f, 0.20f);
            _land = MakeNoise("land", 0.12f, 0.28f, 0.06f);
            _footstep = MakeNoise("step", 0.07f, 0.10f, 0.03f);
            _explosion = MakeExplosion();
            _victory = MakeMelody("victory", new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f }, 0.16f, 0.28f);
            _defeat = MakeMelody("defeat", new[] { 392f, 349.23f, 311.13f, 246.94f, 196f }, 0.22f, 0.30f);
            _error = MakeTone("error", 0.16f, 220f, 140f, 0.28f, 0.10f);

            _ambient.clip = MakeWind();
            _ambient.Play();
            _music.volume = 0.16f;
            _music.clip = GetLevelMusic(0);
            _music.Play();
        }

        /// <summary>Swaps in the level's Balkan folk loop and gives its SFX a slightly different pitch.</summary>
        public void SetLevelMusic(int index)
        {
            _levelIndex = Mathf.Clamp(index, 0, 2);
            if (_music != null)
            {
                _music.Stop();
                _music.clip = GetLevelMusic(_levelIndex);
                _music.Play();
            }
            if (_sfx != null) _sfx.pitch = 0.94f + _levelIndex * 0.05f;
        }

        public void SetMenuMusic()
        {
            _levelIndex = 0;
            if (_music != null)
            {
                _music.Stop();
                _music.clip = GetLevelMusic(0);
                _music.Play();
            }
            if (_sfx != null) _sfx.pitch = 1f;
        }

        private AudioClip GetLevelMusic(int index)
        {
            if (_levelMusic[index] == null) _levelMusic[index] = MakeBalkanLoop(index);
            return _levelMusic[index];
        }

        public void PlayClick() => Play(_click, 0.7f);
        public void PlayFlag() => Play(_flag, 1f);
        public void PlayReveal() => Play(_blip, 0.8f);
        public void PlayJump() => Play(_jump, 0.7f);
        public void PlayLand() => Play(_land, 0.7f);
        public void PlayExplosion() => Play(_explosion, 1f);
        public void PlayVictory() => Play(_victory, 1f);
        public void PlayDefeat() => Play(_defeat, 1f);
        public void PlayError() => Play(_error, 0.8f);

        public void PlayFootstep()
        {
            if (_footstep == null) return;
            _sfx.pitch = Random.Range(0.86f, 1.08f);
            _sfx.PlayOneShot(_footstep, 0.45f);
            _sfx.pitch = 1f;
        }

        public void TickFootsteps(bool moving, bool grounded, float speedFactor)
        {
            if (!moving || !grounded) { _stepTimer = 0f; return; }
            _stepTimer -= Time.deltaTime * Mathf.Max(0.5f, speedFactor);
            if (_stepTimer <= 0f)
            {
                PlayFootstep();
                _stepTimer = 0.44f;
            }
        }

        public void SetMusicIntensity(float v)
        {
            if (_music != null) _music.volume = Mathf.Clamp01(v) * 0.12f;
            if (_ambient != null) _ambient.volume = Mathf.Clamp01(v) * 0.17f;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && _sfx != null) _sfx.PlayOneShot(clip, volume);
        }

        private static float[] Buffer(float seconds) => new float[Mathf.Max(1, (int)(seconds * Rate))];

        private static AudioClip FromSamples(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeTone(string name, float dur, float f0, float f1, float vol, float noise)
        {
            var data = Buffer(dur);
            var rng = new System.Random(name.GetHashCode());
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                float f = Mathf.Lerp(f0, f1, t);
                phase += Mathf.PI * 2f * f / Rate;
                float env = Mathf.Pow(1f - t, 1.8f) * Mathf.Min(1f, t * 40f);
                float s = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2.01f) * 0.2f;
                float n = ((float)rng.NextDouble() * 2f - 1f) * noise;
                data[i] = (s * (1f - noise) + n) * env * vol;
            }
            return FromSamples(name, data);
        }

        private static AudioClip MakeClick(string name, float dur, float vol)
        {
            var data = Buffer(dur);
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                float env = Mathf.Exp(-t * 26f);
                float n = (float)rng.NextDouble() * 2f - 1f;
                float click = Mathf.Sign(Mathf.Sin(i * 0.35f)) * 0.25f;
                data[i] = (n * 0.6f + click) * env * vol;
            }
            return FromSamples(name, data);
        }

        private static AudioClip MakeNoise(string name, float dur, float vol, float lowpass)
        {
            var data = Buffer(dur);
            var rng = new System.Random(name.GetHashCode());
            float prev = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                float n = (float)rng.NextDouble() * 2f - 1f;
                prev = Mathf.Lerp(prev, n, lowpass);
                data[i] = prev * Mathf.Pow(1f - t, 2f) * vol;
            }
            return FromSamples(name, data);
        }

        private static AudioClip MakeExplosion()
        {
            float dur = 1.45f;
            var data = Buffer(dur);
            var rng = new System.Random(1337);
            float low = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                float n = (float)rng.NextDouble() * 2f - 1f;
                low = Mathf.Lerp(low, n, 0.025f);
                float rumble = Mathf.Sin(Mathf.PI * 2f * 42f * i / Rate) * 0.35f;
                float env = Mathf.Exp(-t * 3.1f) * Mathf.Min(1f, t * 60f);
                data[i] = (low * 1.5f + n * 0.25f + rumble) * env * 0.85f;
            }
            return FromSamples("explosion", data);
        }

        private static AudioClip MakeMelody(string name, float[] notes, float noteDur, float vol)
        {
            var data = Buffer(noteDur * notes.Length + 0.35f);
            for (int n = 0; n < notes.Length; n++)
            {
                int start = (int)(n * noteDur * Rate);
                int len = (int)(noteDur * Rate * 1.6f);
                for (int i = 0; i < len && start + i < data.Length; i++)
                {
                    float t = i / (float)len;
                    float env = Mathf.Exp(-t * 3.2f) * Mathf.Min(1f, t * 30f);
                    float phase = Mathf.PI * 2f * notes[n] * i / Rate;
                    float s = Mathf.Sin(phase) * 0.65f + Mathf.Sin(phase * 2f) * 0.15f;
                    data[start + i] += s * env * vol;
                }
            }
            return FromSamples(name, data);
        }

        private static AudioClip MakeWind()
        {
            float dur = 6f;
            var data = Buffer(dur);
            var rng = new System.Random(7);
            float low = 0f, low2 = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                float n = (float)rng.NextDouble() * 2f - 1f;
                low = Mathf.Lerp(low, n, 0.012f);
                low2 = Mathf.Lerp(low2, n, 0.002f);
                float mod = 0.55f + Mathf.Sin(t * Mathf.PI * 2f * 1.5f) * 0.18f + Mathf.Sin(t * Mathf.PI * 2f * 0.37f) * 0.12f;
                data[i] = (low * 0.55f + low2 * 0.9f) * mod * 0.30f;
            }
            // Crossfade the loop ends.
            int fade = (int)(0.35f * Rate);
            for (int i = 0; i < fade; i++)
            {
                float t = i / (float)fade;
                data[i] = Mathf.Lerp(data[data.Length - fade + i], data[i], t);
            }
            return FromSamples("wind", data);
        }

        // ------------------------------------------------------------------
        //  Balkan folk loops (7/8, hijaz / minor scales, frame drum)
        // ------------------------------------------------------------------

        private static readonly int[][] BalkanScales =
        {
            new[] { 0, 1, 4, 5, 7, 8, 11 },   // Hijaz (Srpsko kolo flavour)
            new[] { 0, 2, 3, 5, 7, 8, 11 },   // Harmonic minor (Dalmatian)
            new[] { 0, 1, 3, 5, 7, 8, 10 },   // Phrygian dominant (Sevdah)
        };

        private static readonly float[] BalkanTonic = { 146.83f, 130.81f, 123.47f }; // D3, C3, B2
        private static readonly float[] BalkanStep = { 0.215f, 0.195f, 0.172f };
        private static readonly int[] BalkanBars = { 6, 7, 8 };

        /// <summary>Builds a seamless 7/8 folk loop. Each level gets a different scale, tempo and groove.</summary>
        private static AudioClip MakeBalkanLoop(int level)
        {
            level = Mathf.Clamp(level, 0, 2);
            float tonic = BalkanTonic[level];
            int[] scale = BalkanScales[level];
            float step = BalkanStep[level];
            int bars = BalkanBars[level];
            int stepsTotal = bars * 7;
            int total = (int)(stepsTotal * step * Rate);
            var data = new float[total];
            var rng = new System.Random(4200 + level * 977);

            // ---- Drone (double stopped tonic + fifth, slow breathing) ----
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)Rate;
                float breathe = 0.55f + 0.20f * Mathf.Sin(t * Mathf.PI * 2f * 0.21f)
                                      + 0.10f * Mathf.Sin(t * Mathf.PI * 2f * 0.37f + 1.1f);
                float low = Mathf.Sin(Mathf.PI * 2f * (tonic * 0.5f) * t) * 0.30f;
                float root = Mathf.Sin(Mathf.PI * 2f * tonic * t) * 0.18f;
                float fifth = Mathf.Sin(Mathf.PI * 2f * tonic * 1.5f * t + 0.4f) * 0.10f;
                float shimmer = Mathf.Sin(Mathf.PI * 2f * tonic * 2f * t) * 0.035f;
                data[i] += (low + root + fifth + shimmer) * breathe * 0.34f;
            }

            // ---- Melody + bass + frame drum, one bar at a time ----
            int[] progression = { 0, 2, 4, 3, 1, 4, 2, 0 };
            int[][] rhythm =
            {
                new[] { 3, 4, 7, 10, 12 },       // slow, wide
                new[] { 2, 3, 5, 7, 9, 11, 13 }, // busier
                new[] { 2, 4, 5, 7, 8, 11, 12, 13 }, // dense finale
            };

            for (int bar = 0; bar < bars; bar++)
            {
                int rootDeg = progression[bar % progression.Length];
                int barStart = (int)(bar * 7 * step * Rate);
                int[] slots = rhythm[level];

                // Melody: pick a short motif from the scale, with an octave lift on accents.
                int[] motif = new int[slots.Length];
                for (int n = 0; n < slots.Length; n++)
                {
                    int lift = (n % 3 == 1) ? 2 : (rng.NextDouble() < 0.18 ? 4 : 0);
                    motif[n] = rootDeg + (n % 4) + lift;
                }
                for (int n = 0; n < slots.Length; n++)
                {
                    int slot = slots[n] % 14;
                    int start = barStart + (int)(slot * 0.5f * step * Rate);
                    float dur = step * (n % 4 == 3 ? 1.1f : 0.85f);
                    AddPluck(data, start, NoteFreq(scale, motif[n], tonic * 2f), dur, 0.34f);
                }

                // Bass answers on the strong beats of the 7/8 bar.
                int bassStart = barStart + (int)(3 * step * Rate);
                AddPluck(data, barStart, NoteFreq(scale, rootDeg, tonic * 0.5f), step * 2.6f, 0.30f);
                AddPluck(data, bassStart, NoteFreq(scale, rootDeg + 4, tonic * 0.5f), step * 1.8f, 0.26f);

                // Frame drum: dum - - tek - dum - (2+2+3 feel).
                AddDrum(data, barStart, 0.55f, true, rng);
                AddDrum(data, barStart + (int)(2 * step * Rate), 0.30f, false, rng);
                AddDrum(data, barStart + (int)(3 * step * Rate), 0.46f, true, rng);
                AddDrum(data, barStart + (int)(5 * step * Rate), 0.26f, false, rng);
                AddDrum(data, barStart + (int)(6 * step * Rate), 0.34f, false, rng);
            }

            // ---- A touch of space (loop-safe delay) ----
            int delay = (int)(0.115f * Rate);
            for (int i = 0; i < total; i++)
            {
                int src = i - delay;
                if (src < 0) src += total;
                data[i] += data[src] * 0.18f;
            }

            // ---- Normalise gently and crossfade the seam ----
            float peak = 0.0001f;
            for (int i = 0; i < total; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float gain = 0.72f / peak;
            for (int i = 0; i < total; i++) data[i] *= gain;

            int fade = (int)(0.30f * Rate);
            for (int i = 0; i < fade && i < total; i++)
            {
                float t = i / (float)fade;
                data[i] = Mathf.Lerp(data[total - fade + i], data[i], t);
            }

            return FromSamples("balkan_loop_" + level, data);
        }

        private static float NoteFreq(int[] scale, int degree, float tonic)
        {
            int n = scale.Length;
            int octave = Mathf.FloorToInt(degree / (float)n);
            int idx = degree - octave * n;
            return tonic * Mathf.Pow(2f, (scale[idx] + 12 * octave) / 12f);
        }

        private static void AddPluck(float[] data, int start, float freq, float dur, float vol)
        {
            int len = Mathf.Max(1, (int)(dur * Rate));
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                if (idx < 0 || idx >= data.Length) break;
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 5.2f) * Mathf.Min(1f, t * 260f);
                float phase = Mathf.PI * 2f * freq * t;
                float s = Mathf.Sin(phase) * 0.62f
                        + Mathf.Sin(phase * 2f) * 0.20f
                        + Mathf.Sin(phase * 3.01f) * 0.09f
                        + Mathf.Sin(phase * 4.02f) * 0.04f;
                data[idx] += s * env * vol;
            }
        }

        private static void AddDrum(float[] data, int start, float vol, bool low, System.Random rng)
        {
            int len = Mathf.Max(1, (int)((low ? 0.18f : 0.10f) * Rate));
            float filtered = 0f;
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                if (idx < 0 || idx >= data.Length) break;
                float t = i / (float)Rate;
                float n = (float)rng.NextDouble() * 2f - 1f;
                if (low)
                {
                    float thump = Mathf.Sin(Mathf.PI * 2f * (92f - 120f * t) * t);
                    data[idx] += (thump * 0.85f + n * 0.16f) * Mathf.Exp(-t * 20f) * vol;
                }
                else
                {
                    filtered = Mathf.Lerp(filtered, n, 0.45f);
                    data[idx] += (n - filtered) * Mathf.Exp(-t * 62f) * vol;
                }
            }
        }
    }
}
