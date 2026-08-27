<template>
  <template v-if="images.length">
    <div class="relative mb-6 overflow-hidden rounded-xl bg-bg-surface">
      <div class="flex h-[300px] items-center justify-center lg:h-[400px]">
        <img :src="imageUrl(selectedImage.url)" :alt="`${watch.brand} ${watch.model}`" class="max-h-full max-w-full object-contain" />
      </div>
      <div v-if="images.length > 1" class="absolute inset-x-0 bottom-3 flex justify-center gap-1.5">
        <button v-for="(_, index) in images" :key="index" class="h-2 w-2 rounded-full transition-colors" :class="index === imageIndex ? 'bg-accent' : 'bg-white/40'" @click="imageIndex = index" />
      </div>
    </div>
    <div class="mb-6 flex flex-wrap gap-2">
      <button @click="emit('remove-background', selectedImage.id)" :disabled="removingBackground" class="rounded-lg border border-border bg-bg-surface px-3 py-1.5 text-xs text-text-secondary transition-colors hover:border-accent/50 disabled:opacity-50">
        {{ removingBackground ? 'Removing…' : 'Remove Background' }}
      </button>
      <button @click="emit('delete-image', selectedImage.id)" class="rounded-lg border border-danger/50 bg-bg-surface px-3 py-1.5 text-xs text-danger transition-colors hover:bg-danger/10">
        Delete Image
      </button>
    </div>
  </template>
</template>

<script setup lang="ts">
import { computed, ref, watch as vueWatch } from 'vue'
import type { Watch, WatchImage } from '@/types'
import { imageUrl } from '@/services/watches'

const props = defineProps<{
  watch: Watch
  removingBackground: boolean
}>()

const emit = defineEmits<{
  'delete-image': [imageId: number]
  'remove-background': [imageId: number]
}>()

const imageIndex = ref(0)
const images = computed(() => props.watch.imageUrls)
const selectedImage = computed<WatchImage>(() => images.value[imageIndex.value])

vueWatch(images, (nextImages) => {
  if (imageIndex.value >= nextImages.length) imageIndex.value = Math.max(0, nextImages.length - 1)
})
</script>
