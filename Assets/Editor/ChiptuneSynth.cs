using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Synthesizes retro sound effects and a looping chiptune track as WAV files (no external assets needed).
public static class ChiptuneSynth
{
    const int SampleRate = 22050;
    const string AudioDir = "Assets/Audio";

    public struct Clips
    {
        public AudioClip music, bossMusic, dangerMusic, swing, hit, enemyDie, hurt, pickup, victory, gameOver, fireball, itemGet,
            menuMove, menuSelect, zap, burn;
    }

    public static Clips GenerateAll()
    {
        Directory.CreateDirectory(AudioDir);
        return new Clips
        {
            swing = Save("Swing", Swing()),
            hit = Save("Hit", Slide(700, 180, 0.11f, 0.5f, 0.35f)),
            enemyDie = Save("EnemyDie", EnemyDie()),
            hurt = Save("Hurt", Hurt()),
            pickup = Save("Pickup", Notes(new[] { "C6", "E6", "G6", "C7" }, 0.06f, 0.25f, 0.3f)),
            victory = Save("Victory", Notes(new[] { "C5", "E5", "G5", "C6", "-", "G5", "C6", "C6" }, 0.13f, 0.5f, 0.3f)),
            gameOver = Save("GameOver", Notes(new[] { "E5", "D#5", "D5", "C#5", "C5", "C5", "C5", "-" }, 0.18f, 0.5f, 0.3f)),
            fireball = Save("Fireball", FireballWhoosh()),
            itemGet = Save("ItemGet", Notes(new[] { "G5", "A5", "B5", "D6", "-", "B5", "D6", "G6", "G6", "G6" }, 0.09f, 0.25f, 0.3f)),
            zap = Save("Zap", Zap()),
            burn = Save("Burn", Burn()),
            menuMove = Save("MenuMove", Notes(new[] { "E6" }, 0.035f, 0.5f, 0.2f)),
            menuSelect = Save("MenuSelect", Notes(new[] { "C6", "G6" }, 0.05f, 0.25f, 0.25f)),
            music = Save("Music", Music()),
            bossMusic = Save("BossMusic", BossMusic()),
            dangerMusic = Save("DangerMusic", DangerMusic()),
        };
    }

    // ---------- Sound effects ----------

