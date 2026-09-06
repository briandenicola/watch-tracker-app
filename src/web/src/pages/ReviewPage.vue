<template>
  <div class="max-w-5xl mx-auto">
    <div class="mb-6 flex flex-wrap items-start justify-between gap-4">
      <div>
        <p class="text-xs uppercase tracking-[0.24em] text-accent mb-2">AI review</p>
        <h2 class="font-display text-3xl font-semibold text-text">Collection review</h2>
        <p class="text-sm text-text-muted mt-2 max-w-xl">
          One read of your collection and wish list together — what it does well, where it repeats
          itself, and what is missing.
        </p>
      </div>
      <button
        v-if="review"
        type="button"
        class="rounded-lg border border-accent/50 px-4 py-2 text-sm font-medium text-accent transition-colors hover:bg-accent/10 disabled:opacity-50"
        :disabled="!canGenerate"
        @click="generate"
      >
        {{ generating ? 'Reviewing…' : 'Run it again' }}
      </button>
    </div>

    <p v-if="!loading && !configured" role="alert" class="mb-4 rounded-xl border border-danger/30 bg-danger/5 p-4 text-sm text-danger">
      {{ configurationMessage }}
    </p>

    <div v-if="errorMessage" role="alert" class="mb-4 rounded-xl border border-danger/30 bg-danger/5 p-4">
      <p class="text-sm text-danger">{{ errorMessage }}</p>
      <button
        v-if="loadFailed"
        type="button"
        class="mt-2 text-xs font-medium text-accent hover:underline"
        @click="load"
      >
        Try loading it again
      </button>
    </div>

    <div v-if="loading" class="flex min-h-[20rem] items-center justify-center">
      <div class="h-10 w-10 animate-spin rounded-full border-2 border-accent border-t-transparent" />
    </div>

    <section v-else-if="generating" class="rounded-2xl border border-border bg-bg-card p-8 text-center">
      <div class="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-2 border-accent border-t-transparent" />
      <p class="font-display text-lg text-text">Reading your collection</p>
      <p class="mt-1 text-sm text-text-muted">
        Counting coverage and gaps first, then writing it up. This can take a minute or two.
      </p>
    </section>

    <section v-else-if="!review" class="rounded-2xl border border-border bg-bg-card px-6 py-12 text-center">
      <AppIcon name="review" :size="56" :stroke-width="1" class="mx-auto mb-5 text-accent" />
      <h3 class="font-display text-xl font-semibold text-text">No review yet</h3>
      <p class="mx-auto mt-2 max-w-md text-sm text-text-muted">
        Every number in the report is counted from your watches before the model sees it, so the
        write-up can only comment on what you actually own and want.
      </p>
      <button
        type="button"
        class="mt-6 rounded-lg bg-accent px-5 py-2.5 text-sm font-medium text-bg transition-colors hover:bg-accent-hover disabled:opacity-50"
        :disabled="!canGenerate"
        @click="generate"
      >
        Review my collection
      </button>
    </section>

    <div v-else class="space-y-6">
      <p class="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-text-muted">
        <span>Reviewed {{ reviewedAt }}</span>
        <span
          v-if="review.isStale"
          class="rounded-full border border-accent/40 px-2 py-0.5 text-[11px] font-medium text-accent"
        >
          Your watches changed since this ran
        </span>
      </p>

      <p v-if="review.summary" class="font-display text-lg leading-8 text-text">
        {{ review.summary }}
      </p>

      <section
        v-for="section in sections"
        :key="section.key"
        class="rounded-2xl border border-border bg-bg-card p-5 sm:p-6"
      >
        <h3 class="font-display text-xl font-semibold text-text">{{ section.title }}</h3>
        <p class="mt-1 text-xs text-text-muted">{{ section.blurb }}</p>

        <p v-if="!section.findings.length" class="mt-4 text-sm text-text-muted">
          Nothing to report here.
        </p>

        <ol v-else class="mt-4 space-y-5">
          <li v-for="(finding, index) in section.findings" :key="`${section.key}-${index}`">
            <h4 class="text-sm font-semibold text-text">{{ finding.summary }}</h4>
            <p v-if="finding.detail" class="mt-1 text-sm leading-6 text-text-secondary">
              {{ finding.detail }}
            </p>
            <div v-if="finding.watchIds.length" class="mt-2 flex flex-wrap gap-2">
              <RouterLink
                v-for="watchId in finding.watchIds"
                :key="watchId"
                :to="`/watches/${watchId}`"
                class="rounded-full border border-border px-2.5 py-1 text-xs text-text-secondary transition-colors hover:border-accent/50 hover:text-accent"
              >
                {{ watchName(watchId) }}
              </RouterLink>
            </div>
          </li>
        </ol>
      </section>

      <section class="rounded-2xl border border-border bg-bg-card p-5 sm:p-6">
        <h3 class="font-display text-xl font-semibold text-text">The numbers behind it</h3>
        <p class="mt-1 text-xs text-text-muted">
          Counted from your watches, not written by the model.
        </p>

        <div class="mt-4 grid gap-3 sm:grid-cols-3">
          <div v-for="set in sets" :key="set.label" class="rounded-xl border border-border-light bg-bg-surface p-4">
            <p class="text-xs uppercase tracking-wider text-text-muted">{{ set.label }}</p>
            <p class="mt-1 font-display text-2xl font-semibold text-text">{{ set.watchCount }}</p>
            <p class="text-xs text-text-muted">
              {{ set.watchCount === 1 ? 'watch' : 'watches' }} ·
              {{ set.dataCompletenessPercent }}% filled in
            </p>
          </div>
        </div>

        <details v-for="set in sets" :key="`detail-${set.label}`" class="group mt-3 border-t border-border-light pt-3">
          <summary class="flex cursor-pointer list-none items-center justify-between text-sm font-medium text-text">
            <span>{{ set.label }} breakdown</span>
            <span class="text-xs text-text-muted transition-transform group-open:rotate-180">▾</span>
          </summary>

          <div class="mt-4 space-y-5">
            <div v-for="dimension in set.coverage" :key="dimension.dimension">
              <p class="text-xs uppercase tracking-wider text-text-muted">{{ dimension.dimension }}</p>
              <p v-if="!dimension.values.length" class="mt-1 text-sm text-text-muted">Not recorded.</p>
              <ul v-else class="mt-2 space-y-1.5">
                <li
                  v-for="value in dimension.values"
                  :key="value.value"
                  class="grid grid-cols-[minmax(6rem,10rem)_1fr_2rem] items-center gap-3 text-sm"
                >
                  <span class="truncate text-text-secondary">{{ value.value }}</span>
                  <span class="h-1.5 overflow-hidden rounded-full bg-bg-elevated">
                    <span
                      class="block h-full rounded-full bg-accent/70"
                      :style="{ width: `${barWidth(value.count, set.watchCount)}%` }"
                    />
                  </span>
                  <span class="text-right text-xs tabular-nums text-text-muted">{{ value.count }}</span>
                </li>
              </ul>
            </div>

            <div v-if="set.redundancies.length">
              <p class="text-xs uppercase tracking-wider text-text-muted">Repeats</p>
              <ul class="mt-2 space-y-2">
                <li v-for="(insight, index) in set.redundancies" :key="`r-${index}`" class="text-sm text-text-secondary">
                  <span class="text-text">{{ insight.summary }}</span> — {{ insight.reason }}
                </li>
              </ul>
            </div>

            <div v-if="set.gaps.length">
              <p class="text-xs uppercase tracking-wider text-text-muted">Gaps</p>
              <ul class="mt-2 space-y-2">
                <li v-for="(insight, index) in set.gaps" :key="`g-${index}`" class="text-sm text-text-secondary">
                  <span class="text-text">{{ insight.summary }}</span> — {{ insight.reason }}
                </li>
              </ul>
            </div>
          </div>
        </details>

        <details v-if="review.facts.wishlistFit.length" class="group mt-3 border-t border-border-light pt-3">
          <summary class="flex cursor-pointer list-none items-center justify-between text-sm font-medium text-text">
            <span>How each wanted watch fits</span>
            <span class="text-xs text-text-muted transition-transform group-open:rotate-180">▾</span>
          </summary>
          <ul class="mt-4 space-y-4">
            <li v-for="fit in rankedFit" :key="fit.watchId">
              <div class="flex items-baseline justify-between gap-3">
                <RouterLink
                  :to="`/watches/${fit.watchId}`"
                  class="text-sm font-medium text-text transition-colors hover:text-accent"
                >
                  {{ watchName(fit.watchId) }}
                </RouterLink>
                <span class="text-xs tabular-nums text-text-muted">{{ fit.totalScore }} / 100</span>
              </div>
              <ul class="mt-1 space-y-1">
                <li v-for="reason in fit.reasons" :key="reason" class="flex gap-2 text-sm text-text-secondary">
                  <span class="text-accent">✦</span>
                  <span>{{ reason }}</span>
                </li>
              </ul>
            </li>
          </ul>
        </details>
      </section>

      <section class="rounded-2xl border border-border bg-bg-card p-5 sm:p-6">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h3 class="font-display text-xl font-semibold text-text">Watches that would fill the gaps</h3>
            <p class="mt-1 text-xs text-text-muted">
              Real listings, scored against your collection. Prices and availability come from the
              marketplace, never from the model.
            </p>
          </div>
          <button
            type="button"
            class="rounded-lg border border-accent/50 px-4 py-2 text-sm font-medium text-accent transition-colors hover:bg-accent/10 disabled:opacity-50"
            data-test="find-candidates"
            :disabled="!canFindCandidates"
            @click="findCandidates"
          >
            {{ findingCandidates ? 'Searching…' : candidates.length ? 'Search again' : 'Find candidates' }}
          </button>
        </div>

        <p v-if="candidateError" role="alert" class="mt-4 rounded-xl border border-danger/30 bg-danger/5 p-3 text-sm text-danger">
          {{ candidateError }}
        </p>

        <p
          v-for="status in unavailableProviders"
          :key="status.provider"
          class="mt-4 rounded-xl border border-border-light bg-bg-surface p-3 text-sm text-text-secondary"
        >
          <template v-if="status.status === 'NotConfigured'">
            {{ status.provider }} is not set up{{ auth.isAdmin ? ' — add its credentials under Admin → Settings.' : '. Ask an administrator to set it up.' }}
          </template>
          <template v-else>
            {{ status.provider }} could not be reached{{ status.error ? `: ${status.error}` : '.' }}
          </template>
        </p>

        <div v-if="findingCandidates" class="mt-6 flex flex-col items-center py-8 text-center">
          <div class="mb-4 h-9 w-9 animate-spin rounded-full border-2 border-accent border-t-transparent" />
          <p class="text-sm text-text-muted">
            Searching for watches that answer the gaps, then scoring what comes back.
          </p>
        </div>

        <div v-else-if="candidates.length" class="mt-5 grid gap-3">
          <article
            v-for="card in candidates"
            :key="`${card.provider}-${card.providerItemId}`"
            class="overflow-hidden rounded-xl border border-border bg-bg-surface"
          >
            <div class="grid grid-cols-[5.5rem_1fr] gap-3 p-3 sm:grid-cols-[7rem_1fr]">
              <div class="flex aspect-square items-center justify-center overflow-hidden rounded-lg bg-bg-elevated">
                <img
                  v-if="safeImageUrl(card.imageUrl)"
                  :src="card.imageUrl!"
                  :alt="card.title"
                  class="h-full w-full object-contain"
                  loading="lazy"
                />
                <AppIcon v-else name="watch" :size="34" class="text-text-muted" />
              </div>
              <div class="min-w-0">
                <h4 class="line-clamp-2 text-sm font-semibold text-text">{{ card.title }}</h4>
                <p class="mt-2 text-base font-semibold text-text">
                  {{ money(card.totalPrice ?? card.price, card.currency) }}
                </p>
                <p v-if="card.shippingPrice != null" class="text-[0.7rem] text-text-muted">
                  Item {{ money(card.price, card.currency) }} + shipping
                  {{ money(card.shippingPrice, card.currency) }}
                </p>
                <div class="mt-1 flex flex-wrap gap-x-3 gap-y-1 text-[0.7rem] text-text-muted">
                  <span>{{ card.condition || 'Condition not provided' }}</span>
                  <span v-if="card.fitScore != null">Fit score {{ card.fitScore }}/100</span>
                  <span>Observed {{ observed(card.observedAt) }}</span>
                </div>
              </div>
            </div>
            <div class="border-t border-border px-3 py-2">
              <ul v-if="card.reasons.length" class="space-y-1">
                <li v-for="reason in card.reasons" :key="reason" class="flex gap-2 text-xs text-text-secondary">
                  <span class="text-accent">✦</span>
                  <span>{{ reason }}</span>
                </li>
              </ul>
              <div class="mt-3 flex flex-wrap items-center gap-3">
                <a
                  v-if="safeExternalUrl(card.itemUrl)"
                  :href="card.itemUrl!"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="text-xs font-medium text-accent hover:underline"
                >
                  View the listing
                </a>
                <button
                  type="button"
                  class="rounded-lg border border-border px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:border-accent/50 hover:text-accent disabled:opacity-50"
                  data-test="add-candidate"
                  :disabled="pendingAdd === cardKey(card)"
                  @click="addToWishlist(card)"
                >
                  {{ pendingAdd === cardKey(card) ? 'Adding…' : 'Add to wish list' }}
                </button>
                <span v-if="addStatus[cardKey(card)]" class="text-xs text-text-muted">
                  {{ addStatus[cardKey(card)] }}
                </span>
              </div>
            </div>
          </article>

          <p v-if="review.candidates.droppedStaleListings" class="text-xs text-text-muted">
            Some listings found earlier have been dropped for being over a week old. Search again for
            what is on sale now.
          </p>
        </div>

        <p v-else-if="!candidateError && !unavailableProviders.length" class="mt-5 text-sm text-text-muted">
          {{ review.candidates.generatedAt
            ? 'Nothing came back that fills a gap. Try again later — listings turn over daily.'
            : 'Search the marketplace for watches that answer the gaps above.' }}
        </p>
      </section>

    </div>
  </div>
