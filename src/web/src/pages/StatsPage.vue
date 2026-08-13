<template>
  <div>
    <PullToRefresh :pulling="pulling" :refreshing="refreshing" :pull-distance="pullDistance" />
    <h2 class="font-display text-2xl font-semibold text-text mb-6">Statistics</h2>

    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
    </div>

    <div v-else-if="error" class="text-center py-20">
      <p class="text-danger mb-2">Failed to load statistics</p>
      <button @click="load" class="text-accent text-sm hover:underline">Retry</button>
    </div>

    <div v-else class="space-y-8">
      <!-- Summary Cards -->
      <div class="grid grid-cols-2 lg:grid-cols-3 gap-4">
        <div class="bg-bg-card border border-border rounded-xl p-4 text-center">
          <p class="text-3xl font-display font-semibold text-accent">{{ watches.length }}</p>
          <p class="text-sm text-text-secondary mt-1">Total Watches</p>
        </div>
        <div class="bg-bg-card border border-border rounded-xl p-4 text-center">
          <p class="text-3xl font-display font-semibold text-accent">{{ totalWears }}</p>
          <p class="text-sm text-text-secondary mt-1">Total Wears</p>
        </div>
        <div class="bg-bg-card border border-border rounded-xl p-4 text-center col-span-2 lg:col-span-1">
          <p class="text-3xl font-display font-semibold text-accent">{{ avgWears }}</p>
          <p class="text-sm text-text-secondary mt-1">Avg Wears / Watch</p>
        </div>
        <div class="bg-bg-card border border-border rounded-xl p-4 text-center">
          <p class="text-3xl font-display font-semibold text-accent">{{ formatCurrency(totalCollectionValue) }}</p>
          <p class="text-sm text-text-secondary mt-1">Collection Value</p>
        </div>
        <div class="bg-bg-card border border-border rounded-xl p-4 text-center">
          <p class="text-3xl font-display font-semibold text-accent">{{ formatCurrency(medianValue) }}</p>
          <p class="text-sm text-text-secondary mt-1">Median Value</p>
        </div>
        <div class="bg-bg-card border border-border rounded-xl p-4 text-center">
          <p class="text-3xl font-display font-semibold text-accent">{{ formatCurrency(avgCostPerWear) }}</p>
          <p class="text-sm text-text-secondary mt-1">Avg Cost / Wear</p>
        </div>
      </div>

      <!-- Collection Value Over Time -->
      <div class="bg-bg-card border border-border rounded-xl p-4">
        <div class="flex items-start justify-between gap-4 mb-4">
          <div>
            <h3 class="text-lg font-medium text-text">Collection Value Over Time</h3>
            <p class="text-xs text-text-muted mt-1">Based on purchase price by acquisition date</p>
          </div>
          <span class="text-sm font-medium text-accent flex-shrink-0">{{ formatCurrency(totalCollectionValue) }}</span>
        </div>
        <div v-if="valueTimeline.length === 0" class="text-sm text-text-muted">Add purchase prices to see value trends</div>
        <div v-else class="space-y-3">
          <div class="h-48 rounded-lg bg-bg-surface border border-border p-3">
            <svg viewBox="0 0 100 100" preserveAspectRatio="none" class="w-full h-full overflow-visible">
              <polyline
                :points="valueTimelinePoints"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                vector-effect="non-scaling-stroke"
                class="text-accent"
              />
              <circle
                v-for="point in valueTimelinePlot"
                :key="`${point.date}-${point.total}`"
                :cx="point.x"
                :cy="point.y"
                r="1.6"
                vector-effect="non-scaling-stroke"
                class="fill-accent"
              />
            </svg>
          </div>
          <div class="flex justify-between text-xs text-text-muted">
            <span>{{ formatDate(valueTimeline[0].date) }}</span>
            <span>{{ formatDate(valueTimeline[valueTimeline.length - 1].date) }}</span>
          </div>
        </div>
      </div>

      <div class="grid lg:grid-cols-2 gap-4">
        <!-- Top Value Watches -->
        <div class="bg-bg-card border border-border rounded-xl p-4">
          <h3 class="text-lg font-medium text-text mb-4">Top 5 Most Valuable</h3>
          <div v-if="topValuable.length === 0" class="text-sm text-text-muted">Add purchase prices to rank watches</div>
          <div v-else class="space-y-3">
            <RouterLink
              v-for="(w, i) in topValuable"
              :key="w.id"
              :to="`/watches/${w.id}`"
              class="flex items-center gap-3 group"
            >
              <span class="text-sm font-medium text-accent w-5 text-right">{{ i + 1 }}.</span>
              <div class="w-10 h-10 rounded-lg bg-bg-surface overflow-hidden flex-shrink-0">
                <img v-if="w.imageUrls.length" :src="imageUrl(w.imageUrls[0].url)" class="w-full h-full object-contain" />
                <span v-else class="flex items-center justify-center w-full h-full text-text-muted text-lg">⌚</span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm text-text truncate group-hover:text-accent transition-colors">{{ w.brand }} {{ w.model }}</p>
                <p class="text-xs text-text-muted">{{ valueShare(w.purchasePrice) }}% of collection value</p>
              </div>
              <span class="text-sm text-text-secondary">{{ formatCurrency(w.purchasePrice) }}</span>
            </RouterLink>
          </div>
        </div>

        <!-- Value By Brand -->
        <div class="bg-bg-card border border-border rounded-xl p-4">
          <h3 class="text-lg font-medium text-text mb-4">Value by Brand</h3>
          <div v-if="brandValueBreakdown.length === 0" class="text-sm text-text-muted">Add purchase prices to compare brands</div>
          <div v-else class="space-y-3">
            <div v-for="item in brandValueBreakdown" :key="item.brand" class="space-y-1.5">
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm text-text truncate">{{ item.brand }}</span>
                <span class="text-xs text-text-muted flex-shrink-0">
                  {{ formatCurrency(item.value) }} · {{ item.count }} watch{{ item.count > 1 ? 'es' : '' }}
                </span>
              </div>
              <div class="h-2 bg-bg-surface rounded-full overflow-hidden">
                <div class="h-full bg-accent/60 rounded-full transition-all" :style="{ width: item.pct + '%' }" />
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="grid lg:grid-cols-2 gap-4">
        <!-- Top 10 Best Resale Values -->
        <div class="bg-bg-card border border-border rounded-xl p-4">
          <h3 class="text-lg font-medium text-text mb-4">Top 10 Best Resale Values</h3>
          <div v-if="topResaleValue.length === 0" class="text-sm text-text-muted">Refresh a resale estimate to rank watches</div>
          <div v-else class="space-y-3">
            <RouterLink
              v-for="(w, i) in topResaleValue"
              :key="w.id"
              :to="`/watches/${w.id}`"
              class="flex items-center gap-3 group"
            >
              <span class="text-sm font-medium text-accent w-5 text-right">{{ i + 1 }}.</span>
              <div class="w-10 h-10 rounded-lg bg-bg-surface overflow-hidden flex-shrink-0">
                <img v-if="w.imageUrls.length" :src="imageUrl(w.imageUrls[0].url)" class="w-full h-full object-contain" />
                <span v-else class="flex items-center justify-center w-full h-full text-text-muted text-lg">⌚</span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm text-text truncate group-hover:text-accent transition-colors">{{ w.brand }} {{ w.model }}</p>
              </div>
              <span class="text-sm text-text-secondary">{{ formatCurrency(w.currentResaleValue) }}</span>
            </RouterLink>
          </div>
        </div>

        <!-- Top 10 Resale Gain -->
        <div class="bg-bg-card border border-border rounded-xl p-4">
          <h3 class="text-lg font-medium text-text mb-4">Top 10 Resale Gain</h3>
          <div v-if="topResaleGain.length === 0" class="text-sm text-text-muted">Add purchase prices and resale estimates to rank gains</div>
          <div v-else class="space-y-3">
            <RouterLink
              v-for="(item, i) in topResaleGain"
              :key="item.watch.id"
              :to="`/watches/${item.watch.id}`"
              class="flex items-center gap-3 group"
            >
              <span class="text-sm font-medium text-accent w-5 text-right">{{ i + 1 }}.</span>
              <div class="w-10 h-10 rounded-lg bg-bg-surface overflow-hidden flex-shrink-0">
                <img v-if="item.watch.imageUrls.length" :src="imageUrl(item.watch.imageUrls[0].url)" class="w-full h-full object-contain" />
                <span v-else class="flex items-center justify-center w-full h-full text-text-muted text-lg">⌚</span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm text-text truncate group-hover:text-accent transition-colors">{{ item.watch.brand }} {{ item.watch.model }}</p>
              </div>
              <span class="text-sm" :class="item.gain >= 0 ? 'text-success' : 'text-danger'">
                {{ item.gain >= 0 ? '+' : '' }}{{ formatCurrency(item.gain) }}
              </span>
            </RouterLink>
          </div>
        </div>
      </div>

      <!-- Cost Per Wear -->
      <div class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Best Cost per Wear</h3>
        <div v-if="costPerWearLeaders.length === 0" class="text-sm text-text-muted">Record wears on priced watches to calculate cost per wear</div>
        <div v-else class="space-y-3">
          <RouterLink
            v-for="w in costPerWearLeaders"
            :key="w.id"
            :to="`/watches/${w.id}`"
            class="flex items-center gap-3 group"
          >
            <div class="w-10 h-10 rounded-lg bg-bg-surface overflow-hidden flex-shrink-0">
              <img v-if="w.imageUrls.length" :src="imageUrl(w.imageUrls[0].url)" class="w-full h-full object-contain" />
              <span v-else class="flex items-center justify-center w-full h-full text-text-muted text-lg">⌚</span>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm text-text truncate group-hover:text-accent transition-colors">{{ w.brand }} {{ w.model }}</p>
              <p class="text-xs text-text-muted">{{ formatCurrency(w.purchasePrice) }} / {{ w.timesWorn }} wears</p>
            </div>
            <span class="text-sm text-text-secondary">{{ formatCurrency(costPerWear(w)) }}/wear</span>
          </RouterLink>
        </div>
      </div>

      <!-- Movement Type Breakdown -->
      <div class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Movement Types</h3>
        <div v-if="movementBreakdown.length === 0" class="text-sm text-text-muted">No data</div>
        <div v-else class="space-y-3">
          <div v-for="item in movementBreakdown" :key="item.type" class="flex items-center gap-3">
            <span
              class="inline-block w-3 h-3 rounded-full flex-shrink-0"
              :class="movementColor(item.type)"
            />
            <span class="text-sm text-text flex-1">{{ item.type }}</span>
            <span class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary">
              {{ item.count }} ({{ item.pct }}%)
            </span>
          </div>
        </div>
      </div>

      <div class="grid lg:grid-cols-2 gap-4">
        <!-- Crystal Type Breakdown -->
        <div class="bg-bg-card border border-border rounded-xl p-4">
          <h3 class="text-lg font-medium text-text">Crystal Types</h3>
          <p class="text-xs text-text-muted mt-1 mb-4">Distribution of watches with crystal data</p>
          <div v-if="crystalBreakdown.length === 0" class="text-sm text-text-muted">Add crystal types to see the distribution</div>
          <div v-else class="space-y-3">
            <div v-for="item in crystalBreakdown" :key="item.label" class="space-y-1.5">
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm text-text truncate">{{ item.label }}</span>
                <span class="text-xs text-text-muted flex-shrink-0">{{ item.count }} ({{ item.pct }}%)</span>
              </div>
              <div class="h-2 bg-bg-surface rounded-full overflow-hidden">
                <div class="h-full bg-accent/60 rounded-full transition-all" :style="{ width: item.barPct + '%' }" />
              </div>
            </div>
          </div>
        </div>

        <!-- Case Size Breakdown -->
        <div class="bg-bg-card border border-border rounded-xl p-4">
          <h3 class="text-lg font-medium text-text">Case Size Distribution</h3>
          <p class="text-xs text-text-muted mt-1 mb-4">Grouped by case diameter in millimeters</p>
          <div v-if="caseSizeBreakdown.length === 0" class="text-sm text-text-muted">Add case sizes to see the distribution</div>
          <div v-else class="space-y-3">
            <div v-for="item in caseSizeBreakdown" :key="item.label" class="space-y-1.5">
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm text-text">{{ item.label }}</span>
                <span class="text-xs text-text-muted flex-shrink-0">{{ item.count }} ({{ item.pct }}%)</span>
              </div>
              <div class="h-2 bg-bg-surface rounded-full overflow-hidden">
                <div class="h-full bg-success/70 rounded-full transition-all" :style="{ width: item.barPct + '%' }" />
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Missing Watch Data -->
      <div class="bg-bg-card border border-border rounded-xl overflow-hidden">
        <div class="p-4 border-b border-border">
          <div class="flex items-start justify-between gap-4">
            <div>
              <h3 class="text-lg font-medium text-text">Missing Watch Data</h3>
              <p class="text-xs text-text-muted mt-1">Movement, crystal, case size, and water resistance</p>
            </div>
            <span class="px-3 py-1.5 bg-bg-surface border border-border rounded-full text-xs text-text-secondary flex-shrink-0">
              {{ watchesMissingData.length }} incomplete
            </span>
          </div>
        </div>
        <div v-if="watchesMissingData.length === 0" class="p-4 text-sm text-success">
          All watches have these details.
        </div>
        <div v-else class="divide-y divide-border">
          <RouterLink
            v-for="item in watchesMissingData"
            :key="item.watch.id"
            :to="`/watches/${item.watch.id}`"
            class="flex items-center gap-3 p-4 hover:bg-bg-surface transition-colors group"
          >
            <div class="w-10 h-10 rounded-lg bg-bg-surface overflow-hidden flex-shrink-0">
              <img
                v-if="item.watch.imageUrls.length"
                :src="imageUrl(item.watch.imageUrls[0].url)"
                class="w-full h-full object-contain"
              />
              <span v-else class="flex items-center justify-center w-full h-full text-text-muted text-lg">⌚</span>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm text-text truncate group-hover:text-accent transition-colors">
                {{ item.watch.brand }} {{ item.watch.model }}
              </p>
              <div class="flex flex-wrap gap-1.5 mt-1.5">
                <span
                  v-for="field in item.fields"
                  :key="field"
                  class="px-2 py-0.5 bg-danger/10 border border-danger/20 rounded-full text-xs text-danger"
                >
                  {{ field }}
                </span>
              </div>
            </div>
          </RouterLink>
        </div>
      </div>

      <!-- Most Popular Brands -->
      <div class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Top Brands</h3>
        <div v-if="brandBreakdown.length === 0" class="text-sm text-text-muted">No data</div>
        <div v-else class="space-y-3">
          <div v-for="(item, i) in brandBreakdown" :key="item.brand" class="flex items-center gap-3">
            <span class="text-sm font-medium text-accent w-5 text-right flex-shrink-0">{{ i + 1 }}.</span>
            <div class="flex-1 min-w-0">
              <div class="flex items-center justify-between mb-1">
                <span class="text-sm text-text truncate">{{ item.brand }}</span>
                <span class="text-xs text-text-muted flex-shrink-0 ml-2">{{ item.count }} watch{{ item.count > 1 ? 'es' : '' }}</span>
              </div>
              <div class="h-1.5 bg-bg-surface rounded-full overflow-hidden">
                <div class="h-full bg-accent/60 rounded-full transition-all" :style="{ width: item.pct + '%' }" />
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Most Worn -->
      <div class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Most Worn</h3>
        <div v-if="mostWorn.length === 0" class="text-sm text-text-muted">No wear data yet</div>
        <div v-else class="space-y-3">
          <RouterLink
            v-for="(w, i) in mostWorn"
            :key="w.id"
            :to="`/watches/${w.id}`"
            class="flex items-center gap-3 group"
          >
            <span class="text-sm font-medium text-accent w-5 text-right">{{ i + 1 }}.</span>
            <div class="w-10 h-10 rounded-lg bg-bg-surface overflow-hidden flex-shrink-0">
              <img v-if="w.imageUrls.length" :src="imageUrl(w.imageUrls[0].url)" class="w-full h-full object-contain" />
              <span v-else class="flex items-center justify-center w-full h-full text-text-muted text-lg">⌚</span>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm text-text truncate group-hover:text-accent transition-colors">{{ w.brand }} {{ w.model }}</p>
            </div>
            <span class="text-sm text-text-secondary">{{ w.timesWorn }}×</span>
          </RouterLink>
        </div>
      </div>

      <!-- Neglected Watches -->
      <div class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Neglected (30+ days)</h3>
        <div v-if="neglected.length === 0" class="text-sm text-text-muted">All watches are getting love! 🎉</div>
        <div v-else class="space-y-3">
          <RouterLink
            v-for="w in neglected"
            :key="w.id"
            :to="`/watches/${w.id}`"
            class="flex items-center gap-3 group"
          >
            <div class="w-10 h-10 rounded-lg bg-bg-surface overflow-hidden flex-shrink-0">
              <img v-if="w.imageUrls.length" :src="imageUrl(w.imageUrls[0].url)" class="w-full h-full object-contain" />
              <span v-else class="flex items-center justify-center w-full h-full text-text-muted text-lg">⌚</span>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm text-text truncate group-hover:text-accent transition-colors">{{ w.brand }} {{ w.model }}</p>
              <p class="text-xs text-text-muted">
                {{ w.lastWornDate ? `Last worn ${daysSince(w.lastWornDate)} days ago` : 'Never worn' }}
              </p>
            </div>
          </RouterLink>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { Watch } from '@/types'
