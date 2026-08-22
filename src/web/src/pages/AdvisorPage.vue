<template>
  <div class="mx-auto flex min-h-[calc(100dvh-8.5rem)] max-w-6xl flex-col">
    <header class="mb-4 flex flex-wrap items-start justify-between gap-3">
      <div>
        <p class="mb-1 text-xs uppercase tracking-[0.24em] text-accent">Collection Advisor</p>
        <h2 class="font-display text-3xl font-semibold text-text">Ask your collection</h2>
        <p class="mt-1 max-w-2xl text-sm text-text-muted">
          Compare collection gaps, current asking prices, brands, and value with source-backed research.
        </p>
      </div>
      <button
        type="button"
        class="rounded-lg border border-border px-4 py-2 text-sm text-text-secondary transition-colors hover:border-accent hover:text-text disabled:opacity-50"
        :disabled="loading || sending || loadFailed"
        @click="startOver"
      >
        New conversation
      </button>
    </header>

    <section class="advisor-shell">
      <div
        ref="transcriptEl"
        class="min-h-0 flex-1 overflow-y-auto px-4 py-5 sm:px-6"
        aria-live="polite"
        :aria-busy="sending"
      >
        <div v-if="loading" class="flex h-full min-h-80 flex-col items-center justify-center text-center">
          <div class="mb-4 h-9 w-9 animate-spin rounded-full border-2 border-accent border-t-transparent" />
          <p class="text-sm text-text-secondary">Loading your advisor conversation…</p>
        </div>

        <div v-else-if="!messages.length" class="mx-auto flex min-h-80 max-w-3xl flex-col justify-center">
          <div class="mb-6 text-center">
            <AppIcon name="advisor" :size="52" :stroke-width="1" class="mx-auto mb-4 text-accent" />
            <h3 class="font-display text-xl font-semibold text-text">What would you like to understand?</h3>
            <p class="mx-auto mt-2 max-w-lg text-sm text-text-muted">
              The advisor will ask for missing constraints, inspect only your collection, and clearly label current
              marketplace asking prices and source freshness.
            </p>
          </div>
          <div class="grid gap-2 sm:grid-cols-2">
            <button
              v-for="prompt in starterPrompts"
              :key="prompt"
              type="button"
              class="rounded-xl border border-border bg-bg-surface p-3 text-left text-sm text-text-secondary transition-colors hover:border-accent/60 hover:text-text disabled:opacity-50"
              :disabled="!canChat"
              @click="send(prompt)"
            >
              {{ prompt }}
            </button>
          </div>
        </div>

        <div v-else class="mx-auto max-w-4xl space-y-5">
          <article
            v-for="message in messages"
            :key="message.id"
            class="flex"
            :class="message.role === 'User' ? 'justify-end' : 'justify-start'"
          >
            <div
              class="max-w-[92%] sm:max-w-[82%]"
              :class="message.role === 'User' ? 'user-message' : 'assistant-message'"
            >
              <div
                v-if="message.role === 'Assistant'"
                class="prose-markdown text-sm leading-6"
                v-html="renderMarkdown(message.content)"
              />
              <p v-else class="whitespace-pre-wrap text-sm leading-6">{{ message.content }}</p>

              <div v-if="message.toolActivity.length" class="mt-3 space-y-1.5 border-t border-border/70 pt-3">
                <div
                  v-for="(activity, index) in message.toolActivity"
                  :key="`${activity.tool}-${index}`"
                  class="flex items-start gap-2 text-xs"
                  :class="toolStatusClass(activity.status)"
                >
                  <span class="mt-1 h-1.5 w-1.5 flex-none rounded-full bg-current" />
                  <span>
                    <strong class="font-medium">{{ toolLabel(activity.tool) }}</strong>
                    <span> · {{ toolStatusLabel(activity.status) }}</span>
                    <span v-if="activity.durationMs > 0"> · {{ activity.durationMs }} ms</span>
                    <span v-if="activity.message" class="block text-text-muted">{{ activity.message }}</span>
                  </span>
                </div>
              </div>

              <div v-if="message.recommendationCards.length" class="mt-4 grid gap-3">
                <article
                  v-for="card in message.recommendationCards"
                  :key="`${card.provider}-${card.providerItemId}`"
                  class="overflow-hidden rounded-xl border border-border bg-bg-card"
                >
                  <div class="grid grid-cols-[5.5rem_1fr] gap-3 p-3 sm:grid-cols-[7rem_1fr]">
                    <div class="flex aspect-square items-center justify-center overflow-hidden rounded-lg bg-bg-surface">
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
                      <p class="text-[0.65rem] uppercase tracking-wider text-accent">External marketplace candidate</p>
                      <h4 class="mt-1 line-clamp-2 text-sm font-semibold text-text">{{ card.title }}</h4>
                      <p class="mt-2 text-base font-semibold text-text">
                        {{ money(card.totalPrice ?? card.price, card.currency) }}
                      </p>
                      <p v-if="card.totalPrice == null && card.shippingPrice == null" class="text-[0.7rem] text-accent">
                        Shipping was not available; displayed price may not be the delivered total.
                      </p>
                      <p v-else-if="card.shippingPrice != null" class="text-[0.7rem] text-text-muted">
                        Item {{ money(card.price, card.currency) }} + shipping {{ money(card.shippingPrice, card.currency) }}
                      </p>
                      <div class="mt-1 flex flex-wrap gap-x-3 gap-y-1 text-[0.7rem] text-text-muted">
                        <span>{{ card.condition || 'Condition not provided' }}</span>
                        <span v-if="card.fitScore != null">Fit score {{ card.fitScore }}/100</span>
                      </div>
                    </div>
                  </div>
                  <div class="border-t border-border px-3 py-2">
                    <p
                      class="text-[0.68rem]"
                      :class="isStale(card.observedAt) ? 'font-medium text-danger' : 'text-text-muted'"
                    >
                      Observed {{ observed(card.observedAt) }}. Availability and price may have changed.
                      <template v-if="isStale(card.observedAt)">
                        This saved result is stale; ask the advisor to search again before acting.
                      </template>
                    </p>
                    <ul v-if="card.reasons.length" class="mt-2 space-y-1">
                      <li v-for="reason in card.reasons" :key="reason" class="text-xs text-text-secondary">
                        {{ reason }}
                      </li>
                    </ul>
                    <a
                      v-if="safeExternalUrl(card.itemUrl)"
                      :href="card.itemUrl!"
                      target="_blank"
                      rel="noopener noreferrer"
                      class="mt-2 inline-flex text-xs font-medium text-accent hover:underline"
                    >
                      View observed listing
                    </a>
                    <div
                      v-if="card.provider && card.providerItemId"
                      class="mt-3 border-t border-border/70 pt-3"
                    >
                      <div class="flex flex-wrap gap-1.5">
                        <button
                          type="button"
                          class="min-h-11 rounded-full border px-3 py-2 text-[0.68rem] transition-colors disabled:opacity-50"
                          :class="card.feedback?.kind === option.kind
                            ? 'border-accent bg-accent/10 text-accent'
                            : 'border-border text-text-muted hover:border-accent/60'"
                          :disabled="actionPending(actionKey(message.id, card))"
                          v-for="option in feedbackOptions"
                          :key="option.kind"
                          @click="setFeedback(message.id, card, option.kind)"
                        >
                          {{ option.label }}
                        </button>
                        <button
                          v-if="card.feedback"
                          type="button"
                          class="min-h-11 px-3 py-2 text-[0.68rem] text-text-muted hover:text-danger disabled:opacity-50"
                          :disabled="actionPending(actionKey(message.id, card))"
                          @click="clearFeedback(message.id, card)"
                        >
                          Clear
                        </button>
                      </div>
                      <div class="mt-2 flex flex-wrap items-center gap-2">
                        <input
                          :value="feedbackNote(message.id, card)"
                          type="text"
                          maxlength="500"
                          :aria-label="`Optional feedback note for ${card.title}`"
                          placeholder="Optional feedback note"
                          class="min-w-48 flex-1 rounded-lg border border-border bg-bg px-2.5 py-1.5 text-xs text-text outline-none focus:border-accent"
                          @input="setFeedbackNote(message.id, card, ($event.target as HTMLInputElement).value)"
                        />
                        <button
                          v-if="card.feedback"
                          type="button"
                          class="min-h-11 px-2 text-xs font-medium text-accent hover:underline disabled:opacity-50"
                          :disabled="actionPending(actionKey(message.id, card))"
                          @click="setFeedback(message.id, card, card.feedback.kind)"
                        >
                          Save note
                        </button>
                        <button
                          type="button"
                          class="min-h-11 rounded-lg bg-accent px-3 py-2 text-xs font-semibold text-bg disabled:opacity-50"
                          :disabled="actionPending(actionKey(message.id, card))"
                          @click="addToWishlist(message.id, card)"
                        >
                          Add to wishlist
                        </button>
                      </div>
                      <p
                        v-if="actionStatus[actionKey(message.id, card)]"
                        class="mt-2 text-xs text-text-muted"
                        role="status"
                      >
                        {{ actionStatus[actionKey(message.id, card)] }}
                      </p>
                    </div>
                  </div>
                </article>
              </div>

              <div v-if="message.citations.length" class="mt-4 border-t border-border/70 pt-3">
                <p class="mb-2 text-[0.65rem] uppercase tracking-wider text-text-muted">Sources</p>
                <ol class="space-y-2">
                  <li v-for="citation in message.citations" :key="citation.url" class="text-xs">
                    <a
                      v-if="safeExternalUrl(citation.url)"
                      :href="citation.url"
                      target="_blank"
                      rel="noopener noreferrer"
                      class="font-medium text-accent hover:underline"
                    >
                      {{ citation.title }}
                    </a>
                    <p class="mt-0.5 text-[0.68rem] text-text-muted">
                      {{ citation.provider }} · {{ citation.confidence }} confidence · observed
                      {{ observed(citation.observedAt) }}
                    </p>
                  </li>
                </ol>
              </div>

              <div v-if="message.followUps.length" class="mt-3 flex flex-wrap gap-2">
                <button
                  v-for="followUp in message.followUps"
                  :key="followUp"
                  type="button"
                  class="rounded-full border border-accent/40 px-3 py-1.5 text-xs text-accent transition-colors hover:bg-accent/10 disabled:opacity-50"
                  :disabled="sending || !configured"
                  @click="send(followUp)"
                >
                  {{ followUp }}
                </button>
              </div>
            </div>
          </article>

          <div v-if="sending" class="flex justify-start">
            <div class="assistant-message text-sm text-text-muted">
              <span class="inline-flex items-center gap-2">
                <span class="h-2 w-2 animate-pulse rounded-full bg-accent" />
                Checking your collection and approved sources…
              </span>
            </div>
          </div>
        </div>
      </div>

      <footer class="border-t border-border bg-bg-card px-4 py-3 sm:px-6">
        <p v-if="!loading && !configured" role="alert" class="mb-2 text-sm text-danger">
          {{ configurationMessage }}
        </p>
        <div v-if="errorMessage" role="alert" class="mb-2 rounded-lg border border-danger/30 bg-danger/5 p-3">
          <p class="text-sm text-danger">{{ errorMessage }}</p>
          <button
            v-if="loadFailed"
            type="button"
            class="mt-2 text-xs font-medium text-accent hover:underline"
            @click="load"
          >
            Retry loading this conversation
          </button>
          <button
            v-if="retryMessage"
            type="button"
            class="mt-2 text-xs font-medium text-accent hover:underline"
            @click="send(retryMessage)"
          >
            Try again
          </button>
        </div>
        <div class="flex items-end gap-2">
          <label for="advisor-message" class="sr-only">Message the collection advisor</label>
          <textarea
            id="advisor-message"
            ref="composerEl"
            v-model="draft"
            rows="2"
            maxlength="2000"
            :disabled="!canChat"
            placeholder="Ask about collection gaps, a brand, current listings, or value…"
            class="min-h-[3rem] flex-1 resize-none rounded-xl border border-border bg-bg px-3 py-2.5 text-sm text-text outline-none transition-colors placeholder:text-text-muted focus:border-accent disabled:opacity-50"
            @keydown.enter.exact.prevent="send()"
          />
          <button
            type="button"
            class="rounded-xl bg-accent px-5 py-3 text-sm font-semibold text-bg transition-colors hover:bg-accent-hover disabled:opacity-50"
            :disabled="!canSend"
            @click="send()"
          >
            {{ sending ? 'Working…' : 'Send' }}
          </button>
        </div>
        <p class="mt-2 text-[0.68rem] text-text-muted">
          Marketplace prices are observations, not guarantees. Verify condition, seller, total cost, and availability.
        </p>
      </footer>
    </section>
  </div>
