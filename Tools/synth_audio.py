# Sintetiza todos los sonidos y la música de "1 % de Probabilidad" (estilo tarot nocturno).
# Todo se genera con matemáticas (ondas, ruido filtrado, reverberación): no usa grabaciones de terceros.
# Uso: python3 synth_audio.py <carpeta_salida>
import os, sys, subprocess
import numpy as np
from scipy.signal import butter, lfilter, fftconvolve

SR = 44100
OUT = sys.argv[1]
rng = np.random.default_rng(1)

def t_axis(dur): return np.arange(int(dur * SR)) / SR
def note(name):
    names = {'C':-9,'C#':-8,'Db':-8,'D':-7,'D#':-6,'Eb':-6,'E':-5,'F':-4,'F#':-3,'Gb':-3,'G':-2,'G#':-1,'Ab':-1,'A':0,'A#':1,'Bb':1,'B':2}
    p, o = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((names[p] + (o - 4) * 12) / 12)

def lowpass(x, cutoff, order=2):
    b, a = butter(order, cutoff / (SR / 2), 'low'); return lfilter(b, a, x)
def highpass(x, cutoff, order=2):
    b, a = butter(order, cutoff / (SR / 2), 'high'); return lfilter(b, a, x)
def bandpass(x, lo, hi, order=2):
    b, a = butter(order, [lo / (SR / 2), hi / (SR / 2)], 'band'); return lfilter(b, a, x)

def reverb(x, seconds=1.8, mix=0.3, tone=5000):
    n = int(seconds * SR)
    ir = rng.standard_normal(n) * np.exp(-np.arange(n) / SR * 6.0 / seconds)
    ir = lowpass(ir, tone); ir /= np.sqrt(np.sum(ir ** 2))
    wet = fftconvolve(x, ir)
    wet = np.concatenate([wet, np.zeros(len(x) + n - len(wet))])[: len(x) + n]
    dry = np.concatenate([x, np.zeros(n)])
    return dry * (1 - mix) + wet * mix

def bell(freq, dur, amp=1.0, bright=1.0):
    t = t_axis(dur)
    partials = [(1.0, 1.0, 3.0), (2.0, 0.45 * bright, 5.0), (2.76, 0.28 * bright, 7.0), (5.4, 0.12 * bright, 11.0), (8.93, 0.05 * bright, 16.0)]
    y = sum(a * np.sin(2 * np.pi * freq * r * t) * np.exp(-t * d) for r, a, d in partials)
    attack = np.minimum(1.0, t / 0.004)
    return amp * y * attack

def place(buffer, sound, start_sec):
    i = int(start_sec * SR); end = min(len(buffer), i + len(sound))
    buffer[i:end] += sound[: end - i]; return buffer

def finish(x, peak=0.9, fade_out=0.02):
    n = int(fade_out * SR)
    if n > 0 and len(x) > n: x[-n:] *= np.linspace(1, 0, n)
    m = np.max(np.abs(x)) or 1.0
    return x / m * peak

def save_wav(name, x):
    import wave
    path = os.path.join(OUT, 'Sfx', name + '.wav'); os.makedirs(os.path.dirname(path), exist_ok=True)
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())
    print(path, f"{len(x)/SR:.2f}s")

