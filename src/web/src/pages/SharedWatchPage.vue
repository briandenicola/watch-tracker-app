<template>
  <div class="min-h-screen bg-bg text-text">
    <div class="max-w-3xl mx-auto px-4 py-8 sm:py-12">
      <div v-if="loading" class="flex justify-center py-24">
        <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
      </div>

      <div v-else-if="!watch" class="text-center py-24">
        <p class="font-display text-2xl font-semibold text-text mb-2">This link isn't available</p>
        <p class="text-sm text-text-muted">
          It may have been revoked by its owner, or the address may be incomplete.
        </p>
      </div>

      <template v-else>
        <header class="mb-6">
          <p class="text-xs uppercase tracking-[0.24em] text-accent mb-2">
            {{ watch.isWishList ? 'Wish List' : 'Shared Watch' }}
          </p>
          <h1 class="font-display text-3xl sm:text-4xl font-semibold leading-tight">
            {{ watch.brand }} {{ watch.model }}
          </h1>
        </header>

        <div v-if="watch.imageUrls.length" class="relative rounded-xl overflow-hidden bg-bg-surface mb-6">
          <div class="h-[300px] lg:h-[420px] flex items-center justify-center">
            <img
              :src="imageUrl(watch.imageUrls[imageIndex].url)"
              :alt="`${watch.brand} ${watch.model}`"
              class="max-w-full max-h-full object-contain"
            />
          </div>
          <div v-if="watch.imageUrls.length > 1" class="absolute bottom-3 inset-x-0 flex justify-center gap-1.5">
            <button
              v-for="(_, i) in watch.imageUrls"
              :key="i"
              class="w-2 h-2 rounded-full transition-colors"
              :class="i === imageIndex ? 'bg-accent' : 'bg-white/40'"
              :aria-label="`Show image ${i + 1}`"
              @click="imageIndex = i"
            />
          </div>
        </div>

        <section v-for="section in sections" :key="section.heading" class="detail-card mb-5">
          <h2 class="detail-heading">{{ section.heading }}</h2>
          <dl class="detail-list">
            <div v-for="row in section.rows" :key="row.label" class="detail-row">
              <dt class="detail-label">{{ row.label }}</dt>
              <dd class="detail-value">
                <a v-if="row.href" :href="row.href" target="_blank" rel="noopener noreferrer" class="detail-link">
                  {{ row.value }}
                </a>
                <template v-else>{{ row.value }}</template>
              </dd>
            </div>
          </dl>
        </section>

        <footer class="mt-8 pt-5 border-t border-border text-center space-y-1">
          <p class="text-xs text-text-muted">
            Shared from a private WatchTracker collection on {{ formatDate(watch.sharedAt) }}. This page is read-only,
            and shows only what the owner chose to publish.
          </p>
          <RouterLink to="/" class="text-xs text-accent hover:underline">Open WatchTracker</RouterLink>
        </footer>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import type { SharedWatch } from '@/types'
import { getSharedWatch } from '@/services/sharing'
import { imageUrl } from '@/services/watches'

const route = useRoute()

const watch = ref<SharedWatch | null>(null)
const loading = ref(true)
const imageIndex = ref(0)

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

// "Anyone with the link" is not the same as "anyone at all" — a shared watch
// should not turn up in a search for its owner. The tag is added per visit and
// removed on the way out, so it never leaks onto the rest of the app.
const previousTitle = document.title
let robotsTag: HTMLMetaElement | null = null

function blockIndexing() {
  const existing = document.querySelector<HTMLMetaElement>('meta[name="robots"]')
  if (existing) return
  robotsTag = document.createElement('meta')
  robotsTag.name = 'robots'
  robotsTag.content = 'noindex, nofollow'
  document.head.appendChild(robotsTag)
}

onMounted(async () => {
  blockIndexing()
  try {
    watch.value = await getSharedWatch(String(route.params.token))
    if (watch.value) document.title = `${watch.value.brand} ${watch.value.model}`
  } catch {
    // Revoked, mistyped, or never existed — all the same to a visitor.
    watch.value = null
  } finally {
    loading.value = false
  }
})

onBeforeUnmount(() => {
  robotsTag?.remove()
  robotsTag = null
  document.title = previousTitle
})

interface Row { label: string, value: string, href?: string }

const sections = computed<{ heading: string, rows: Row[] }[]>(() => {
  const w = watch.value
  if (!w) return []

  const mm = (value?: number | null) => (value ? `${value} mm` : undefined)
  const text = (value?: string | null) => value || undefined

  const raw: { heading: string, rows: (Row | undefined)[] }[] = [
    {
      heading: 'Identification',
      rows: [
        row('Brand', w.brand),
        row('Model', w.model),
        row('SKU / Reference', text(w.sku)),
        row('Production Year', w.productionYear?.toString()),
        row('Origin', text(w.countryOfOrigin)),
      ],
    },
    {
      heading: 'Case & Band',
      rows: [
        row('Case Size', mm(w.caseSizeMm)),
        row('Lug Width', mm(w.lugWidthMm)),
        row('Case Shape', text(w.caseShape)),
        row('Crystal', text(w.crystalType)),
        row('Bezel', text(w.bezelType)),
        row('Crown', text(w.crownType)),
        row('Dial', text(w.dialColor)),
        row('Water Resistance', text(w.waterResistance)),
        row('Band Type', text(w.bandType)),
        row('Band Color', text(w.bandColor)),
      ],
    },
    {
      heading: 'Movement',
      rows: [
        row('Movement Type', w.movementType),
        row('Power Reserve', w.powerReserveHours ? `${w.powerReserveHours} hours` : undefined),
        row('Calendar', text(w.calendarType)),
        row('Battery Type', text(w.batteryType)),
      ],
    },
    {
      heading: 'More',
      rows: [
        w.linkUrl ? { label: 'Product / Reference', value: w.linkText || 'Product Link', href: w.linkUrl } : undefined,
      ],
    },
  ]

  return raw
    .map(section => ({ heading: section.heading, rows: section.rows.filter((r): r is Row => Boolean(r)) }))
    .filter(section => section.rows.length > 0)
})

function row(label: string, value?: string): Row | undefined {
  return value ? { label, value } : undefined
}
</script>

<style scoped>
.detail-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  padding: 1.25rem;
}

.detail-heading {
  color: var(--color-accent);
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.22em;
  margin-bottom: 0.85rem;
  text-transform: uppercase;
}

.detail-list {
  display: grid;
  gap: 0;
}

.detail-row {
  display: grid;
  grid-template-columns: minmax(8rem, 0.7fr) minmax(0, 1fr);
  gap: 1rem;
  padding: 0.85rem 0;
  border-bottom: 1px solid var(--color-border);
}

.detail-row:last-child {
  border-bottom: 0;
}

.detail-label {
  color: var(--color-text-secondary);
  font-size: 0.95rem;
}

.detail-value {
  color: var(--color-text);
  font-size: 1rem;
  min-width: 0;
  overflow-wrap: anywhere;
}

.detail-link {
  color: var(--color-accent);
}

.detail-link:hover {
  text-decoration: underline;
}

@media (min-width: 768px) {
  .detail-card {
    padding: 1.5rem 1.75rem;
  }
}
</style>
