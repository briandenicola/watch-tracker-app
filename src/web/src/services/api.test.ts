import { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const auth = vi.hoisted(() => ({
  token: 'expired-token' as string | null,
  refresh: vi.fn(),
}))
const router = vi.hoisted(() => ({ push: vi.fn() }))

vi.mock('@/stores/auth', () => ({
  useAuthStore: () => auth,
}))
vi.mock('@/router', () => ({
  default: router,
}))

function unauthorized(config: InternalAxiosRequestConfig) {
  const error = new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config)
  error.response = {
    config,
    data: {},
    headers: {},
    status: 401,
    statusText: 'Unauthorized',
  }
  return error
}

describe('API token refresh queue', () => {
  beforeEach(() => {
    vi.resetModules()
    vi.clearAllMocks()
    auth.token = 'expired-token'
  })

  it('retries concurrent unauthorized requests after one successful refresh', async () => {
    let completeRefresh!: (success: boolean) => void
    auth.refresh.mockImplementation(() => new Promise<boolean>((resolve) => {
      completeRefresh = (success) => {
        auth.token = success ? 'fresh-token' : null
        resolve(success)
      }
    }))

    const { api } = await import('./api')
    const attempts: Array<{ retry: boolean; authorization?: string }> = []
    api.defaults.adapter = async (config) => {
      const authorization = config.headers.Authorization
      attempts.push({
        retry: Boolean((config as InternalAxiosRequestConfig & { _retry?: boolean })._retry),
        authorization: typeof authorization === 'string' ? authorization : undefined,
      })
      if (!(config as InternalAxiosRequestConfig & { _retry?: boolean })._retry)
        return Promise.reject(unauthorized(config))

      return {
        config,
        data: { ok: true },
        headers: {},
        status: 200,
        statusText: 'OK',
      }
    }

    const first = api.get('/api/first')
    const second = api.get('/api/second')
    await vi.waitFor(() => expect(auth.refresh).toHaveBeenCalledTimes(1))
    completeRefresh(true)

    await expect(Promise.all([first, second])).resolves.toHaveLength(2)
    expect(attempts.filter(attempt => attempt.retry)).toEqual([
      { retry: true, authorization: 'Bearer fresh-token' },
      { retry: true, authorization: 'Bearer fresh-token' },
    ])
  })
})