    static float[] Swing()
    {
        // Filtered noise "whoosh" with a cutoff that sweeps down.
        int n = (int)(0.16f * SampleRate);
        var s = new float[n];
        var rng = new System.Random(1);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            float cutoff = Mathf.Lerp(0.6f, 0.05f, k);
            float noise = (float)rng.NextDouble() * 2f - 1f;
            lp += cutoff * (noise - lp);
            float env = Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.6f));
            s[i] = lp * env * 0.9f;
        }
        return s;
    }

    static float[] EnemyDie()
    {
        int n = (int)(0.3f * SampleRate);
        var s = new float[n];
        var rng = new System.Random(2);
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            float freq = Mathf.Lerp(500f, 60f, k);
            phase += freq / SampleRate;
            float square = Square(phase, 0.5f);
            float noise = (float)rng.NextDouble() * 2f - 1f;
            float env = 1f - k;
            s[i] = (square * 0.5f + noise * 0.5f * (1f - k)) * env * 0.4f;
        }
        return s;
    }

    static float[] Hurt()
    {
        int n = (int)(0.28f * SampleRate);
        var s = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            float t = (float)i / SampleRate;
            float freq = Mathf.Lerp(380f, 110f, k) * (1f + 0.08f * Mathf.Sin(t * 2f * Mathf.PI * 30f));
            phase += freq / SampleRate;
            s[i] = Square(phase, 0.5f) * (1f - k) * 0.35f;
        }
        return s;
    }

    static float[] FireballWhoosh()
    {
        // Rising noise burst layered with a low square growl.
        int n = (int)(0.35f * SampleRate);
        var s = new float[n];
        var rng = new System.Random(4);
        float lp = 0f, phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            lp += Mathf.Lerp(0.05f, 0.4f, k) * (noise - lp);
            phase += Mathf.Lerp(90f, 160f, k) / SampleRate;
            float env = Mathf.Sin(Mathf.PI * k);
            s[i] = (lp * 0.8f + Square(phase, 0.5f) * 0.15f) * env * 0.6f;
        }
        return s;
    }

    static float[] Zap()
    {
        // Crackling upward sweep: thin pulse wave rising fast, with a little noise.
        int n = (int)(0.18f * SampleRate);
        var s = new float[n];
        var rng = new System.Random(5);
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            phase += Mathf.Lerp(500f, 1800f, Mathf.Sqrt(k)) / SampleRate;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            float env = (1f - k) * (1f - k);
            s[i] = (Square(phase, 0.125f) * 0.7f + noise * 0.3f) * env * 0.3f;
        }
        return s;
    }

    static float[] Burn()
    {
        // A whoosh of flame: low-passed noise swelling then dying away, with random crackling pops.
        int n = (int)(0.45f * SampleRate);
        var s = new float[n];
        var rng = new System.Random(7);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            lp += 0.15f * (noise - lp);
            float env = Mathf.Min(1f, k * 8f) * (1f - k);
            float pop = rng.NextDouble() < 0.004 ? (float)rng.NextDouble() * 0.8f : 0f;
            s[i] = (lp * 1.6f + pop) * env * 0.45f;
        }
        return s;
    }

    static float[] Slide(float from, float to, float duration, float duty, float volume)
    {
        int n = (int)(duration * SampleRate);
        var s = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            phase += Mathf.Lerp(from, to, k) / SampleRate;
            s[i] = Square(phase, duty) * (1f - k) * volume;
        }
        return s;
    }

    static float[] Notes(string[] notes, float noteLength, float duty, float volume)
    {
        int per = (int)(noteLength * SampleRate);
        var s = new float[per * notes.Length + SampleRate / 10];
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] == "-") continue;
            // Repeated notes are held rather than retriggered.
            int len = 1;
            while (i + len < notes.Length && notes[i + len] == notes[i]) len++;
            AddTone(s, i * per, per * len, Freq(notes[i]), Wave.Square, duty, volume);
            i += len - 1;
        }
        return s;
    }

    // ---------- Music ----------

    // An original adventure-style loop in A minor, 16 bars of eighth notes. '.' holds the previous note, '-' is a rest.
    static readonly string[] Melody =
    {
        "A4 - C5 E5 A5 . G5 E5",
        "F5 . E5 C5 A4 . . -",
        "G4 C5 E5 G5 . E5 C5 E5",
        "D5 . . B4 G4 . . -",
        "A4 C5 E5 A5 B5 . C6 .",
        "A5 . F5 . C5 . F5 .",
        "G5 . D5 . B4 D5 G5 .",
        "G#5 . . E5 B4 . E5 -",
        "C6 . A5 F5 C5 . F5 A5",
        "G5 . E5 C5 G4 . C5 E5",
        "F5 . D5 A4 F4 . A4 D5",
        "E5 . B4 G#4 E4 . G#4 B4",
        "A4 . C5 . E5 . A5 .",
        "F5 . A5 . C6 . A5 F5",
        "G5 F5 E5 D5 B4 . D5 .",
        "E5 . . . . . - -",
    };

    // Bass roots per bar (root, fifth).
    static readonly string[,] Bass =
    {
        { "A2", "E3" }, { "F2", "C3" }, { "C3", "G3" }, { "G2", "D3" },
        { "A2", "E3" }, { "F2", "C3" }, { "G2", "D3" }, { "E2", "B2" },
        { "F2", "C3" }, { "C3", "G3" }, { "D3", "A3" }, { "E2", "B2" },
        { "A2", "E3" }, { "F2", "C3" }, { "G2", "D3" }, { "E2", "B2" },
    };

    static float[] Music()
    {
        const float bpm = 140f;
        float eighth = 60f / bpm / 2f;
        int eighthSamples = (int)(eighth * SampleRate);
        int bars = Melody.Length;
        var s = new float[eighthSamples * 8 * bars];

        // Lead: 25% pulse wave.
        var tokens = new List<string>();
        foreach (var bar in Melody) tokens.AddRange(bar.Split(' '));
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] == "." || tokens[i] == "-") continue;
            int len = 1;
            while (i + len < tokens.Count && tokens[i + len] == ".") len++;
            AddTone(s, i * eighthSamples, len * eighthSamples - eighthSamples / 8, Freq(tokens[i]), Wave.Square, 0.25f, 0.16f);
        }

        // Bass: triangle wave on quarter notes, root-root-fifth-root.
        for (int bar = 0; bar < bars; bar++)
        {
            string root = Bass[bar, 0], fifth = Bass[bar, 1];
            string[] pattern = { root, root, fifth, root };
            for (int q = 0; q < 4; q++)
            {
                int start = (bar * 8 + q * 2) * eighthSamples;
                AddTone(s, start, eighthSamples * 2 - eighthSamples / 4, Freq(pattern[q]), Wave.Triangle, 0.5f, 0.3f);
            }
        }

        // Hi-hat: short noise ticks on every off-beat eighth.
        var rng = new System.Random(3);
        int hatLen = SampleRate / 40;
        for (int e = 1; e < bars * 8; e += 2)
        {
            int start = e * eighthSamples;
            for (int i = 0; i < hatLen && start + i < s.Length; i++)
                s[start + i] += ((float)rng.NextDouble() * 2f - 1f) * (1f - (float)i / hatLen) * 0.05f;
        }

        return s;
    }

    // ---------- Boss music ----------

    // A slow, uneasy loop: chromatic bass ostinato with tritone leaps, a thin high lead, and a heartbeat drum.
    static readonly string[] BossMelody =
    {
        "E5 . . . . . F5 .",
        "E5 . . . A#4 . . .",
        "F5 . . . . . F#5 .",
        "F5 . . . B4 . . .",
        "E6 . D#6 . E6 . A#5 .",
        "B5 . A#5 . G5 . E5 .",
        "D#5 . E5 . F5 . F#5 .",
        "G5 . . . - - - -",
    };

    static readonly string[] BossBass =
    {
        "E2 E2 F2 E2 E2 A#2 A2 F2",
        "E2 E2 F2 E2 E2 A#2 A2 F2",
        "F2 F2 F#2 F2 F2 B2 A#2 F#2",
        "F2 F2 F#2 F2 F2 B2 A#2 F#2",
        "E2 E2 F2 E2 E2 A#2 A2 F2",
        "E2 E2 F2 E2 E2 A#2 A2 F2",
        "D#2 D#2 E2 D#2 D#2 A2 G#2 E2",
        "D#2 D#2 E2 D#2 D#2 A2 G#2 E2",
    };

    static float[] BossMusic()
    {
        const float bpm = 112f;
        float eighth = 60f / bpm / 2f;
        int eighthSamples = (int)(eighth * SampleRate);
        int bars = BossMelody.Length;
        var s = new float[eighthSamples * 8 * bars];

        // Lead: thin 12.5% pulse, quiet and high.
        var tokens = new List<string>();
        foreach (var bar in BossMelody) tokens.AddRange(bar.Split(' '));
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] == "." || tokens[i] == "-") continue;
            int len = 1;
            while (i + len < tokens.Count && tokens[i + len] == ".") len++;
            AddTone(s, i * eighthSamples, len * eighthSamples - eighthSamples / 8, Freq(tokens[i]), Wave.Square, 0.125f, 0.11f);
        }

        // Bass: gritty square eighth notes.
        for (int bar = 0; bar < bars; bar++)
        {
            var notes = BossBass[bar].Split(' ');
            for (int e = 0; e < notes.Length; e++)
            {
                int start = (bar * 8 + e) * eighthSamples;
                AddTone(s, start, eighthSamples - eighthSamples / 4, Freq(notes[e]), Wave.Square, 0.5f, 0.12f);
            }
        }

        // Heartbeat: a low "lub-dub" thump at the start of every bar.
        int thumpLen = SampleRate / 8;
        for (int bar = 0; bar < bars; bar++)
        {
            foreach (int offset in new[] { 0, eighthSamples })
            {
                int start = bar * 8 * eighthSamples + offset;
                float phase = 0f;
                for (int i = 0; i < thumpLen && start + i < s.Length; i++)
                {
                    float k = (float)i / thumpLen;
                    phase += Mathf.Lerp(90f, 40f, k) / SampleRate;
                    s[start + i] += Mathf.Sin(phase * 2f * Mathf.PI) * (1f - k) * (offset == 0 ? 0.45f : 0.3f);
                }
            }
        }

        return s;
    }

    // ---------- Danger music (low health) ----------

    // Fast and urgent: hammering repeated notes over a pounding octave bass, with kick and snare.
    static readonly string[] DangerMelody =
    {
        "D5 D5 - D5 F5 . E5 .",
        "D5 D5 - D5 A5 . G#5 .",
        "D5 D5 - D5 F5 . E5 D5",
        "C#5 . E5 . A4 . . -",
        "F5 F5 - F5 A5 . G5 .",
        "F5 F5 - F5 C6 . B5 .",
        "A5 G#5 A5 B5 C6 C#6 D6 .",
        "D6 . A5 . D5 . - -",
    };

    static readonly string[] DangerBass =
    {
        "D2 D3 D2 D3 D2 D3 D2 D3",
        "D2 D3 D2 D3 D2 D3 D2 D3",
        "D2 D3 D2 D3 D2 D3 D2 D3",
        "A1 A2 A1 A2 A1 A2 C#2 E2",
        "F2 F3 F2 F3 F2 F3 F2 F3",
        "F2 F3 F2 F3 F2 F3 F2 F3",
        "E2 E3 E2 E3 G#2 G#3 A2 A3",
        "D2 D3 D2 D3 A1 A2 D2 -",
    };

    static float[] DangerMusic()
    {
        const float bpm = 168f;
        float eighth = 60f / bpm / 2f;
        int eighthSamples = (int)(eighth * SampleRate);
        int bars = DangerMelody.Length;
        var s = new float[eighthSamples * 8 * bars];

        // Lead: bright 25% pulse; repeated notes are re-struck for urgency.
        var tokens = new List<string>();
        foreach (var bar in DangerMelody) tokens.AddRange(bar.Split(' '));
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] == "." || tokens[i] == "-") continue;
            int len = 1;
            while (i + len < tokens.Count && tokens[i + len] == ".") len++;
            AddTone(s, i * eighthSamples, len * eighthSamples - eighthSamples / 4, Freq(tokens[i]), Wave.Square, 0.25f, 0.13f);
        }

        // Bass: octave-jumping triangle on every eighth.
        for (int bar = 0; bar < bars; bar++)
        {
            var notes = DangerBass[bar].Split(' ');
            for (int e = 0; e < notes.Length; e++)
            {
                if (notes[e] == "-") continue;
                AddTone(s, (bar * 8 + e) * eighthSamples, eighthSamples - eighthSamples / 5, Freq(notes[e]), Wave.Triangle, 0.5f, 0.3f);
            }
        }

        // Drums: kick on beats 1 and 3, noisy snare on 2 and 4, soft hats on the off-beats.
        var rng = new System.Random(6);
        for (int beat = 0; beat < bars * 4; beat++)
        {
            int start = beat * 2 * eighthSamples;
            if (beat % 2 == 0)
            {
                int len = SampleRate / 10;
                float phase = 0f;
                for (int i = 0; i < len && start + i < s.Length; i++)
                {
                    float k = (float)i / len;
                    phase += Mathf.Lerp(120f, 45f, k) / SampleRate;
                    s[start + i] += Mathf.Sin(phase * 2f * Mathf.PI) * (1f - k) * 0.4f;
                }
            }
            else
            {
                int len = SampleRate / 12;
                for (int i = 0; i < len && start + i < s.Length; i++)
                    s[start + i] += ((float)rng.NextDouble() * 2f - 1f) * (1f - (float)i / len) * 0.18f;
            }

            int hat = start + eighthSamples;
            int hatLen = SampleRate / 50;
            for (int i = 0; i < hatLen && hat + i < s.Length; i++)
                s[hat + i] += ((float)rng.NextDouble() * 2f - 1f) * (1f - (float)i / hatLen) * 0.05f;
        }

        return s;
    }

    // ---------- Helpers ----------

    enum Wave { Square, Triangle }

    static void AddTone(float[] buffer, int start, int length, float freq, Wave wave, float duty, float volume)
    {
        int attack = SampleRate / 200;  // 5 ms
        int release = SampleRate / 50;  // 20 ms
        float phase = 0f;
        for (int i = 0; i < length && start + i < buffer.Length; i++)
        {
            phase += freq / SampleRate;
            float env = 1f;
            if (i < attack) env = (float)i / attack;
            else if (i > length - release) env = Mathf.Max(0f, (float)(length - i) / release);
            env *= Mathf.Lerp(1f, 0.7f, Mathf.Min(1f, (float)i / (SampleRate * 0.2f))); // gentle decay
            float v = wave == Wave.Square ? Square(phase, duty) : Triangle(phase);
            buffer[start + i] += v * env * volume;
        }
    }

    static float Square(float phase, float duty) => (phase - Mathf.Floor(phase)) < duty ? 1f : -1f;

    static float Triangle(float phase)
    {
        float p = phase - Mathf.Floor(phase);
        return 4f * Mathf.Abs(p - 0.5f) - 1f;
    }

    static readonly Dictionary<string, int> NoteIndex = new Dictionary<string, int>
    {
        { "C", 0 }, { "C#", 1 }, { "D", 2 }, { "D#", 3 }, { "E", 4 }, { "F", 5 },
        { "F#", 6 }, { "G", 7 }, { "G#", 8 }, { "A", 9 }, { "A#", 10 }, { "B", 11 },
    };

    static float Freq(string note)
    {
        string name = note.Substring(0, note.Length - 1);
        int octave = note[note.Length - 1] - '0';
        int midi = (octave + 1) * 12 + NoteIndex[name];
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }

    static AudioClip Save(string name, float[] samples)
    {
        string path = $"{AudioDir}/{name}.wav";
        WriteWav(path, samples);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    static void WriteWav(string path, float[] samples)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            int dataSize = samples.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataSize);
            w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);              // PCM
            w.Write((short)1);              // mono
            w.Write(SampleRate);
            w.Write(SampleRate * 2);        // byte rate
            w.Write((short)2);              // block align
            w.Write((short)16);             // bits per sample
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataSize);
            foreach (float f in samples)
                w.Write((short)(Mathf.Clamp(f, -1f, 1f) * short.MaxValue));
        }
    }
}
