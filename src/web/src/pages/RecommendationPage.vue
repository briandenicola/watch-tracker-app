<template>
  <div class="max-w-5xl mx-auto">
    <div class="mb-6">
      <p class="text-xs uppercase tracking-[0.24em] text-accent mb-2">AI stylist</p>
      <h2 class="font-display text-3xl font-semibold text-text">What should I wear?</h2>
      <p class="text-sm text-text-muted mt-2">
        Build your outfit and get two ranked watch recommendations from your collection.
      </p>
    </div>

    <div class="grid lg:grid-cols-[minmax(0,1fr)_minmax(20rem,0.85fr)] gap-6 items-start">
      <form class="bg-bg-card border border-border rounded-2xl p-5 sm:p-6 space-y-5" @submit.prevent="getRecommendation">
        <fieldset>
          <legend class="block text-sm font-medium text-text mb-2">Occasion</legend>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="option in occasions"
              :key="option"
              type="button"
              class="px-3 py-2 border rounded-lg text-sm transition-colors"
              :class="form.occasion === option
                ? 'bg-accent border-accent text-bg'
                : 'bg-bg-surface border-border text-text-secondary hover:border-accent/50'"
              @click="form.occasion = option"
            >
              {{ option }}
            </button>
          </div>
          <input
            v-if="form.occasion === 'Other'"
            v-model.trim="customOccasion"
            maxlength="100"
            required
            placeholder="Describe the occasion"
            class="field mt-3"
          />
        </fieldset>

        <div>
          <label for="outfit" class="block text-sm font-medium text-text mb-2">Your outfit</label>
          <textarea
            id="outfit"
            v-model.trim="form.outfitDescription"
            required
            maxlength="1500"
            rows="5"
            placeholder="For example: navy linen blazer, white crew-neck tee, stone chinos, and brown suede loafers"
            class="field resize-y"
          />
          <p class="text-xs text-text-muted mt-1">Include clothing, shoes, and accessories.</p>
        </div>

        <div class="grid sm:grid-cols-2 gap-4">
          <div>
            <label for="colors" class="block text-sm font-medium text-text mb-2">Color palette</label>
            <input id="colors" v-model.trim="form.colorPalette" maxlength="200" placeholder="Navy, white, tan" class="field" />
          </div>
          <div>
            <label for="weather" class="block text-sm font-medium text-text mb-2">Weather</label>
            <input id="weather" v-model.trim="form.weather" maxlength="200" placeholder="Warm and sunny" class="field" />
          </div>
        </div>

        <div>
          <label for="preferences" class="block text-sm font-medium text-text mb-2">Preferences</label>
          <input
            id="preferences"
            v-model.trim="form.preferences"
            maxlength="500"
            placeholder="Optional: understated, bracelet only, avoid oversized watches..."
            class="field"
          />
        </div>

        <p v-if="errorMessage" role="alert" class="text-sm text-danger">{{ errorMessage }}</p>

        <button
          type="submit"
          :disabled="loading || !canSubmit"
          class="w-full px-5 py-3 bg-accent hover:bg-accent-hover text-bg font-semibold rounded-xl transition-colors disabled:opacity-50"
        >
          {{ loading ? 'Consulting your collection...' : 'Recommend Watches' }}
        </button>
      </form>

      <section class="recommendation-panel">
        <div v-if="loading" class="flex flex-col items-center justify-center min-h-[26rem] text-center">
          <div class="w-10 h-10 border-2 border-accent border-t-transparent rounded-full animate-spin mb-4" />
          <p class="font-display text-lg text-text">Considering every detail</p>
          <p class="text-sm text-text-muted mt-1">Matching your outfit to your collection.</p>
        </div>

        <div v-else-if="recommendation" class="space-y-6">
          <article
            v-for="(pick, index) in rankedRecommendations"
            :key="pick.watchId"
            :class="{ 'pt-6 border-t border-border': index > 0 }"
          >
            <p
              class="text-xs uppercase tracking-[0.22em] mb-3"
              :class="index === 0 ? 'text-accent' : 'text-text-muted'"
            >
              {{ index === 0 ? 'Primary recommendation' : 'Secondary recommendation' }}
            </p>
            <RouterLink :to="`/watches/${pick.watchId}`" class="group grid grid-cols-[6rem_1fr] gap-4 items-center">
              <div class="aspect-square rounded-xl bg-bg-surface overflow-hidden">
                <img
                  v-if="pick.imageUrl"
                  :src="imageUrl(pick.imageUrl)"
                  :alt="`${pick.brand} ${pick.model}`"
                  class="w-full h-full object-contain p-2 group-hover:scale-105 transition-transform duration-300"
                />
                <div v-else class="w-full h-full flex items-center justify-center text-4xl text-text-muted">⌚</div>
              </div>
              <div>
                <h3 class="font-display text-xl font-semibold text-text group-hover:text-accent transition-colors">
                  {{ pick.brand }}
                </h3>
                <p class="text-sm text-text-secondary">{{ pick.model }}</p>
              </div>
            </RouterLink>
            <div class="mt-4">
              <h4 class="text-xs uppercase tracking-wider text-text-muted mb-1">Why this watch</h4>
              <p class="text-sm leading-6 text-text">{{ pick.reason }}</p>
            </div>
            <div v-if="pick.stylingTips.length" class="mt-4">
              <h4 class="text-xs uppercase tracking-wider text-text-muted mb-2">Styling notes</h4>
              <ul class="space-y-2">
                <li v-for="tip in pick.stylingTips" :key="tip" class="flex gap-2 text-sm text-text-secondary">
                  <span class="text-accent">✦</span>
                  <span>{{ tip }}</span>
                </li>
              </ul>
            </div>
          </article>
        </div>

        <div v-else class="flex flex-col items-center justify-center min-h-[26rem] text-center px-5">
          <AppIcon name="recommend" :size="56" :stroke-width="1" class="text-accent mb-5" />
          <h3 class="font-display text-xl font-semibold text-text">Your collection, styled</h3>
          <p class="text-sm text-text-muted mt-2 max-w-xs">
            Tell us what you are wearing and your AI stylist will rank two finishing touches.
          </p>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import axios from 'axios'
