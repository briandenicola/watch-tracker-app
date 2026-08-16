<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-[80] flex items-end sm:items-center justify-center bg-black/60 p-0 sm:p-4"
      @click.self="emit('close')"
    >
      <div
        ref="dialogEl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="style-agent-title"
        tabindex="-1"
        class="flex w-full sm:max-w-2xl h-[92vh] sm:h-[85vh] flex-col overflow-hidden bg-bg-card border border-border rounded-t-2xl sm:rounded-2xl shadow-xl focus:outline-none"
        @keydown.esc="emit('close')"
      >
        <!-- Header -->
        <div class="flex items-start justify-between gap-3 border-b border-border px-5 py-4">
          <div class="min-w-0">
            <p class="text-[0.65rem] uppercase tracking-[0.24em] text-accent mb-1">Style Agent</p>
            <h2 id="style-agent-title" class="font-display text-xl font-semibold text-text truncate">
              {{ watchName }}
            </h2>
          </div>
          <div class="flex flex-shrink-0 items-center gap-2">
            <button
              type="button"
              class="chip"
              :disabled="sending || loading"
              title="Clear the conversation — remembered outfits are kept"
              @click="startOver"
            >
              New chat
            </button>
            <button
              type="button"
              class="w-11 h-11 inline-flex items-center justify-center text-text-muted hover:text-text"
              aria-label="Close style agent"
              @click="emit('close')"
            >
              <AppIcon name="close" :size="20" :stroke-width="2" />
            </button>
          </div>
        </div>

        <!-- Guidance the agent asks for up front -->
        <div class="border-b border-border px-5 py-3 space-y-2">
          <div class="grid gap-2 sm:grid-cols-2">
            <div>
              <label for="style-occasion" class="guidance-label">Occasion</label>
              <input
                id="style-occasion"
                v-model="occasion"
                type="text"
                maxlength="200"
                placeholder="Dinner with friends"
                class="guidance-input"
                :disabled="!canChat"
              />
              <div class="mt-1.5 flex flex-wrap gap-1.5">
                <button
                  v-for="preset in OCCASION_PRESETS"
                  :key="preset"
                  type="button"
                  class="chip"
                  :class="{ 'chip-on': occasion === preset }"
                  :disabled="!canChat"
                  @click="occasion = preset"
                >
                  {{ preset }}
                </button>
              </div>
            </div>
            <div>
              <label for="style-weather" class="guidance-label">Weather</label>
              <input
                id="style-weather"
                v-model="weather"
                type="text"
                maxlength="200"
                placeholder="12°C and drizzling"
                class="guidance-input"
                :disabled="!canChat"
              />
              <div class="mt-1.5 flex flex-wrap gap-1.5">
                <button
                  v-for="preset in WEATHER_PRESETS"
                  :key="preset"
                  type="button"
                  class="chip"
                  :class="{ 'chip-on': weather === preset }"
                  :disabled="!canChat"
                  @click="weather = preset"
                >
                  {{ preset }}
                </button>
              </div>
            </div>
          </div>

          <button
            v-if="memory.length"
            type="button"
            class="flex w-full items-center justify-between text-xs text-text-secondary hover:text-accent"
            @click="showMemory = !showMemory"
          >
            <span>
              Remembered outfits ({{ memory.length }})
              <span v-if="pendingFeedback" class="text-accent">· {{ pendingFeedback }} awaiting your verdict</span>
            </span>
            <span>{{ showMemory ? 'Hide' : 'Show' }}</span>
          </button>

          <div v-if="showMemory && memory.length" class="max-h-56 overflow-y-auto space-y-2 pt-1">
            <div v-for="item in memory" :key="item.id" class="rounded-lg border border-border bg-bg-surface p-3">
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0">
                  <p class="text-sm text-text">{{ item.summary }}</p>
                  <p class="text-[0.7rem] text-text-muted mt-0.5">
                    {{ new Date(item.createdAt).toLocaleDateString() }}
                    <template v-if="item.occasion"> · {{ item.occasion }}</template>
                    <template v-if="item.weather"> · {{ item.weather }}</template>
                  </p>
                </div>
                <button type="button" class="text-xs text-danger hover:underline flex-shrink-0" @click="forget(item)">
                  Forget
                </button>
              </div>
              <div class="mt-2 flex flex-wrap items-center gap-1.5">
                <button
                  type="button"
                  class="chip"
                  :class="{ 'chip-on': item.wasHelpful === true }"
                  :disabled="savingFeedback === item.id"
                  @click="rate(item, true)"
                >
                  Worked
                </button>
                <button
                  type="button"
                  class="chip"
                  :class="{ 'chip-on': item.wasHelpful === false }"
                  :disabled="savingFeedback === item.id"
                  @click="rate(item, false)"
                >
                  Missed
                </button>
                <span v-if="!isRated(item)" class="text-[0.7rem] text-text-muted">Not rated yet</span>
              </div>
              <div v-if="isRated(item)" class="mt-2 flex gap-2">
                <input
                  v-model="noteDrafts[item.id]"
                  type="text"
                  maxlength="1000"
                  placeholder="What worked, what didn't"
                  class="guidance-input flex-1"
                  @keydown.enter.prevent="saveNote(item)"
                />
                <button type="button" class="chip" :disabled="savingFeedback === item.id" @click="saveNote(item)">
                  Save
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Transcript -->
        <div ref="transcriptEl" class="flex-1 overflow-y-auto px-5 py-4 space-y-3">
          <div v-if="loading" class="flex justify-center py-10">
            <div class="w-6 h-6 border-2 border-accent border-t-transparent rounded-full animate-spin" />
          </div>

          <template v-else>
            <div v-if="!messages.length" class="bubble bubble-agent prose-markdown text-sm">
              <p>
                I'll build an outfit around your <strong>{{ watchName }}</strong>.
              </p>
              <p v-if="hasPhoto">
                Tell me the <strong>occasion</strong> and the <strong>weather</strong> — the chips above are the quick
                way — and I'll work from your photo of it, dial and strap colours included.
              </p>
              <p v-else>
                Tell me the <strong>occasion</strong> and the <strong>weather</strong> — the chips above are the quick
                way — and I'll work from its recorded details. Add a photo of this watch and I can style to its actual
                colours instead.
              </p>
              <p v-if="pendingFeedback">
                I also still owe you a question: {{ pendingFeedback }}
                {{ pendingFeedback === 1 ? 'earlier outfit has' : 'earlier outfits have' }} no verdict yet. Marking them
                Worked or Missed teaches me what to suggest next.
              </p>
            </div>

            <div
              v-for="message in messages"
              :key="message.id"
              class="flex"
              :class="message.role === 'User' ? 'justify-end' : 'justify-start'"
            >
              <div class="bubble" :class="message.role === 'User' ? 'bubble-user' : 'bubble-agent'">
                <div
                  v-if="message.role === 'Assistant'"
                  class="prose-markdown text-sm text-text"
                  v-html="renderMarkdown(message.content)"
                />
                <p v-else class="text-sm whitespace-pre-wrap">{{ message.content }}</p>

                <div
                  v-if="message.recommendation"
                  class="mt-3 flex flex-wrap items-center gap-1.5 border-t border-border pt-2"
                >
                  <span class="text-[0.7rem] text-text-muted">Did this one work?</span>
                  <button
                    type="button"
                    class="chip"
                    :class="{ 'chip-on': message.recommendation?.wasHelpful === true }"
                    :disabled="savingFeedback === message.recommendation?.id"
                    @click="rate(message.recommendation, true)"
                  >
                    Worked
                  </button>
                  <button
                    type="button"
                    class="chip"
                    :class="{ 'chip-on': message.recommendation?.wasHelpful === false }"
                    :disabled="savingFeedback === message.recommendation?.id"
                    @click="rate(message.recommendation, false)"
                  >
                    Missed
                  </button>
                  <span v-if="message.recommendation?.feedbackAt" class="text-[0.7rem] text-text-muted">Saved</span>
                </div>
              </div>
            </div>

            <div v-if="sending" class="flex justify-start">
              <div class="bubble bubble-agent text-sm text-text-muted">Putting a look together…</div>
            </div>
          </template>
        </div>

        <!-- Composer -->
        <div class="border-t border-border px-5 py-3 space-y-2">
          <p v-if="!loading && !configured" class="text-xs text-danger">{{ configurationHint }}</p>
          <p v-if="error" class="text-xs text-danger">{{ error }}</p>

          <div v-if="followUps.length" class="flex flex-wrap gap-1.5">
            <button
              v-for="followUp in followUps"
              :key="followUp"
              type="button"
              class="chip"
              :disabled="sending"
              @click="send(followUp)"
            >
              {{ followUp }}
            </button>
          </div>

          <div class="flex items-end gap-2">
            <textarea
              v-model="draft"
              rows="2"
              maxlength="2000"
              :disabled="!canChat"
              placeholder="Ask for a look, or answer the agent's question…"
              class="guidance-input flex-1 resize-none"
              @keydown.enter.exact.prevent="send()"
            />
            <button
              type="button"
              class="px-4 py-2.5 bg-accent hover:bg-accent-hover text-bg text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
              :disabled="!canSend"
              @click="send()"
            >
              {{ sending ? 'Styling…' : 'Send' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { marked } from 'marked'
import type { SendStyleMessage, StyleChatState, StyleRecommendation } from '@/types'
import AppIcon from '@/components/icons/AppIcon.vue'
import {
  forgetStyleRecommendation, getStyleChat, rateStyleRecommendation, sendStyleMessage, startStyleSession,
} from '@/services/style'

const props = defineProps<{ watchId: number, watchName: string, hasPhoto: boolean }>()
const emit = defineEmits<{ close: [] }>()

const OCCASION_PRESETS = ['Work', 'Casual weekend', 'Dinner out', 'Wedding', 'Travel day', 'Outdoors']
const WEATHER_PRESETS = ['Hot', 'Warm and dry', 'Mild', 'Cool and crisp', 'Cold', 'Rainy']

const state = ref<StyleChatState | null>(null)
const loading = ref(true)
const sending = ref(false)
const error = ref('')
const draft = ref('')
const occasion = ref('')
const weather = ref('')
const showMemory = ref(false)
const savingFeedback = ref<number | null>(null)
const noteDrafts = ref<Record<number, string>>({})
const transcriptEl = ref<HTMLElement | null>(null)
const dialogEl = ref<HTMLElement | null>(null)

const messages = computed(() => state.value?.session.messages ?? [])
const memory = computed(() => state.value?.memory ?? [])
const followUps = computed(() => state.value?.followUps ?? [])
const configured = computed(() => state.value?.configured ?? false)
const configurationHint = computed(() => state.value?.configurationHint ?? '')
const pendingFeedback = computed(() => memory.value.filter(item => !isRated(item)).length)

/** The API sends null until the user has said whether an outfit worked out. */
function isRated(recommendation?: StyleRecommendation | null): boolean {
  return recommendation?.wasHelpful === true || recommendation?.wasHelpful === false
}
const canChat = computed(() => !loading.value && configured.value)

// Guidance already on the session is context the agent still has, so only a
// change to it is worth sending again.
const guidanceChanged = computed(() => {
  const session = state.value?.session
  const nextOccasion = occasion.value.trim()
  const nextWeather = weather.value.trim()
  return (!!nextOccasion && nextOccasion !== (session?.occasion ?? ''))
    || (!!nextWeather && nextWeather !== (session?.weather ?? ''))
})

const canSend = computed(() => canChat.value && !sending.value && (!!draft.value.trim() || guidanceChanged.value))

function renderMarkdown(text: string): string {
  return marked.parse(text, { async: false }) as string
}

function apply(next: StyleChatState) {
  state.value = next
  occasion.value = next.session.occasion ?? ''
  weather.value = next.session.weather ?? ''
  for (const item of next.memory) {
    if (noteDrafts.value[item.id] === undefined) noteDrafts.value[item.id] = item.feedbackNotes ?? ''
  }
  scrollToBottom()
}

function scrollToBottom() {
  nextTick(() => {
    const el = transcriptEl.value
    if (el) el.scrollTop = el.scrollHeight
  })
}

function serverMessage(err: unknown): string | undefined {
  const data = (err as { response?: { data?: Record<string, unknown> } })?.response?.data
  return typeof data?.error === 'string' ? data.error : undefined
}

onMounted(async () => {
  dialogEl.value?.focus()
  try {
    apply(await getStyleChat(props.watchId))
  } catch (err) {
    error.value = serverMessage(err) || 'Could not open the style agent.'
  } finally {
    loading.value = false
  }
})

async function send(text?: string) {
  const message = (text ?? draft.value).trim()
  const session = state.value?.session
  const nextOccasion = occasion.value.trim()
  const nextWeather = weather.value.trim()

  const payload: SendStyleMessage = {}
  if (message) payload.message = message
  if (nextOccasion && nextOccasion !== (session?.occasion ?? '')) payload.occasion = nextOccasion
  if (nextWeather && nextWeather !== (session?.weather ?? '')) payload.weather = nextWeather
  if (!payload.message && !payload.occasion && !payload.weather) return

  sending.value = true
  error.value = ''
  scrollToBottom()
  try {
    const next = await sendStyleMessage(props.watchId, payload)
    // The turn is only stored once the agent answers, so the box is cleared here.
    if (!text) draft.value = ''
    apply(next)
  } catch (err) {
    error.value = serverMessage(err) || 'The style agent could not answer just now.'
  } finally {
    sending.value = false
  }
}

async function startOver() {
  if (!state.value || sending.value) return
  if (messages.value.length && !confirm('Clear this conversation? Remembered outfits are kept.')) return
  error.value = ''
  try {
    apply(await startStyleSession(props.watchId))
    draft.value = ''
  } catch (err) {
    error.value = serverMessage(err) || 'Could not start a new conversation.'
  }
}

function applyRecommendation(updated: StyleRecommendation) {
  const current = state.value
  if (!current) return
  current.memory = current.memory.map(item => (item.id === updated.id ? updated : item))
  current.session.messages = current.session.messages.map(message =>
    message.recommendation?.id === updated.id ? { ...message, recommendation: updated } : message,
  )
}

async function rate(recommendation: StyleRecommendation | null | undefined, wasHelpful: boolean) {
  if (!recommendation) return
  savingFeedback.value = recommendation.id
  error.value = ''
  try {
    applyRecommendation(await rateStyleRecommendation(
      props.watchId,
      recommendation.id,
      wasHelpful,
      noteDrafts.value[recommendation.id] || undefined,
    ))
  } catch (err) {
    error.value = serverMessage(err) || 'Could not save that feedback.'
  } finally {
    savingFeedback.value = null
  }
}

async function saveNote(recommendation: StyleRecommendation) {
  // The API takes a verdict and its note together, so the note rides along with
  // the rating the user already gave.
  if (recommendation.wasHelpful !== true && recommendation.wasHelpful !== false) return
  await rate(recommendation, recommendation.wasHelpful)
}

async function forget(recommendation: StyleRecommendation) {
  const current = state.value
  if (!current || !confirm('Forget this outfit? The agent will stop taking it into account.')) return
  error.value = ''
  try {
    await forgetStyleRecommendation(props.watchId, recommendation.id)
    current.memory = current.memory.filter(item => item.id !== recommendation.id)
    current.session.messages = current.session.messages.map(message =>
      message.recommendation?.id === recommendation.id ? { ...message, recommendation: null } : message,
    )
  } catch (err) {
    error.value = serverMessage(err) || 'Could not forget that outfit.'
  }
}
</script>

<style scoped>
.guidance-label {
  display: block;
  margin-bottom: 0.25rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
  font-weight: 500;
}

.guidance-input {
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-bg-surface);
  color: var(--color-text);
  font-size: 0.875rem;
}

.guidance-input:focus {
  border-color: var(--color-accent);
  outline: none;
}

.guidance-input:disabled {
  opacity: 0.55;
}

.chip {
  padding: 0.25rem 0.6rem;
  border: 1px solid var(--color-border);
  border-radius: 9999px;
  background: var(--color-bg-surface);
  color: var(--color-text-secondary);
  font-size: 0.7rem;
  transition: border-color 0.15s ease, color 0.15s ease;
}

.chip:hover:not(:disabled) {
  border-color: color-mix(in srgb, var(--color-accent) 50%, transparent);
  color: var(--color-accent);
}

.chip:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.chip-on {
  border-color: var(--color-accent);
  color: var(--color-accent);
}

.bubble {
  max-width: 85%;
  padding: 0.75rem 0.9rem;
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  overflow-wrap: anywhere;
}

.bubble-agent {
  background: var(--color-bg-surface);
  border-bottom-left-radius: 0.35rem;
  color: var(--color-text);
}

.bubble-user {
  background: color-mix(in srgb, var(--color-accent) 14%, var(--color-bg-surface));
  border-color: color-mix(in srgb, var(--color-accent) 35%, transparent);
  border-bottom-right-radius: 0.35rem;
  color: var(--color-text);
}
</style>
