# Renderiza los SVG del estilo "tarot nocturno" a PNG transparentes con Chromium (Playwright).
import os, sys
from playwright.sync_api import sync_playwright

OUT = sys.argv[1]
GOLD, GOLD_LINE, INK, NIGHT = "#F2CF7A", "#C9A55A", "#121839", "#0B0F24"

SOL = '<circle cx="50" cy="50" r="16"/><circle cx="50" cy="50" r="9" stroke-width="{thin}"/><path d="M72 50H86M61 69.05L68 81.18M39 69.05L32 81.18M28 50H14M39 30.95L32 18.82M61 30.95L68 18.82M69.05 61L75.11 64.5M50 72V79M30.95 61L24.89 64.5M30.95 39L24.89 35.5M50 28V21M69.05 39L75.11 35.5"/>'
LUNA = '<path d="M47.88 26.1A24 24 0 1 0 72.86 57.32A20 20 0 1 1 47.88 26.1Z"/><path d="M74 20L75.5 24.5L80 26L75.5 27.5L74 32L72.5 27.5L68 26L72.5 24.5Z" fill="{c}" stroke="none"/><path d="M83 37L83.75 39.25L86 40L83.75 40.75L83 43L82.25 40.75L80 40L82.25 39.25Z" fill="{c}" stroke="none"/>'
OJO = '<path d="M16 50Q50 20 84 50Q50 80 16 50Z"/><circle cx="50" cy="50" r="12"/><circle cx="50" cy="50" r="4.5" fill="{c}" stroke="none"/><path d="M50 32V22M37 35L32 27M63 35L68 27M26 41L19 35M74 41L81 35"/>'
# Versiones simplificadas para iconos pequeños (historial).
SOL_S = '<circle cx="50" cy="50" r="16"/><path d="M76 50H88M24 50H12M50 76V88M50 24V12M68.4 68.4L76.9 76.9M31.6 68.4L23.1 76.9M31.6 31.6L23.1 23.1M68.4 31.6L76.9 23.1"/>'
LUNA_S = '<path d="M47.88 26.1A24 24 0 1 0 72.86 57.32A20 20 0 1 1 47.88 26.1Z"/>'
OJO_S = '<path d="M10 50Q50 16 90 50Q50 84 10 50Z"/><circle cx="50" cy="50" r="12"/>'
STAR4 = '<path d="M50 4L60 40L96 50L60 60L50 96L40 60L4 50L40 40Z"/>'

def symbol(body, color, sw, thin=1.2):
    return f'<g fill="none" stroke="{color}" stroke-width="{sw}" stroke-linecap="round" stroke-linejoin="round">{body.format(c=color, thin=thin)}</g>'

def card(body):
    # 560x800: marco doble, estrella arriba, símbolo grande. El nombre lo escribe Unity (abajo dentro de la carta).
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="560" height="800" viewBox="0 0 560 800">
<rect x="4" y="4" width="552" height="792" rx="40" fill="{INK}" stroke="{GOLD_LINE}" stroke-width="6"/>
<rect x="30" y="30" width="500" height="740" rx="22" fill="none" stroke="{GOLD_LINE}" stroke-opacity="0.5" stroke-width="3"/>
<g transform="translate(262 62) scale(0.36)" fill="{GOLD_LINE}">{STAR4}</g>
<g fill="none" stroke="{GOLD_LINE}" stroke-opacity="0.35" stroke-width="2"><path d="M60 120H500M60 640H500"/></g>
<g transform="translate(40 160) scale(4.8)">{symbol(body, GOLD, 2.2)}</g>
</svg>'''

def glow():
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="600" height="800" viewBox="0 0 600 800">
<defs><radialGradient id="g" cx="50%" cy="50%" r="50%"><stop offset="0%" stop-color="#F2CF7A" stop-opacity="0.30"/><stop offset="55%" stop-color="#F2CF7A" stop-opacity="0.10"/><stop offset="100%" stop-color="#F2CF7A" stop-opacity="0"/></radialGradient></defs>
<ellipse cx="300" cy="400" rx="300" ry="400" fill="url(#g)"/></svg>'''