import { getWatches, imageUrl } from '@/services/watches'
import { usePullToRefresh } from '@/composables/usePullToRefresh'
import PullToRefresh from '@/components/common/PullToRefresh.vue'

const { refreshing, pullDistance, pulling } = usePullToRefresh(load)

const watches = ref<Watch[]>([])
const loading = ref(true)
const error = ref(false)

const totalWears = computed(() => watches.value.reduce((sum, w) => sum + w.timesWorn, 0))
const avgWears = computed(() => watches.value.length ? (totalWears.value / watches.value.length).toFixed(1) : '0')
const pricedWatches = computed(() => watches.value.filter(w => hasValue(w.purchasePrice)))
const totalCollectionValue = computed(() => pricedWatches.value.reduce((sum, w) => sum + (w.purchasePrice ?? 0), 0))
const medianValue = computed(() => {
  const values = pricedWatches.value.map(w => w.purchasePrice ?? 0).sort((a, b) => a - b)
  if (values.length === 0) return 0

  const middle = Math.floor(values.length / 2)
  return values.length % 2 === 0 ? (values[middle - 1] + values[middle]) / 2 : values[middle]
})
const avgCostPerWear = computed(() => {
  const watchesWithWearCost = pricedWatches.value.filter(w => w.timesWorn > 0)
  if (watchesWithWearCost.length === 0) return 0

  const total = watchesWithWearCost.reduce((sum, w) => sum + costPerWear(w), 0)
  return total / watchesWithWearCost.length
})

