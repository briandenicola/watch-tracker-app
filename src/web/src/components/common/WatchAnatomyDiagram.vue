<template>
  <section aria-labelledby="watch-anatomy-title">
    <h2 id="watch-anatomy-title" class="font-display text-xl font-semibold text-text">Anatomy of a Watch</h2>
    <p class="mt-1 text-sm leading-6 text-text-muted">
      The main parts of a typical wristwatch. Hover or tap a part in the list to highlight it on the diagram.
    </p>
    <div class="mt-3 grid gap-6 rounded-xl border border-border bg-bg-card p-4 sm:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] sm:items-center">
      <svg
        viewBox="0 0 400 400"
        class="mx-auto w-full max-w-sm"
        role="img"
        aria-labelledby="watch-anatomy-svg-title"
        data-testid="watch-anatomy-svg"
      >
        <title id="watch-anatomy-svg-title">Front view of a wristwatch with each part shaded in its own color</title>

        <g :style="partStyle('band')">
          <rect x="172" y="0" width="56" height="92" rx="4" :fill="colors.band" />
          <rect x="172" y="308" width="56" height="92" rx="4" :fill="colors.band" />
          <g :stroke="shade" stroke-width="1.5" opacity="0.35">
            <line x1="172" y1="30" x2="228" y2="30" />
            <line x1="172" y1="60" x2="228" y2="60" />
            <line x1="172" y1="340" x2="228" y2="340" />
            <line x1="172" y1="370" x2="228" y2="370" />
          </g>
        </g>

        <g :style="partStyle('lugs')" :fill="colors.lugs">
          <rect x="156" y="78" width="16" height="50" rx="5" />
          <rect x="228" y="78" width="16" height="50" rx="5" />
          <rect x="156" y="272" width="16" height="50" rx="5" />
          <rect x="228" y="272" width="16" height="50" rx="5" />
        </g>

        <g :style="partStyle('crown')" :fill="colors.crown">
          <rect x="290" y="187" width="20" height="26" rx="4" />
          <g :stroke="shade" stroke-width="1.5" opacity="0.4">
            <line x1="296" y1="189" x2="296" y2="211" />
            <line x1="300" y1="189" x2="300" y2="211" />
            <line x1="304" y1="189" x2="304" y2="211" />
          </g>
        </g>

        <circle :style="partStyle('case')" cx="200" cy="200" r="90" fill="none" :stroke="colors.case" stroke-width="8" />

        <g :style="partStyle('bezel')">
          <circle cx="200" cy="200" r="79" fill="none" :stroke="colors.bezel" stroke-width="14" />
          <g :stroke="shade" stroke-linecap="round" opacity="0.55">
            <line
              v-for="tick in bezelTicks"
              :key="tick.key"
              :x1="tick.x1" :y1="tick.y1" :x2="tick.x2" :y2="tick.y2"
              :stroke-width="tick.major ? 3 : 1.5"
            />
          </g>
          <polygon points="200,118 194,128 206,128" fill="#f5f0e8" />
        </g>

        <circle :style="partStyle('crystal')" cx="200" cy="200" r="70" fill="none" :stroke="colors.crystal" stroke-width="5" />
        <g :style="partStyle('dial')">
          <circle cx="200" cy="200" r="67.5" :fill="colors.dial" />
          <path d="M 150 175 A 55 55 0 0 1 185 145" fill="none" stroke="#ffffff" stroke-width="4" stroke-linecap="round" opacity="0.3" />
        </g>

        <g :style="partStyle('markers')" :fill="colors.markers">
          <rect
            v-for="marker in hourMarkers"
            :key="marker.hour"
            :x="196" :y="138" :width="8" :height="marker.hour === 12 ? 16 : 12" rx="1.5"
            :transform="`rotate(${marker.hour * 30} 200 200)`"
          />
        </g>

        <g :style="partStyle('date')">
          <rect x="238" y="192" width="20" height="16" rx="2" fill="#ffffff" :stroke="colors.date" stroke-width="3" />
          <text x="248" y="204" text-anchor="middle" font-size="10" font-weight="700" fill="#1a1714">14</text>
        </g>

        <g :style="partStyle('hands')" :fill="colors.hands" :stroke="colors.hands" stroke-linecap="round">
          <line x1="200" y1="200" x2="167" y2="181" stroke-width="7" />
          <line x1="200" y1="200" x2="243" y2="175" stroke-width="5" />
          <line x1="212" y1="221" x2="173" y2="153" stroke-width="2" />
          <circle cx="200" cy="200" r="5" stroke="none" />
        </g>

        <g v-for="callout in callouts" :key="callout.key" :style="partStyle(callout.key)">
          <polyline :points="callout.line" fill="none" :stroke="colors[callout.key]" stroke-width="1.5" />
          <circle :cx="callout.tx" :cy="callout.ty" r="3" :fill="colors[callout.key]" :stroke="shade" stroke-width="1" />
          <circle :cx="callout.bx" :cy="callout.by" r="12" :fill="colors[callout.key]" />
          <text :x="callout.bx" :y="callout.by + 4.5" text-anchor="middle" font-size="13" font-weight="700" fill="#1a1714">
            {{ partNumber(callout.key) }}
          </text>
        </g>
      </svg>

      <ol class="space-y-1">
        <li v-for="(part, index) in watchAnatomyParts" :key="part.key">
          <button
            type="button"
            class="flex w-full items-start gap-3 rounded-lg px-2 py-2 text-left transition-colors hover:bg-bg-elevated focus:bg-bg-elevated focus:outline-none"
            :aria-pressed="active === part.key"
            :data-part="part.key"
            @mouseenter="active = part.key"
            @mouseleave="active = null"
            @focus="active = part.key"
            @blur="active = null"
            @click="active = active === part.key ? null : part.key"
          >
            <span
              class="mt-0.5 inline-flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-full text-xs font-bold text-[#1a1714]"
              :style="{ backgroundColor: part.color }"
              aria-hidden="true"
            >{{ index + 1 }}</span>
            <span>
              <span class="block text-sm font-semibold text-text">{{ part.name }}</span>
              <span class="block text-xs leading-5 text-text-secondary">{{ part.description }}</span>
            </span>
          </button>
        </li>
      </ol>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { watchAnatomyParts, type WatchAnatomyPartKey } from '@/constants/watchHelp'