def coin():
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="160" height="160" viewBox="0 0 100 100">
<circle cx="50" cy="50" r="44" fill="#C99A45" stroke="{GOLD}" stroke-width="5"/><circle cx="50" cy="50" r="32" fill="none" stroke="#8C6A2A" stroke-width="3"/>
<path d="M50 32L54.5 45.5L68 50L54.5 54.5L50 68L45.5 54.5L32 50L45.5 45.5Z" fill="#F7DE9C"/></svg>'''

def rounded(size, r, fill="#FFFFFF", stroke=None, sw=0, fill_opacity=1):
    s = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ''
    o = sw / 2
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 {size} {size}"><rect x="{o}" y="{o}" width="{size-sw}" height="{size-sw}" rx="{r}" fill="{fill}" fill-opacity="{fill_opacity}"{s}/></svg>'

def icon(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 100 100">{symbol(body, "#FFFFFF", 8)}</svg>'

def astrolabe():
    dots = ''.join(f'<circle cx="{512+350*c:.1f}" cy="{512+350*s:.1f}" r="4" fill="{GOLD_LINE}" fill-opacity="0.45"/>'
                   for c, s in [(1,0),(.866,.5),(.5,.866),(0,1),(-.5,.866),(-.866,.5),(-1,0),(-.866,-.5),(-.5,-.866),(0,-1),(.5,-.866),(.866,-.5)])
    stars = ''.join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#EDE6D3" fill-opacity="0.5"/>' for x, y, r in
                    [(90,140,2.5),(930,110,3),(860,880,2.5),(140,900,2),(60,520,2),(980,600,2.2),(300,40,1.8),(760,990,2)])
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 1024 1024">
<g fill="none" stroke="{GOLD_LINE}"><circle cx="512" cy="512" r="300" stroke-opacity="0.22" stroke-width="2.5"/>
<circle cx="512" cy="512" r="350" stroke-opacity="0.16" stroke-width="2"/><circle cx="512" cy="512" r="420" stroke-opacity="0.10" stroke-width="2" stroke-dasharray="4 16"/></g>
{dots}{stars}<path d="M860 880L930 820L980 860" fill="none" stroke="#EDE6D3" stroke-opacity="0.18" stroke-width="2"/></svg>'''

def motif(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 100 100">{symbol(body, GOLD_LINE, 3)}</svg>'

def diamond():
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 100 100">
<path d="M50 6L94 50L50 94L6 50Z" fill="#C2384F" stroke="#FF9AA9" stroke-width="4" stroke-linejoin="round"/>
<path d="M50 24L76 50L50 76L24 50Z" fill="none" stroke="#FF9AA9" stroke-opacity="0.5" stroke-width="2"/></svg>'''

# Pregunta misteriosa: carta pequeña con "?" y anillo que marca el tiempo de la notificación.
QMARK = '<path d="M36 34Q36 18 50 18Q64 18 64 32Q64 42 54 47Q50 49 50 56V61"/><circle cx="50" cy="75" r="3.5" fill="{c}" stroke="none"/>'

def question_card():
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="280" height="400" viewBox="0 0 280 400">
<rect x="4" y="4" width="272" height="392" rx="26" fill="{INK}" stroke="{GOLD_LINE}" stroke-width="5"/>
<rect x="20" y="20" width="240" height="360" rx="14" fill="none" stroke="{GOLD_LINE}" stroke-opacity="0.5" stroke-width="2.5"/>
<g transform="translate(126 34) scale(0.28)" fill="{GOLD_LINE}">{STAR4}</g>
<g transform="translate(126 338) scale(0.28)" fill="{GOLD_LINE}">{STAR4}</g>
<g transform="translate(20 80) scale(2.4)">{symbol(QMARK, GOLD, 6)}</g>
</svg>'''

def ring():
    return '<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256"><circle cx="128" cy="128" r="118" fill="none" stroke="#FFFFFF" stroke-width="12"/></svg>'

ASSETS = {
    "Cards/card_sol.png": card(SOL), "Cards/card_luna.png": card(LUNA), "Cards/card_ojo.png": card(OJO),
    "Cards/card_glow.png": glow(),
    "Icons/coin.png": coin(),
    "Cards/question_card.png": question_card(), "Icons/ring.png": ring(),
    "Icons/sym_sol.png": icon(SOL_S), "Icons/sym_luna.png": icon(LUNA_S), "Icons/sym_ojo.png": icon(OJO_S),
    "UI/panel_9slice.png": rounded(96, 24, fill=INK, stroke="#C9A55A", sw=3),
    "UI/button_9slice.png": rounded(96, 24),
    "UI/outline_9slice.png": rounded(96, 24, fill="none", stroke="#FFFFFF", sw=4),
    "UI/minicard_9slice.png": rounded(64, 12),
    "Background/astrolabe.png": astrolabe(),
    "Background/motif_eye.png": motif(OJO_S), "Background/motif_moon.png": motif(LUNA_S),
    "Shield/projectile.png": diamond(),
}

with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page()
    for rel, svg in ASSETS.items():
        path = os.path.join(OUT, rel)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        page.set_content(f'<html><body style="margin:0;background:transparent">{svg}</body></html>')
        el = page.query_selector("svg")
        el.screenshot(path=path, omit_background=True)
        print(rel)
    browser.close()