</template>

<script setup lang="ts">
import axios from 'axios'
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AppIcon from '@/components/icons/AppIcon.vue'
import {
  addCandidateToWishlist,
  generateCandidates,
  generateCollectionReview,
  getCollectionReview,
} from '@/services/review'
import { useAuthStore } from '@/stores/auth'
import { formatInstant } from '@/utils/dateTime'
import { serverMessage } from '@/utils/serverMessage'
import type { AdvisorRecommendationCard, CollectionReviewState, ReviewWatch } from '@/types'

const auth = useAuthStore()
const state = ref<CollectionReviewState | null>(null)
const loading = ref(true)
const generating = ref(false)
const errorMessage = ref('')
const loadFailed = ref(false)
const findingCandidates = ref(false)
const candidateError = ref('')
const pendingAdd = ref('')
const addStatus = ref<Record<string, string>>({})

const review = computed(() => state.value?.review ?? null)
const configured = computed(() => state.value?.configured ?? false)
const canGenerate = computed(() => configured.value && !generating.value)
const candidates = computed(() => review.value?.candidates.candidates ?? [])
const canFindCandidates = computed(() =>
  configured.value && !generating.value && !findingCandidates.value)

// Success needs no announcement; the cards are the announcement.
const unavailableProviders = computed(() =>
  (review.value?.candidates.marketplaceStatus ?? []).filter((s) => s.status !== 'Success'))