</template>

<script setup lang="ts">
import axios from 'axios'
import { computed, nextTick, onMounted, ref } from 'vue'
import { marked } from 'marked'
import AppIcon from '@/components/icons/AppIcon.vue'
import {
  addAdvisorRecommendationToWishlist,
  getAdvisorChat,
  removeAdvisorFeedback,
  saveAdvisorFeedback,
  sendAdvisorMessage,
  startAdvisorSession,
} from '@/services/advisor'
import { useAuthStore } from '@/stores/auth'
import { formatInstant } from '@/utils/dateTime'
import type {
  AdvisorChatState,
  AdvisorFeedbackKind,
  AdvisorRecommendationCard,
  AdvisorToolActivity,
} from '@/types'

const starterPrompts = [
  'What is missing from my collection?',
  'What would complement my collection under $2,000 USD?',
  'Which watches in my collection overlap the most?',
  'Help me research a watch brand or model.',
]

const auth = useAuthStore()
const state = ref<AdvisorChatState | null>(null)
const loading = ref(true)
const sending = ref(false)
const draft = ref('')
const errorMessage = ref('')
const retryMessage = ref('')
const loadFailed = ref(false)
const transcriptEl = ref<HTMLElement | null>(null)
const composerEl = ref<HTMLTextAreaElement | null>(null)
const feedbackDrafts = ref<Record<string, string>>({})
const pendingActions = ref(new Set<string>())
const actionStatus = ref<Record<string, string>>({})
const feedbackOptions: Array<{ kind: AdvisorFeedbackKind; label: string }> = [
  { kind: 'Helpful', label: 'Helpful' },
  { kind: 'Irrelevant', label: 'Irrelevant' },
  { kind: 'AlreadyOwned', label: 'Already owned' },
  { kind: 'NotInterested', label: 'Not interested' },
]