const mostWorn = computed(() =>
  [...watches.value].filter(w => w.timesWorn > 0).sort((a, b) => b.timesWorn - a.timesWorn).slice(0, 5)
)

const topValuable = computed(() =>
  [...pricedWatches.value].sort((a, b) => (b.purchasePrice ?? 0) - (a.purchasePrice ?? 0)).slice(0, 5)
)

const resaleValuedWatches = computed(() => watches.value.filter(w => hasValue(w.currentResaleValue)))

const topResaleValue = computed(() =>
  [...resaleValuedWatches.value]
    .sort((a, b) => (b.currentResaleValue ?? 0) - (a.currentResaleValue ?? 0))
    .slice(0, 10)
)

const topResaleGain = computed(() =>
  [...resaleValuedWatches.value]
    .filter(w => hasValue(w.purchasePrice))
    .map(w => ({ watch: w, gain: (w.currentResaleValue ?? 0) - (w.purchasePrice ?? 0) }))
    .sort((a, b) => b.gain - a.gain)
    .slice(0, 10)
)

const brandValueBreakdown = computed(() => {
  const brands: Record<string, { value: number; count: number }> = {}
  pricedWatches.value.forEach(w => {
    const brand = w.brand.trim() || 'Unknown'
    const current = brands[brand] ?? { value: 0, count: 0 }
    brands[brand] = {
      value: current.value + (w.purchasePrice ?? 0),
      count: current.count + 1,
    }
  })
  const max = Math.max(...Object.values(brands).map(item => item.value), 1)
  return Object.entries(brands)
    .map(([brand, item]) => ({ brand, ...item, pct: Math.round((item.value / max) * 100) }))
    .sort((a, b) => b.value - a.value)
    .slice(0, 5)
})