const shade = '#1a1714'
const active = ref<WatchAnatomyPartKey | null>(null)

const colors = Object.fromEntries(watchAnatomyParts.map(part => [part.key, part.color])) as Record<WatchAnatomyPartKey, string>

function partNumber(key: WatchAnatomyPartKey) {
  return watchAnatomyParts.findIndex(part => part.key === key) + 1
}

function partStyle(key: WatchAnatomyPartKey) {
  return { opacity: active.value && active.value !== key ? 0.2 : 1, transition: 'opacity 150ms ease' }
}

function polar(radius: number, degrees: number) {
  const radians = (degrees - 90) * Math.PI / 180
  return { x: 200 + radius * Math.cos(radians), y: 200 + radius * Math.sin(radians) }
}

const bezelTicks = Array.from({ length: 60 }, (_, minute) => minute)
  .filter(minute => minute !== 0)
  .filter(minute => minute % 5 === 0 || minute < 15)
  .map(minute => {
    const major = minute % 5 === 0
    const outer = polar(83, minute * 6)
    const inner = polar(major ? 75 : 79, minute * 6)
    return { key: minute, major, x1: inner.x, y1: inner.y, x2: outer.x, y2: outer.y }
  })

// The 3 o'clock marker is replaced by the date window.
const hourMarkers = Array.from({ length: 12 }, (_, i) => ({ hour: i + 1 })).filter(marker => marker.hour !== 3)

interface Callout { key: WatchAnatomyPartKey; tx: number; ty: number; bx: number; by: number; line: string }

function callout(key: WatchAnatomyPartKey, tx: number, ty: number, bx: number, by: number): Callout {
  const elbowX = bx < 200 ? bx + 30 : bx - 30
  return { key, tx, ty, bx, by, line: `${bx},${by} ${elbowX},${by} ${tx},${ty}` }
}

const callouts: Callout[] = [
  callout('band', 184, 40, 30, 40),
  callout('lugs', 164, 100, 30, 100),
  callout('bezel', 144, 144, 30, 150),
  callout('crystal', 130, 200, 30, 200),
  callout('dial', 172, 228, 30, 250),
  callout('case', 136, 264, 30, 300),
  callout('markers', 229, 150, 370, 100),
  callout('hands', 226, 185, 370, 150),
  callout('crown', 310, 200, 370, 200),
  callout('date', 250, 208, 370, 250),
]
</script>