const configurationMessage = computed(() => auth.isAdmin
  ? state.value?.configurationHint || 'The collection review needs Ollama.'
  : 'The collection review needs Ollama. Ask an administrator to set it up.')

const reviewedAt = computed(() => {
  const generatedAt = review.value?.generatedAt
  if (!generatedAt) return ''
  return formatInstant(generatedAt, { dateStyle: 'medium', timeStyle: 'short' })
})

const sections = computed(() => [
  {
    key: 'strengths',
    title: 'Strengths',
    blurb: 'What the collection already covers well.',
    findings: review.value?.strengths ?? [],
  },
  {
    key: 'weaknesses',
    title: 'Weaknesses',
    blurb: 'Repetition, over-concentration, and wanted watches you already own something like.',
    findings: review.value?.weaknesses ?? [],
  },
  {
    key: 'recommendations',
    title: 'Recommendations',
    blurb: 'Gaps worth filling, judged against what you own and what you want.',
    findings: review.value?.recommendations ?? [],
  },
])

const sets = computed(() => {
  const facts = review.value?.facts
  return facts ? [facts.collection, facts.wishlist, facts.combined] : []
})

// Best fit first: the point of the score is which wanted watch earns its place.
const rankedFit = computed(() =>
  [...(review.value?.facts.wishlistFit ?? [])].sort((a, b) => b.totalScore - a.totalScore))

