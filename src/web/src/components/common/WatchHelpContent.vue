<template>
  <div class="space-y-8">
    <section v-for="section in watchHelpSections" :key="section.title">
      <h2 class="font-display text-xl font-semibold text-text">{{ section.title }}</h2>
      <p class="mt-1 text-sm leading-6 text-text-muted">{{ section.description }}</p>
      <dl class="mt-3 divide-y divide-border overflow-hidden rounded-xl border border-border bg-bg-card">
        <div v-for="term in section.terms" :key="term.name" class="p-4 sm:grid sm:grid-cols-[10rem_1fr] sm:gap-5">
          <dt class="text-sm font-semibold text-text">{{ term.name }}</dt>
          <dd class="mt-1 text-sm leading-6 text-text-secondary sm:mt-0">
            {{ term.description }}
            <span v-if="term.example" class="mt-1 block text-xs text-text-muted">Examples: {{ term.example }}</span>
          </dd>
        </div>
      </dl>
    </section>
  </div>
</template>

<script setup lang="ts">
import { watchHelpSections } from '@/constants/watchHelp'
</script>