def save_ogg(name, x):
    import wave, tempfile
    path = os.path.join(OUT, 'Music', name + '.ogg'); os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = path + '.tmp.wav'
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(tmp, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', tmp, '-c:a', 'libvorbis', '-q:a', '4', path], check=True)
    os.remove(tmp); print(path, f"{len(x)/SR:.1f}s")

# ---------------------------------------------------------------- Efectos de sonido

def sfx_tap():
    t = t_axis(0.06)
    click = np.sin(2 * np.pi * 1700 * t) * np.exp(-t * 90)
    noise = highpass(rng.standard_normal(len(t)), 3000) * np.exp(-t * 200) * 0.25
    return finish(click + noise, 0.5)

def sfx_card_lift():
    buf = np.zeros(int(0.35 * SR))
    for i, n in enumerate(['E6', 'G#6', 'B6']):
        place(buf, bell(note(n), 0.3, 0.35, 0.6), i * 0.03)
    t = t_axis(0.2)
    swell = bandpass(rng.standard_normal(len(t)), 2500, 7000) * np.sin(np.pi * t / 0.2) * 0.15
    place(buf, swell, 0)
    return finish(reverb(buf, 1.2, 0.35), 0.55)

def sfx_card_flip():
    t = t_axis(0.11)
    env = np.sin(np.pi * np.minimum(1, t / 0.11)) ** 2
    swish = bandpass(rng.standard_normal(len(t)), 1200, 5000) * env
    return finish(swish, 0.6)

def sfx_win():
    buf = np.zeros(int(1.0 * SR))
    for i, n in enumerate(['D5', 'F#5', 'A5', 'D6']):
        place(buf, bell(note(n), 0.9, 1.0), i * 0.06)
    return finish(reverb(buf, 1.6, 0.3), 0.85)

def sfx_big_win():
    buf = np.zeros(int(2.0 * SR))
    seq = ['D5', 'F#5', 'A5', 'D6', 'F#6', 'A6', 'D7']
    for i, n in enumerate(seq):
        place(buf, bell(note(n), 1.2, 0.9), i * 0.055)
    for n in ['D5', 'F#5', 'A5']:
        place(buf, bell(note(n), 1.6, 0.6, 0.7), 0.45)
    t = t_axis(1.2)
    sparkle = highpass(rng.standard_normal(len(t)), 6000) * np.exp(-t * 3) * 0.08
    place(buf, sparkle * (rng.random(len(t)) > 0.97), 0.4)
    return finish(reverb(buf, 2.0, 0.35), 0.9)

def sfx_lose():
    buf = np.zeros(int(0.8 * SR))
    for i, (n, d) in enumerate([('A3', 0.25), ('F3', 0.45)]):
        t = t_axis(d + 0.2)
        f = note(n)
        tone = (np.sin(2 * np.pi * f * t) + 0.3 * np.sin(2 * np.pi * 2 * f * t)) * np.exp(-t * 6)
        place(buf, lowpass(tone, 900) * np.minimum(1, t / 0.01), i * 0.18)
    return finish(reverb(buf, 1.0, 0.25), 0.75)

def sfx_save():
    buf = np.zeros(int(0.7 * SR))
    place(buf, bell(note('A5'), 0.6, 0.8, 0.5), 0)
    place(buf, bell(note('E6'), 0.3, 0.25, 0.5), 0.05)
    return finish(reverb(buf, 1.2, 0.3), 0.6)

def sfx_achievement():
    buf = np.zeros(int(1.8 * SR))
    for i, n in enumerate(['D5', 'A5', 'E6', 'F#6']):
        b = bell(note(n), 1.5, 0.7, 0.8)
        t = t_axis(1.5)
        place(buf, b * np.minimum(1, t / 0.12), 0.05 * i)
    place(buf, bell(note('D7'), 0.8, 0.3), 0.3)
    return finish(reverb(buf, 2.2, 0.4), 0.85)

def sfx_shield_alert():
    t = t_axis(1.8)
    f = 98.0
    gong = sum(a * np.sin(2 * np.pi * f * r * t + p) * np.exp(-t * d) for r, a, d, p in
               [(1, 1, 1.8, 0), (1.52, 0.6, 2.4, 1), (2.11, 0.4, 3, 2), (2.83, 0.25, 4, 0.5), (3.9, 0.15, 6, 1.2)])
    gong *= np.minimum(1, t / 0.01)
    return finish(reverb(lowpass(gong, 2500), 2.0, 0.3), 0.85)

def sfx_shield_start():
    t = t_axis(0.5)
    sweep = rng.standard_normal(len(t))
    out = np.zeros_like(t)
    for k in range(5):  # barrido de ruido que sube
        lo = 400 * (2 ** k); seg = slice(int(len(t) * k / 5), int(len(t) * (k + 1) / 5))
        out[seg] = bandpass(sweep, lo, lo * 2.5)[seg]
    out *= np.sin(np.pi * t / 0.5) * 0.5
    buf = np.concatenate([out, np.zeros(int(0.4 * SR))])
    place(buf, bell(note('D6'), 0.5, 0.8), 0.42)
    return finish(reverb(buf, 1.2, 0.3), 0.75)

def sfx_shield_block():
    t = t_axis(0.4)
    ting = sum(a * np.sin(2 * np.pi * f * t) * np.exp(-t * d) for f, a, d in [(2350, 1, 9), (3720, 0.6, 13), (5210, 0.35, 18)])
    hit = highpass(rng.standard_normal(len(t)), 2000) * np.exp(-t * 120) * 0.6
    return finish(reverb(ting + hit, 0.8, 0.2), 0.8)

def sfx_timer_tick():
    t = t_axis(0.08)
    tick = np.sin(2 * np.pi * 950 * t) * np.exp(-t * 70) + bandpass(rng.standard_normal(len(t)), 1500, 4000) * np.exp(-t * 300) * 0.4
    return finish(tick, 0.6)

def sfx_victory():
    buf = np.zeros(int(3.6 * SR))
    chords = [(['D5', 'F#5', 'A5'], 0.0), (['G5', 'B5', 'D6'], 0.6), (['A5', 'C#6', 'E6'], 1.2), (['D6', 'F#6', 'A6', 'D5'], 1.8)]
    for notes, start in chords:
        for i, n in enumerate(notes):
            place(buf, bell(note(n), 1.6, 0.6), start + i * 0.04)
    return finish(reverb(buf, 2.4, 0.35), 0.9)

def sfx_bankrupt():
    t = t_axis(2.4)
    f = note('D3') * (1 - 0.25 * t / 2.4)  # cae lentamente
    phase = 2 * np.pi * np.cumsum(f) / SR
    tone = np.sin(phase) + 0.4 * np.sin(2 * phase) + 0.2 * np.sin(3 * phase)
    env = np.minimum(1, t / 0.05) * np.exp(-t * 0.9)
    return finish(reverb(lowpass(tone * env, 700), 2.0, 0.35), 0.8)

# ---------------------------------------------------------------- Música

def pad_voice(freqs, dur, attack=1.5, release=1.5):
    t = t_axis(dur)
    y = np.zeros_like(t)
    for f in freqs:
        for det in (-0.12, 0.0, 0.12):  # tres voces algo desafinadas: sonido más "aterciopelado"
            ff = f * 2 ** (det / 12)
            y += np.sin(2 * np.pi * ff * t) + 0.25 * np.sin(4 * np.pi * ff * t) + 0.08 * np.sin(6 * np.pi * ff * t)
    env = np.minimum(1, t / attack) * np.minimum(1, (dur - t) / release)
    return y * np.clip(env, 0, 1)

def music_table():
    loop = 48.0
    chords = [['D3', 'F3', 'A3'], ['Bb2', 'D3', 'F3'], ['G2', 'Bb2', 'D3'], ['A2', 'C#3', 'E3']] * 2
    tail = 6.0
    buf = np.zeros(int((loop + tail) * SR))
    for i, ch in enumerate(chords):
        place(buf, pad_voice([note(n) for n in ch], 7.5) * 0.18, i * 6.0 - 0.75 if i else 0)
    t = t_axis(loop + tail)
    buf += np.sin(2 * np.pi * note('D2') * t) * 0.10 * (0.8 + 0.2 * np.sin(2 * np.pi * t / 12))
    buf = lowpass(buf, 1400)
    # Caja de música: notas sueltas de la escala de re menor armónica, a intervalos irregulares.
    scale = ['D5', 'E5', 'F5', 'A5', 'C#6', 'D6', 'F6', 'A6']
    mb_rng = np.random.default_rng(7)
    pos = 1.0
    while pos < loop - 1.0:
        n = scale[mb_rng.integers(len(scale))]
        place(buf, bell(note(n), 2.0, mb_rng.uniform(0.10, 0.18), 0.5), pos)
        pos += mb_rng.choice([1.5, 2.0, 2.5, 3.0])
    wet = reverb(buf, 3.0, 0.45, 4000)
    # Bucle sin cortes: lo que suena después del final se suma al comienzo.
    n_loop = int(loop * SR)
    out = wet[:n_loop].copy()
    rest = wet[n_loop:]
    out[: len(rest)] += rest[: n_loop]
    return finish(out, 0.7, 0)

def music_shield():
    loop = 16.0  # 120 pulsos por minuto
    n = int(loop * SR)
    buf = np.zeros(n + int(2 * SR))
    beat = 0.5
    for i in range(int(loop / beat)):
        t = t_axis(0.35)
        kick = np.sin(2 * np.pi * (55 + 60 * np.exp(-t * 30)) * t) * np.exp(-t * 9)
        place(buf, kick * (0.9 if i % 2 == 0 else 0.6), i * beat)
        th = t_axis(0.05)
        hat = highpass(rng.standard_normal(len(th)), 7000) * np.exp(-th * 80) * 0.18
        place(buf, hat, i * beat + beat / 2)
    t = t_axis(loop + 2)
    drone = (np.sin(2 * np.pi * note('D2') * t) + 0.7 * np.sin(2 * np.pi * note('Eb2') * t)) * 0.12
    buf += lowpass(drone * (0.7 + 0.3 * np.sin(2 * np.pi * t / 4)), 500)
    for k, nn in enumerate(['D5', 'Eb5', 'D5', 'A4']):
        place(buf, bell(note(nn), 1.5, 0.12, 0.4), k * 4.0)
    wet = reverb(buf, 1.4, 0.25)
    out = wet[:n].copy()
    rest = wet[n:]
    out[: len(rest)] += rest[:n]
    return finish(out, 0.7, 0)

SFX = {
    'tap': sfx_tap, 'card_lift': sfx_card_lift, 'card_flip': sfx_card_flip, 'win': sfx_win, 'big_win': sfx_big_win,
    'lose': sfx_lose, 'save': sfx_save, 'achievement': sfx_achievement, 'shield_alert': sfx_shield_alert,
    'shield_start': sfx_shield_start, 'shield_block': sfx_shield_block, 'timer_tick': sfx_timer_tick,
    'victory': sfx_victory, 'bankrupt': sfx_bankrupt,
}
for name, fn in SFX.items():
    save_wav(name, fn())
save_ogg('music_table', music_table())
save_ogg('music_shield', music_shield())