const messages = computed(() => state.value?.session.messages ?? [])
const configured = computed(() => state.value?.configured ?? false)
const configurationHint = computed(() => state.value?.configurationHint ?? '')
const configurationMessage = computed(() => auth.isAdmin
  ? configurationHint.value
  : 'The Collection Advisor is not configured. Contact an administrator to enable it.')
const canChat = computed(() => !loading.value && configured.value && !sending.value)
const canSend = computed(() => canChat.value && draft.value.trim().length > 0)

function apply(next: AdvisorChatState) {
  state.value = next
  scrollToBottom()
}

function scrollToBottom() {
  nextTick(() => {
    if (transcriptEl.value) transcriptEl.value.scrollTop = transcriptEl.value.scrollHeight
  })
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  loadFailed.value = false
  try {
    apply(await getAdvisorChat())
  } catch (error: unknown) {
    errorMessage.value = requestError(error, 'Unable to load the advisor conversation.')
    loadFailed.value = true
  } finally {
    loading.value = false
  }
}

async function startOver() {
  if (loading.value || sending.value) return
  loading.value = true
  errorMessage.value = ''
  retryMessage.value = ''
  try {
    apply(await startAdvisorSession())
    draft.value = ''
    nextTick(() => composerEl.value?.focus())
  } catch (error: unknown) {
    errorMessage.value = requestError(error, 'Unable to start a new conversation.')
  } finally {
    loading.value = false
  }
}