const valueTimeline = computed(() => {
  let runningTotal = 0
  return [...pricedWatches.value]
    .sort((a, b) => acquisitionTime(a) - acquisitionTime(b))
    .map(w => {
      runningTotal += w.purchasePrice ?? 0
      return {
        date: acquisitionDate(w),
        total: runningTotal,
      }
    })
})

const valueTimelinePlot = computed(() => {
  const values = valueTimeline.value
  const max = Math.max(...values.map(point => point.total), 1)
  return values.map((point, index) => ({
    ...point,
    x: values.length === 1 ? 50 : (index / (values.length - 1)) * 100,
    y: 100 - (point.total / max) * 90,
  }))
})

const valueTimelinePoints = computed(() =>
  valueTimelinePlot.value.map(point => `${point.x},${point.y}`).join(' ')
)

const costPerWearLeaders = computed(() =>
  [...pricedWatches.value]
    .filter(w => w.timesWorn > 0)
    .sort((a, b) => costPerWear(a) - costPerWear(b))
    .slice(0, 5)
)

const neglected = computed(() => {
  const threshold = Date.now() - 30 * 24 * 60 * 60 * 1000
  return watches.value.filter(w => {
    if (!w.lastWornDate) return true
    return new Date(w.lastWornDate).getTime() < threshold
  }).sort((a, b) => {
    if (!a.lastWornDate) return -1
    if (!b.lastWornDate) return 1
    return new Date(a.lastWornDate).getTime() - new Date(b.lastWornDate).getTime()
  })
})