const watchesById = computed(() => {
  const facts = review.value?.facts
  const all: ReviewWatch[] = facts ? [...facts.collectionWatches, ...facts.wishlistWatches] : []
  return new Map(all.map((watch) => [watch.id, watch]))
})

function watchName(watchId: number): string {
  const watch = watchesById.value.get(watchId)
  return watch ? `${watch.brand} ${watch.model}`.trim() : `Watch #${watchId}`
}

function cardKey(card: AdvisorRecommendationCard): string {
  return `${card.provider}-${card.providerItemId}`
}

function money(value?: number | null, currency?: string | null): string {
  if (value == null) return 'Price unavailable'
  if (!currency) return value.toLocaleString()
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(value)
  } catch {
    return `${value.toLocaleString()} ${currency}`
  }
}

function observed(value?: string | null): string {
  if (!value) return 'at an unknown time'
  return formatInstant(value, { dateStyle: 'medium' }) || 'at an unknown time'
}

function safeExternalUrl(value?: string | null): boolean {
  if (!value) return false
  try {
    const url = new URL(value)
    return url.protocol === 'https:' || url.protocol === 'http:'
  } catch {
    return false
  }
}

function safeImageUrl(value?: string | null): boolean {
  if (!value) return false
  try {
    return new URL(value).protocol === 'https:'
  } catch {
    return false
  }
}