async function send(prefill?: string) {
  if (!state.value || sending.value || !configured.value) return
  const message = (prefill ?? draft.value).trim()
  if (!message) return

  sending.value = true
  errorMessage.value = ''
  retryMessage.value = ''
  if (!prefill) draft.value = ''
  scrollToBottom()
  try {
    apply(await sendAdvisorMessage(state.value.session.id, message))
  } catch (error: unknown) {
    errorMessage.value = requestError(error, 'The advisor could not complete that request.')
    retryMessage.value = message
    if (!prefill) draft.value = message
  } finally {
    sending.value = false
    nextTick(() => composerEl.value?.focus())
  }
}

function requestError(error: unknown, fallback: string): string {
  if (!axios.isAxiosError(error)) return fallback
  const serverError = error.response?.data?.error
  if (typeof serverError === 'string') return serverError
  if (error.response?.status === 429) return 'Too many advisor requests. Wait a minute, then try again.'
  if (!error.response) return 'The advisor API is unreachable. Check your connection and try again.'
  return fallback
}

function actionKey(messageId: number, card: AdvisorRecommendationCard): string {
  return `${messageId}|${card.provider}|${card.providerItemId}`
}

function actionPending(key: string): boolean {
  return pendingActions.value.has(key)
}

function feedbackNote(messageId: number, card: AdvisorRecommendationCard): string {
  const key = actionKey(messageId, card)
  return feedbackDrafts.value[key] ?? card.feedback?.notes ?? ''
}

function setFeedbackNote(messageId: number, card: AdvisorRecommendationCard, value: string) {
  feedbackDrafts.value[actionKey(messageId, card)] = value
}

