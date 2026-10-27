"""전자기파로 보는 세상 - 임시 효과음 합성 스크립트.

외부 음원 없이 코드로 직접 합성한다 (저작권 문제 없음).
서브 콘텐츠라 BGM 없이 짧고 가벼운 효과음만 만든다.
콘셉트: 전자기파 - 맑은 사인파, 주파수가 오르내리는 "파동" 느낌, 살짝 반짝이는 배음.

실행: python Tools/gen_sfx.py   (프로젝트 폴더 ElectromagneticWolrd/ 에서)
결과: Assets/StreamingAssets/Audio/sfx_*.wav (44.1kHz, 16bit, mono)
만든 뒤 각 파일의 피크/RMS와 앞뒤 끝 값(틱 소리 여부)을 출력한다.
"""
import os
import wave

import numpy as np

SR = 44100
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "Assets", "StreamingAssets", "Audio")

PEAK_LIMIT = 10 ** (-1.0 / 20)   # 피크 상한 -1 dBFS
TARGET_RMS_DB = -20.0            # 소리가 나는 구간의 RMS 목표 (파일 사이 크기 맞춤)


def t_axis(seconds):
    return np.arange(int(SR * seconds)) / SR


def sweep(f0, f1, seconds, curve=1.0):
    """f0 → f1 로 미끄러지는 사인파 (위상 누적으로 끊김 없이)."""
    t = t_axis(seconds)
    k = (t / seconds) ** curve
    freq = f0 + (f1 - f0) * k
    phase = 2 * np.pi * np.cumsum(freq) / SR
    return np.sin(phase), freq


def env(n, attack, decay_tau=None, release=None):
    """attack(초) 선형 상승 + 지수 감쇠(decay_tau) 또는 끝부분 release(초) 하강."""
    t = np.arange(n) / SR
    e = np.minimum(1.0, t / max(attack, 1e-4))
    if decay_tau:
        e *= np.exp(-np.maximum(0, t - attack) / decay_tau)
    if release:
        r = int(SR * release)
        e[-r:] *= np.linspace(1, 0, r) ** 2
    return e


def fade_edges(x, ms=3):
    """앞뒤를 짧게 페이드해 시작/끝 틱 소리를 없앤다."""
    n = int(SR * ms / 1000)
    x = x.copy()
    x[:n] *= np.linspace(0, 1, n)
    x[-n:] *= np.linspace(1, 0, n)
    return x


def pad(x, seconds):
    return np.concatenate([x, np.zeros(int(SR * seconds))])


def mix_at(base, x, start):
    i = int(SR * start)
    need = i + len(x)
    if need > len(base):
        base = np.concatenate([base, np.zeros(need - len(base))])
    base[i:need] += x
    return base


def bandpass_noise(seconds, f0, f1, q=4.0, seed=1):
    """중심 주파수가 f0 → f1 로 움직이는 대역 통과 잡음 (상태 변수 필터)."""
    rng = np.random.default_rng(seed)
    n = int(SR * seconds)
    noise = rng.uniform(-1, 1, n)
    centers = f0 * (f1 / f0) ** (np.arange(n) / n)
    low = band = 0.0
    out = np.empty(n)
    damp = 1.0 / q
    for i in range(n):
        f = 2 * np.sin(np.pi * centers[i] / SR)
        high = noise[i] - low - damp * band
        band += f * high
        low += f * band
        out[i] = band
    return out


# ---------------------------------------------------------------- 소리 정의

def sfx_button():
    """버튼 터치: 짧고 맑은 "틱" 블립 (60ms)."""
    s, _ = sweep(2100, 1700, 0.06, 0.5)
    h, _ = sweep(4200, 3400, 0.06, 0.5)
    x = (s + 0.18 * h) * env(len(s), 0.002, decay_tau=0.014)
    return pad(x, 0.02)