function barWidth(count: number, total: number): number {
  return total > 0 ? Math.round((count / total) * 100) : 0
}

function requestError(error: unknown, fallback: string): string {
  if (!axios.isAxiosError(error)) return fallback
  if (error.response?.status === 429) {
    return 'That is a lot of reviews at once. Give it a minute and try again.'
  }
  // A 500 answers with problem details rather than an `error` field, so reading
  // only that field turned every server fault into the same generic sentence.
  if (!error.response) return 'The review API is unreachable. Check your connection and try again.'
  return serverMessage(error) || fallback
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  loadFailed.value = false
  try {
    state.value = await getCollectionReview()
  } catch (error: unknown) {
    loadFailed.value = true
    errorMessage.value = requestError(error, 'Unable to load your collection review right now.')
  } finally {
    loading.value = false
  }
}

async function generate() {
  if (!canGenerate.value) return
  generating.value = true
  errorMessage.value = ''
  try {
    state.value = await generateCollectionReview()
  } catch (error: unknown) {
    errorMessage.value = requestError(error, 'Unable to review your collection right now.')
  } finally {
    generating.value = false
  }
}

async function findCandidates() {
  if (!canFindCandidates.value || !review.value) return
  findingCandidates.value = true
  candidateError.value = ''
  addStatus.value = {}
  try {
    review.value.candidates = await generateCandidates()
  } catch (error: unknown) {
    candidateError.value = requestError(error, 'Unable to search for candidates right now.')
  } finally {
    findingCandidates.value = false
  }
}

async function addToWishlist(card: AdvisorRecommendationCard) {
  if (!card.provider || !card.providerItemId) return
  const key = cardKey(card)
  pendingAdd.value = key
  try {
    const result = await addCandidateToWishlist(card.provider, card.providerItemId)
    addStatus.value = { ...addStatus.value, [key]: result.message }
  } catch (error: unknown) {
    addStatus.value = {
      ...addStatus.value,
      [key]: requestError(error, 'Could not add this one to your wish list.'),
    }
  } finally {
    pendingAdd.value = ''
  }
}

onMounted(load)
</script>
