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
      button.text() === 'Test Connection')
    expect(testButtons).toHaveLength(1)
    await testButtons[0].trigger('click')
    await flushPromises()

    expect(api.post).toHaveBeenCalledWith(
      '/api/admin/ollama/models',
      { url: 'http://ollama.test:11434' },
    )
    expect(wrapper.text()).toContain('Connected — 1 model(s) available')
  })
})