def sfx_select():
    """정보검색 메뉴 선택: 위로 오르는 두 음 차임 (C6 → G6)."""
    out = np.zeros(0)
    for start, f in ((0.0, 1046.5), (0.07, 1568.0)):
        t = t_axis(0.22)
        wobble = 1 + 0.004 * np.sin(2 * np.pi * 7 * t)  # 살짝 흔들리는 파동
        phase = 2 * np.pi * np.cumsum(f * wobble) / SR
        tone = np.sin(phase) + 0.25 * np.sin(2 * phase) + 0.08 * np.sin(3 * phase)
        out = mix_at(out, tone * env(len(t), 0.004, decay_tau=0.06), start)
    return pad(out, 0.03)


def sfx_pickup():
    """레일에서 카드 꺼내기: 위로 차오르는 전파 스윕 + 반짝이는 배음 (240ms)."""
    dur = 0.24
    s, freq = sweep(520, 1480, dur, 0.7)
    t = t_axis(dur)
    shimmer = 0.5 + 0.5 * np.sin(2 * np.pi * 22 * t)  # 전파가 떨리는 듯한 반짝임
    phase2 = 2 * np.pi * np.cumsum(freq * 2.0) / SR
    x = s + 0.22 * np.sin(phase2) * shimmer
    x *= env(len(s), 0.025, release=0.12)
    return pad(x, 0.03)


def sfx_close():
    """X로 카드 돌려보내기: 아래로 내려가며 사라지는 스윕 (180ms)."""
    dur = 0.18
    s, freq = sweep(1250, 480, dur, 0.6)
    phase2 = 2 * np.pi * np.cumsum(freq * 1.5) / SR
    x = s + 0.15 * np.sin(phase2)
    x *= env(len(s), 0.004, decay_tau=0.07)
    return pad(x, 0.03)


def sfx_flip():
    """카드 뒤집기: 부드러운 바람 소리(대역 잡음 스윕) + 자리 잡는 작은 블립."""
    dur = 0.2
    whoosh = bandpass_noise(dur, 700, 2600, q=3.0)
    whoosh /= np.max(np.abs(whoosh)) + 1e-9
    t = t_axis(dur)
    whoosh *= np.sin(np.pi * t / dur) ** 2  # 가운데가 가장 크게
    blip, _ = sweep(1500, 1900, 0.06, 0.5)
    blip *= env(len(blip), 0.002, decay_tau=0.02)
    out = mix_at(0.6 * whoosh, 0.7 * blip, dur - 0.04)
    return pad(out, 0.03)


SOUNDS = {
    "sfx_button": sfx_button,
    "sfx_select": sfx_select,
    "sfx_pickup": sfx_pickup,
    "sfx_close": sfx_close,
    "sfx_flip": sfx_flip,
}


# ---------------------------------------------------------------- 정리 / 저장 / 확인

def active_rms_db(x):
    """소리가 나는 구간(피크 대비 -40dB 이상)의 RMS."""
    a = np.abs(x)
    mask = a > a.max() * 0.01
    rms = np.sqrt(np.mean(x[mask] ** 2))
    return 20 * np.log10(rms + 1e-12)


def normalize(x):
    x = x - np.mean(x)
    x = x * 10 ** ((TARGET_RMS_DB - active_rms_db(x)) / 20)
    peak = np.max(np.abs(x))
    if peak > PEAK_LIMIT:
        x *= PEAK_LIMIT / peak
    return fade_edges(x)


def write_wav(path, x):
    data = np.clip(np.round(x * 32767), -32768, 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    return data


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    print(f"{'file':<16}{'len(ms)':>8}{'peak(dBFS)':>12}{'rms(dB)':>9}{'clip':>6}{'first':>7}{'last':>6}")
    for name, make in SOUNDS.items():
        x = normalize(make())
        data = write_wav(os.path.join(OUT_DIR, name + ".wav"), x)
        peak = np.max(np.abs(data)) / 32768
        clipped = int(np.sum(np.abs(data) >= 32767))
        print(f"{name:<16}{len(data) * 1000 // SR:>8}{20 * np.log10(peak):>12.2f}"
              f"{active_rms_db(data / 32768):>9.2f}{clipped:>6}{int(data[0]):>7}{int(data[-1]):>6}")


if __name__ == "__main__":
    main()
