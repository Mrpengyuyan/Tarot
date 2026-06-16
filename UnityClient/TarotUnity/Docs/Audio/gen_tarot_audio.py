#!/usr/bin/env python3
"""Original royalty-free audio synthesis for Tarot Unity (pure stdlib, no numpy).

Generates a warm, calm "Moonlit Tarot" palette: 7 short interaction SFX + one
loopable ambient music bed. All sound is synthesized from scratch here, so it is
100% original / royalty-free (no third-party licensing) - the safest option for a
downloadable game. Outputs 16-bit mono WAV into the Unity project.
"""
import math, random, wave, struct, os

RATE = 44100
SFX_DIR = "Assets/Audio/SFX"
MUSIC_DIR = "Assets/Audio/Music"
random.seed(7)  # deterministic output


def write_wav(path, samples):
    peak = max(1e-9, max(abs(s) for s in samples))
    # leave a touch of headroom; only normalize down if hot
    norm = 0.97 / peak if peak > 0.97 else 1.0
    frames = bytearray()
    for s in samples:
        v = int(max(-1.0, min(1.0, s * norm)) * 32767)
        frames += struct.pack("<h", v)
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(bytes(frames))
    rms = math.sqrt(sum(s * s for s in samples) / len(samples))
    print(f"  {os.path.basename(path):28} {len(samples)/RATE:5.2f}s  peak={peak:5.3f} rms={rms:5.3f} norm={norm:4.2f}")


def env_exp(n, attack, decay):
    """Attack then exponential decay envelope over n samples (times in seconds)."""
    a = max(1, int(attack * RATE))
    out = []
    for i in range(n):
        if i < a:
            e = i / a
        else:
            e = math.exp(-(i - a) / (decay * RATE))
        out.append(e)
    return out


def bell(freq, dur, partials=((1, 1.0, 1.0), (2.01, 0.5, 0.8), (2.99, 0.32, 0.6),
                              (4.21, 0.22, 0.45), (5.43, 0.12, 0.3)), attack=0.004):
    """Inharmonic bell/chime: partials = (ratio, amp, decay_scale)."""
    n = int(dur * RATE)
    out = [0.0] * n
    for ratio, amp, dscale in partials:
        env = env_exp(n, attack, dur * 0.45 * dscale)
        w = 2 * math.pi * freq * ratio / RATE
        for i in range(n):
            out[i] += amp * math.sin(w * i) * env[i]
    return out


def lowpass(samples, cutoff):
    """One-pole lowpass; cutoff in Hz (rough)."""
    a = math.exp(-2 * math.pi * cutoff / RATE)
    out = []
    y = 0.0
    for x in samples:
        y = (1 - a) * x + a * y
        out.append(y)
    return out


def reverb_tail(samples, taps=((0.035, 0.5), (0.071, 0.32), (0.113, 0.2), (0.157, 0.12))):
    """Cheap multi-tap reverb for space."""
    n = len(samples)
    extra = int(0.25 * RATE)
    out = list(samples) + [0.0] * extra
    for delay, gain in taps:
        d = int(delay * RATE)
        for i in range(n):
            if i + d < len(out):
                out[i + d] += samples[i] * gain
    return out


def mix(into, src, at=0):
    for i, s in enumerate(src):
        idx = at + i
        while idx >= len(into):
            into.append(0.0)
        into[idx] += s


# ---- SFX -------------------------------------------------------------------

def menu_open():
    # warm low bell swell, welcoming
    s = bell(196.0, 1.05, attack=0.03)
    s = [v * 0.9 for v in s]
    s2 = bell(392.0, 0.8, attack=0.04)  # soft octave shimmer
    mix(s, [v * 0.25 for v in s2])
    return reverb_tail(s)


def spread_selected():
    # two soft ascending mallet notes (confirm)
    out = []
    mix(out, bell(392.0, 0.32, attack=0.003), at=0)
    mix(out, [v * 0.9 for v in bell(587.33, 0.42, attack=0.003)], at=int(0.10 * RATE))
    return reverb_tail([v * 0.8 for v in out], taps=((0.04, 0.3), (0.09, 0.15)))


def shuffle_started():
    # riffle: a run of short band-limited noise ticks
    dur = 0.8
    n = int(dur * RATE)
    out = [0.0] * n
    ticks = 9
    for t in range(ticks):
        pos = int((0.05 + 0.085 * t) * RATE)
        tn = int(0.05 * RATE)
        env = env_exp(tn, 0.002, 0.02)
        for i in range(tn):
            if pos + i < n:
                out[pos + i] += random.uniform(-1, 1) * env[i]
    out = lowpass(out, 3500)
    out = lowpass(out, 3500)          # ~bandpass-ish via two passes + subtract
    # overall gentle swell-fade
    for i in range(n):
        swell = math.sin(math.pi * i / n)
        out[i] *= (0.4 + 0.6 * swell) * 0.9
    return out