const movementBreakdown = computed(() => {
  const counts: Record<string, number> = {}
  watches.value.forEach(w => {
    if (!isMissingText(w.movementType)) {
      counts[w.movementType] = (counts[w.movementType] || 0) + 1
    }
  })
  const total = watches.value.length || 1
  return Object.entries(counts)
    .map(([type, count]) => ({ type, count, pct: Math.round((count / total) * 100) }))
    .sort((a, b) => b.count - a.count)
})

const crystalBreakdown = computed(() => {
  const counts = new Map<string, { label: string; count: number }>()
  watches.value.forEach(w => {
    if (isMissingText(w.crystalType)) return

    const label = w.crystalType!.trim()
    const key = label.toLocaleLowerCase()
    const item = counts.get(key)
    counts.set(key, { label: item?.label ?? label, count: (item?.count ?? 0) + 1 })
  })

  return toDistribution([...counts.values()])
})

const caseSizeBreakdown = computed(() => {
  const bins = [
    { label: 'Under 36 mm', count: 0 },
    { label: '36-39.9 mm', count: 0 },
    { label: '40-42.9 mm', count: 0 },
    { label: '43 mm and over', count: 0 },
  ]

  watches.value.forEach(w => {
    if (!hasValue(w.caseSizeMm)) return

    const size = w.caseSizeMm ?? 0
    if (size < 36) bins[0].count++
    else if (size < 40) bins[1].count++
    else if (size < 43) bins[2].count++
    else bins[3].count++
  })

  return toDistribution(bins.filter(bin => bin.count > 0), false)
})

