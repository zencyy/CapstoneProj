using UnityEngine;

namespace Env3.Anxiety
{
    /// <summary>
    /// Synthesises the encounter's audio so the scene needs no imported clips.
    /// Assign real clips on AnxietyDialogueController to override any of these.
    /// </summary>
    public static class Env3Audio
    {
        const int SampleRate = 44100;

        static AudioClip _heartbeat;
        static AudioClip _tick;
        static AudioClip _drone;

        public static AudioClip Heartbeat
        {
            get { if (_heartbeat == null) _heartbeat = BuildHeartbeat(); return _heartbeat; }
        }

        public static AudioClip Tick
        {
            get { if (_tick == null) _tick = BuildTick(); return _tick; }
        }

        public static AudioClip Drone
        {
            get { if (_drone == null) _drone = BuildDrone(); return _drone; }
        }

        /// <summary>Lub-dub: two pitch-falling low thumps.</summary>
        static AudioClip BuildHeartbeat()
        {
            const float length = 0.9f;
            int count = Mathf.RoundToInt(SampleRate * length);
            var data = new float[count];

            AddThump(data, 0.00f, 0.20f, 62f, 38f, 1.00f);
            AddThump(data, 0.26f, 0.24f, 52f, 32f, 0.72f);

            var clip = AudioClip.Create("Env3_Heartbeat", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void AddThump(float[] data, float startSec, float durSec, float startHz, float endHz, float gain)
        {
            int start = Mathf.RoundToInt(startSec * SampleRate);
            int dur = Mathf.RoundToInt(durSec * SampleRate);
            float phase = 0f;

            for (int i = 0; i < dur; i++)
            {
                int idx = start + i;
                if (idx < 0 || idx >= data.Length) continue;

                float t = i / (float)dur;
                float hz = Mathf.Lerp(startHz, endHz, t * t);
                phase += 2f * Mathf.PI * hz / SampleRate;

                float env = Mathf.Exp(-5.5f * t) * (1f - Mathf.Exp(-90f * t)); // fast attack, long tail
                float body = Mathf.Sin(phase);
                float transient = (Random.value * 2f - 1f) * Mathf.Exp(-140f * t) * 0.18f;

                data[idx] += (body * env + transient) * gain * 0.9f;
            }
        }

        /// <summary>Dry UI click used for typewriter and choice reveals.</summary>
        static AudioClip BuildTick()
        {
            int count = Mathf.RoundToInt(SampleRate * 0.055f);
            var data = new float[count];
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                phase += 2f * Mathf.PI * Mathf.Lerp(1400f, 700f, t) / SampleRate;
                float env = Mathf.Exp(-26f * t);
                data[i] = (Mathf.Sin(phase) * 0.55f + (Random.value * 2f - 1f) * 0.45f) * env * 0.5f;
            }

            var clip = AudioClip.Create("Env3_Tick", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Seamless low drone that swells under the panic stage.</summary>
        static AudioClip BuildDrone()
        {
            const float length = 4f;
            int count = Mathf.RoundToInt(SampleRate * length);
            var data = new float[count];

            // Each frequency completes a whole number of cycles over the 4s clip (160, 240, 362)
            // so the loop point lands on zero. 90.5Hz is detuned against 60Hz to get some beating.
            float[] hz = { 40f, 60f, 90.5f };

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float lfo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                float s = Mathf.Sin(2f * Mathf.PI * hz[0] * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * hz[1] * t) * 0.28f
                        + Mathf.Sin(2f * Mathf.PI * hz[2] * t) * 0.14f;
                data[i] = s * lfo * 0.32f;
            }

            // Short crossfade across the seam to kill any residual click.
            int fade = Mathf.RoundToInt(SampleRate * 0.02f);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[count - 1 - i] *= k;
            }

            var clip = AudioClip.Create("Env3_Drone", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