async function setFeedback(
  messageId: number,
  card: AdvisorRecommendationCard,
  kind: AdvisorFeedbackKind,
) {
  if (!card.provider || !card.providerItemId) return
  const key = actionKey(messageId, card)
  pendingActions.value.add(key)
  actionStatus.value[key] = ''
  try {
    card.feedback = await saveAdvisorFeedback(
      messageId,
      card.provider,
      card.providerItemId,
      kind,
      feedbackNote(messageId, card),
    )
    feedbackDrafts.value[key] = card.feedback.notes ?? ''
    actionStatus.value[key] = 'Feedback saved for future recommendations.'
  } catch (error: unknown) {
    actionStatus.value[key] = requestError(error, 'Unable to save feedback.')
  } finally {
    pendingActions.value.delete(key)
  }
}

async function clearFeedback(messageId: number, card: AdvisorRecommendationCard) {
  if (!card.feedback) return
  const feedbackId = card.feedback.id
  const key = actionKey(messageId, card)
  pendingActions.value.add(key)
  try {
    await removeAdvisorFeedback(feedbackId)
    card.feedback = null
    feedbackDrafts.value[key] = ''
    actionStatus.value[key] = 'Feedback cleared.'
  } catch (error: unknown) {
    actionStatus.value[key] = requestError(error, 'Unable to clear feedback.')
  } finally {
    pendingActions.value.delete(key)
  }
}

async function addToWishlist(messageId: number, card: AdvisorRecommendationCard) {
  if (!card.provider || !card.providerItemId) return
  const key = actionKey(messageId, card)
  pendingActions.value.add(key)
  actionStatus.value[key] = ''
  try {
    const result = await addAdvisorRecommendationToWishlist(
      messageId,
      card.provider,
      card.providerItemId,
    )
    actionStatus.value[key] = result.message
  } catch (error: unknown) {
    actionStatus.value[key] = requestError(error, 'Unable to add this recommendation to your wishlist.')
  } finally {
    pendingActions.value.delete(key)
  }
}

function renderMarkdown(text: string): string {
  const raw = marked.parse(text, { async: false }) as string
  const document = new DOMParser().parseFromString(raw, 'text/html')
  const allowedTags = new Set([
    'A', 'BLOCKQUOTE', 'BR', 'CODE', 'EM', 'H1', 'H2', 'H3', 'H4',
    'HR', 'LI', 'OL', 'P', 'PRE', 'STRONG', 'UL',
  ])

  for (const element of Array.from(document.body.querySelectorAll('*'))) {
    if (!allowedTags.has(element.tagName)) {
      element.replaceWith(document.createTextNode(element.textContent ?? ''))
      continue
    }

    const href = element.tagName === 'A' ? element.getAttribute('href') : null
    for (const attribute of Array.from(element.attributes)) {
      element.removeAttribute(attribute.name)
    }
    if (element.tagName === 'A' && safeExternalUrl(href)) {
      element.setAttribute('href', href!)
      element.setAttribute('target', '_blank')
      element.setAttribute('rel', 'noopener noreferrer')
    }
  }

  return document.body.innerHTML
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
  return formatInstant(value, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }) || 'at an unknown time'
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

function isStale(value?: string | null): boolean {
  if (!value) return true
  const observedAt = new Date(value).getTime()
  return Number.isNaN(observedAt) || Date.now() - observedAt > 24 * 60 * 60 * 1000
}

function toolLabel(tool: string): string {
  return tool.replaceAll('_', ' ')
}

function toolStatusLabel(status: AdvisorToolActivity['status']): string {
  return {
    completed: 'checked',
    completed_with_warnings: 'checked with provider warnings',
    unavailable: 'provider unavailable',
    failed: 'failed',
  }[status]
}

function toolStatusClass(status: AdvisorToolActivity['status']): string {
  return status === 'completed'
    ? 'text-text-muted'
    : status === 'completed_with_warnings'
    ? 'text-accent'
      : 'text-danger'
}

onMounted(load)
</script>

<style scoped>
.advisor-shell {
  display: flex;
  flex: 1;
  min-height: 36rem;
  max-height: calc(100dvh - 12rem);
  flex-direction: column;
  overflow: hidden;
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  background:
    radial-gradient(circle at 12% 5%, color-mix(in srgb, var(--color-accent) 8%, transparent), transparent 28%),
    var(--color-bg-card);
}

.user-message {
  border-radius: 1rem 1rem 0.25rem 1rem;
  background: var(--color-accent);
  color: var(--color-bg);
  padding: 0.75rem 1rem;
}

.assistant-message {
  border: 1px solid var(--color-border);
  border-radius: 1rem 1rem 1rem 0.25rem;
  background: var(--color-bg-surface);
  color: var(--color-text);
  padding: 0.875rem 1rem;
}

@media (max-width: 1023px) {
  .advisor-shell {
    min-height: calc(100dvh - 13rem);
    max-height: none;
  }
}
</style>
