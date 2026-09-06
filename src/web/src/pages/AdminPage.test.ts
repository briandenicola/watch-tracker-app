import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AdminPage from './AdminPage.vue'

const api = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
}))

vi.mock('@/services/api', () => ({ api }))

describe('Admin Ollama settings', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    api.get.mockImplementation((url: string) => {
      if (url === '/api/admin/users') return Promise.resolve({ data: [] })
      if (url === '/api/admin/oidc/providers') return Promise.resolve({ data: [] })
      return Promise.resolve({
        data: [
          { key: 'AiAnalysisPrompt', value: 'Analyze the watch.' },
          { key: 'OllamaUrl', value: 'http://ollama.test:11434' },
          { key: 'OllamaModel', value: 'test-model' },
          { key: 'StyleAgentPrompt', value: 'Style a watch.' },
          { key: 'WatchRecommendationPrompt', value: 'Recommend a watch.' },
          { key: 'CollectionAdvisorPrompt', value: 'Advise the collector.' },
          { key: 'ResaleValuePrompt', value: 'Estimate resale value.' },
          { key: 'WebSearchProvider', value: 'SearXNG' },
          { key: 'MarketplaceVendor', value: 'Chrono24' },
          { key: 'SearXngUrl', value: 'http://search.test' },
          { key: 'EbayClientId', value: 'client-id' },
          { key: 'EbayClientSecret', value: 'secret' },
          { key: 'ResaleValueRefreshIntervalDays', value: '7' },
          { key: 'PriceAlertScanIntervalHours', value: '24' },
          { key: 'ApplicationTimeZone', value: 'America/Chicago' },
        ],
      })
    })
    api.post.mockResolvedValue({ data: ['test-model'] })
  })

  it('tests the single editable OllamaUrl without a duplicate connection panel', async () => {
    const wrapper = mount(AdminPage)
    await flushPromises()

    const ollamaInput = wrapper.findAll('input').find(input =>
      input.element.value === 'http://ollama.test:11434')
    expect(ollamaInput).toBeDefined()
    expect(wrapper.text()).not.toContain('Ollama Connection')

    const testButtons = wrapper.findAll('button').filter(button =>
      button.text() === 'Test')
    expect(testButtons).toHaveLength(1)
    await testButtons[0].trigger('click')
    await flushPromises()

    expect(api.post).toHaveBeenCalledWith(
      '/api/admin/ollama/models',
      { url: 'http://ollama.test:11434' },
    )
    expect(wrapper.text()).toContain('Connected — 1 model(s) available')
  })

  it('orders provider configuration before the unified prompts group', async () => {
    const wrapper = mount(AdminPage)
    await flushPromises()
    const text = wrapper.text()

    const ollama = text.indexOf('Ollama Configuration')
    const search = text.indexOf('Web Search Configuration')
    const ebay = text.indexOf('eBay Pricing')
    const resale = text.indexOf('Resale Configuration')
    const priceMonitoring = text.indexOf('Price Monitoring')
    const prompts = text.indexOf('Prompts')

    expect(ollama).toBeGreaterThan(-1)
    expect(search).toBeGreaterThan(ollama)
    expect(ebay).toBeGreaterThan(search)
    expect(resale).toBeGreaterThan(ebay)
    expect(priceMonitoring).toBeGreaterThan(resale)
    expect(prompts).toBeGreaterThan(priceMonitoring)

    const promptSettings = text.slice(prompts)
    const resalePrompt = promptSettings.indexOf('ResaleValuePrompt')
    const advisorPrompt = promptSettings.indexOf('CollectionAdvisorPrompt')
    const recommendationPrompt = promptSettings.indexOf('WatchRecommendationPrompt')
    const stylePrompt = promptSettings.indexOf('StyleAgentPrompt')
    const analysisPrompt = promptSettings.indexOf('AiAnalysisPrompt')

    expect(resalePrompt).toBeGreaterThan(-1)
    expect(advisorPrompt).toBeGreaterThan(resalePrompt)
    expect(recommendationPrompt).toBeGreaterThan(advisorPrompt)
    expect(stylePrompt).toBeGreaterThan(recommendationPrompt)
    expect(analysisPrompt).toBeGreaterThan(stylePrompt)
  })

  it('renders and saves the application timezone with the other settings', async () => {
    api.put.mockResolvedValue({ data: undefined })
    const wrapper = mount(AdminPage)
    await flushPromises()

    expect(wrapper.text()).toContain('Regional Settings')
    const timeZoneInput = wrapper.findAll('input').find(input =>
      input.element.value === 'America/Chicago')
    expect(timeZoneInput).toBeDefined()

    await wrapper.findAll('button').find(button => button.text() === 'Save Settings')!.trigger('click')
    await flushPromises()

    expect(api.put).toHaveBeenCalledWith(
      '/api/admin/settings',
      expect.arrayContaining([
        { key: 'ApplicationTimeZone', value: 'America/Chicago' },
      ]),
    )
  })
})