import type { WatchRecommendation, WatchRecommendationRequest } from '@/types'
import { imageUrl } from '@/services/watches'
import { recommendWatch } from '@/services/recommendations'
import AppIcon from '@/components/icons/AppIcon.vue'

const occasions = ['Everyday', 'Work', 'Date night', 'Formal event', 'Travel', 'Outdoor', 'Other']
const customOccasion = ref('')
const loading = ref(false)
const errorMessage = ref('')
const recommendation = ref<WatchRecommendation | null>(null)
const rankedRecommendations = computed(() =>
  recommendation.value
    ? [recommendation.value.primary, recommendation.value.secondary]
    : []
)
const form = reactive<WatchRecommendationRequest>({
  occasion: 'Everyday',
  outfitDescription: '',
  colorPalette: '',
  weather: '',
  preferences: '',
})

const canSubmit = computed(() =>
  form.outfitDescription.length > 0
  && (form.occasion !== 'Other' || customOccasion.value.length > 0)
)

async function getRecommendation() {
  if (!canSubmit.value) return

  loading.value = true
  errorMessage.value = ''
  recommendation.value = null
  try {
    recommendation.value = await recommendWatch({
      ...form,
      occasion: form.occasion === 'Other' ? customOccasion.value : form.occasion,
    })
  } catch (error: unknown) {
    if (axios.isAxiosError(error)) {
      errorMessage.value = error.response?.data?.error
        || (error.response?.status === 429
          ? 'Too many recommendations requested. Please wait a moment.'
          : 'Unable to create a recommendation right now.')
    } else {
      errorMessage.value = 'Unable to create a recommendation right now.'
    }
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.field {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-bg);
  color: var(--color-text);
  font-size: 0.875rem;
  outline: none;
  transition: border-color 0.2s ease;
}

.field:focus {
  border-color: var(--color-accent);
}

.recommendation-panel {
  padding: 1.25rem;
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  background:
    radial-gradient(circle at 50% 15%, color-mix(in srgb, var(--color-accent) 10%, transparent), transparent 45%),
    var(--color-bg-card);
}

@media (min-width: 640px) {
  .recommendation-panel {
    padding: 1.5rem;
  }
}
</style>
