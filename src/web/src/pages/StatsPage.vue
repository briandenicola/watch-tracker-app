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

      <!-- Recent Activity -->
      <div class="bg-bg-card border border-border rounded-xl p-4">
        <h3 class="text-lg font-medium text-text mb-4">Recent Activity</h3>
        <div v-if="recentLogs.length === 0" class="text-sm text-text-muted">No wear logs yet</div>
        <div v-else class="relative">
          <!-- Timeline line -->
          <div class="absolute left-[15px] top-2 bottom-2 w-px bg-border" />
          <div class="space-y-4">
            <RouterLink
              v-for="(log, i) in recentLogs"
              :key="log.id"
              :to="`/watches/${log.watchId}`"
              class="flex items-start gap-4 group relative"
            >
              <!-- Timeline dot -->
              <div class="relative z-10 flex-shrink-0 mt-1">
                <div
                  class="w-[9px] h-[9px] rounded-full border-2 transition-colors ml-[11px]"
                  :class="i === 0 ? 'bg-accent border-accent' : 'bg-bg-card border-text-muted group-hover:border-accent'"
                />
              </div>
              <!-- Card -->
              <div class="flex-1 flex items-center gap-3 px-3 py-2.5 rounded-lg bg-bg-surface/50 border border-transparent group-hover:border-accent/30 transition-all">
                <div class="flex-1 min-w-0">
                  <p class="text-sm text-text font-medium truncate group-hover:text-accent transition-colors">
                    {{ log.watchBrand }} {{ log.watchModel }}
                  </p>
                  <p class="text-xs text-text-muted mt-0.5">{{ formatRelativeDate(log.wornDate) }}</p>
                </div>
                <span class="text-xs text-text-muted flex-shrink-0">{{ formatDate(log.wornDate) }}</span>
              </div>
            </RouterLink>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { Watch, WearLog } from '@/types'
import { getWatches, getWearLogs, imageUrl } from '@/services/watches'
import { usePullToRefresh } from '@/composables/usePullToRefresh'
import PullToRefresh from '@/components/common/PullToRefresh.vue'

const { refreshing, pullDistance, pulling } = usePullToRefresh(load)

const watches = ref<Watch[]>([])
const wearLogs = ref<WearLog[]>([])
const loading = ref(true)
const error = ref(false)

const totalWears = computed(() => watches.value.reduce((sum, w) => sum + w.timesWorn, 0))
const avgWears = computed(() => watches.value.length ? (totalWears.value / watches.value.length).toFixed(1) : '0')

const mostWorn = computed(() =>
  [...watches.value].filter(w => w.timesWorn > 0).sort((a, b) => b.timesWorn - a.timesWorn).slice(0, 5)
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
  watches.value.forEach(w => { counts[w.movementType] = (counts[w.movementType] || 0) + 1 })
  const total = watches.value.length || 1
  return Object.entries(counts)
    .map(([type, count]) => ({ type, count, pct: Math.round((count / total) * 100) }))
    .sort((a, b) => b.count - a.count)
})

const recentLogs = computed(() => [...wearLogs.value].sort((a, b) => new Date(b.wornDate).getTime() - new Date(a.wornDate).getTime()).slice(0, 10))

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
  return Math.floor((Date.now() - new Date(dateStr).getTime()) / (1000 * 60 * 60 * 24))
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

function formatRelativeDate(dateStr: string): string {
  const days = daysSince(dateStr)
  if (days === 0) return 'Today'
  if (days === 1) return 'Yesterday'
  if (days < 7) return `${days} days ago`
  if (days < 14) return '1 week ago'
  return `${Math.floor(days / 7)} weeks ago`
}

async function load() {
  loading.value = true
  error.value = false
  try {
    const [allWatches, logs] = await Promise.all([getWatches(), getWearLogs()])
    watches.value = allWatches.filter(w => !w.isRetired && !w.isWishList)
    wearLogs.value = logs
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