def card_dealt():
    # soft place: quick noise transient + low woody thud
    n = int(0.22 * RATE)
    out = [0.0] * n
    tn = int(0.05 * RATE)
    env = env_exp(tn, 0.001, 0.02)
    for i in range(tn):
        out[i] += random.uniform(-1, 1) * env[i] * 0.6
    out = lowpass(out, 5000)
    thud = bell(120.0, 0.18, partials=((1, 1.0, 1.0), (2.0, 0.3, 0.7)), attack=0.002)
    mix(out, [v * 0.7 for v in thud])
    return out


def card_flipped():
    # whoosh (rising lowpass noise) + tiny bright tick at end
    n = int(0.32 * RATE)
    noise = [random.uniform(-1, 1) for _ in range(n)]
    out = []
    y = 0.0
    for i in range(n):
        cutoff = 400 + 6000 * (i / n)          # rising brightness
        a = math.exp(-2 * math.pi * cutoff / RATE)
        y = (1 - a) * noise[i] + a * y
        env = math.sin(math.pi * i / n) ** 1.5  # swell-fade
        out.append(y * env * 0.8)
    tick = bell(784.0, 0.16, attack=0.002)
    mix(out, [v * 0.35 for v in tick], at=int(0.18 * RATE))
    return out


def result_ready():
    # anticipatory rising pad
    n = int(0.85 * RATE)
    out = [0.0] * n
    base = 261.63
    for ratio, amp in ((1, 1.0), (1.5, 0.5), (2.0, 0.3)):
        for i in range(n):
            glide = 1.0 + 0.06 * (i / n)        # slight upward glide
            out[i] += amp * math.sin(2 * math.pi * base * ratio * glide * i / RATE)
    for i in range(n):
        out[i] *= (i / n) ** 1.3 * math.exp(-max(0, i - 0.7 * n) / (0.15 * RATE)) * 0.5
    return lowpass(out, 4000)


def result_reveal():
    # bright bell cascade + soft sparkle (reward)
    out = []
    notes = [(440.0, 0.0), (523.25, 0.12), (659.25, 0.24), (880.0, 0.38)]
    for f, t in notes:
        mix(out, [v * 0.8 for v in bell(f, 1.2, attack=0.003)], at=int(t * RATE))
    # high decaying sparkle
    n = len(out)
    sp = [0.0] * n
    senv = env_exp(n, 0.05, 0.5)
    for i in range(n):
        sp[i] = random.uniform(-1, 1) * senv[i]
    sp = lowpass(sp, 9000)
    # remove low end of sparkle (crude highpass = x - lowpass(x))
    spl = lowpass(sp, 2000)
    for i in range(n):
        out[i] += (sp[i] - spl[i]) * 0.18
    return reverb_tail(out)


# ---- Ambient music ---------------------------------------------------------

def ambient_music():
    dur = 24.0                      # loopable bed
    n = int(dur * RATE)
    out = [0.0] * n
    # A-minor-ish warm drone: A2 E3 A3 C4, each a detuned sine pair (beating)
    chord = [110.0, 164.81, 220.0, 261.63]
    detune = 0.6                    # Hz
    amps = [0.5, 0.34, 0.3, 0.22]
    for f, amp in zip(chord, amps):
        for df in (-detune, +detune):
            w = 2 * math.pi * (f + df) / RATE
            for i in range(n):
                out[i] += amp * 0.5 * math.sin(w * i)
    # slow breathing LFO (2 full cycles over the loop -> seamless)
    for i in range(n):
        lfo = 0.78 + 0.22 * math.sin(2 * math.pi * 2 * i / n)
        out[i] *= lfo
    out = lowpass(out, 1800)        # keep it warm
    # gentle high shimmer (soft, slow-modulated high partial)
    sh = [0.0] * n
    for i in range(n):
        m = 0.5 + 0.5 * math.sin(2 * math.pi * 3 * i / n)
        sh[i] = 0.05 * m * math.sin(2 * math.pi * 1318.5 * i / RATE)
    for i in range(n):
        out[i] += sh[i]
    # ensure seamless loop: short equal-power crossfade of head into tail
    xf = int(0.5 * RATE)
    for i in range(xf):
        g = i / xf
        out[n - xf + i] = out[n - xf + i] * (1 - g) + out[i] * g
    return out


def main():
    os.makedirs(SFX_DIR, exist_ok=True)
    os.makedirs(MUSIC_DIR, exist_ok=True)
    print("SFX:")
    write_wav(f"{SFX_DIR}/SFX_MenuOpen.wav", menu_open())
    write_wav(f"{SFX_DIR}/SFX_SpreadSelected.wav", spread_selected())
    write_wav(f"{SFX_DIR}/SFX_ShuffleStarted.wav", shuffle_started())
    write_wav(f"{SFX_DIR}/SFX_CardDealt.wav", card_dealt())
    write_wav(f"{SFX_DIR}/SFX_CardFlipped.wav", card_flipped())
    write_wav(f"{SFX_DIR}/SFX_ResultReady.wav", result_ready())
    write_wav(f"{SFX_DIR}/SFX_ResultReveal.wav", result_reveal())
    print("Music:")
    write_wav(f"{MUSIC_DIR}/MUS_AmbientMoonlit.wav", ambient_music())
    print("done.")


if __name__ == "__main__":
    main()