const watchesMissingData = computed(() =>
  watches.value
    .map(watch => {
      const fields: string[] = []
      if (isMissingText(watch.movementType)) fields.push('Movement')
      if (isMissingText(watch.crystalType)) fields.push('Crystal')
      if (!hasValue(watch.caseSizeMm)) fields.push('Case size')
      if (isMissingText(watch.waterResistance)) fields.push('Water resistance')
      return { watch, fields }
    })
    .filter(item => item.fields.length > 0)
    .sort((a, b) => b.fields.length - a.fields.length || a.watch.brand.localeCompare(b.watch.brand))
)

const brandBreakdown = computed(() => {
  const counts: Record<string, number> = {}
  watches.value.forEach(w => { counts[w.brand] = (counts[w.brand] || 0) + 1 })
  const max = Math.max(...Object.values(counts), 1)
  return Object.entries(counts)
    .map(([brand, count]) => ({ brand, count, pct: Math.round((count / max) * 100) }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 5)
})

function hasValue(value: number | undefined): boolean {
  return value !== undefined && value > 0
}

function isMissingText(value: string | undefined): boolean {
  return !value?.trim()
}

function toDistribution(
  items: Array<{ label: string; count: number }>,
  sort = true,
): Array<{ label: string; count: number; pct: number; barPct: number }> {
  const total = items.reduce((sum, item) => sum + item.count, 0) || 1
  const max = Math.max(...items.map(item => item.count), 1)
  const distribution = items.map(item => ({
    ...item,
    pct: Math.round((item.count / total) * 100),
    barPct: Math.round((item.count / max) * 100),
  }))

  return sort ? distribution.sort((a, b) => b.count - a.count || a.label.localeCompare(b.label)) : distribution
}

function costPerWear(watch: Watch): number {
  if (!hasValue(watch.purchasePrice) || watch.timesWorn <= 0) return 0
  return (watch.purchasePrice ?? 0) / watch.timesWorn
}

function acquisitionDate(watch: Watch): string {
  return watch.purchaseDate ?? watch.createdAt
}

function acquisitionTime(watch: Watch): number {
  return new Date(acquisitionDate(watch)).getTime()
}

function valueShare(value: number | undefined): number {
  if (!hasValue(value) || totalCollectionValue.value === 0) return 0
  return Math.round(((value ?? 0) / totalCollectionValue.value) * 100)
}

function formatCurrency(value: number | undefined): string {
  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(value ?? 0)
}

function movementColor(type: string): string {
  const map: Record<string, string> = {
    Automatic: 'bg-accent',
    Manual: 'bg-success',
    Quartz: 'bg-blue-400',
    Digital: 'bg-purple-400',
  }
  return map[type] || 'bg-text-muted'
}

function daysSince(dateStr: string): number {
  // Parse as local date to avoid timezone offset issues with date-only strings
  const parts = dateStr.split('T')[0].split('-')
  const d = new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]))
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  return Math.floor((today.getTime() - d.getTime()) / (1000 * 60 * 60 * 24))
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

async function load() {
  loading.value = true
  error.value = false
  try {
    const allWatches = await getWatches()
    watches.value = allWatches.filter(w => !w.isRetired && !w.isWishList)
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
