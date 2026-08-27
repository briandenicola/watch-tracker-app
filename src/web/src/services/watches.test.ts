import { beforeEach, describe, expect, it, vi } from 'vitest'

const api = vi.hoisted(() => ({ put: vi.fn() }))

vi.mock('./api', () => ({ api }))

describe('watch notification API', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    api.put.mockResolvedValue(undefined)
  })

  it('marks every owned price alert as read in one request', async () => {
    const { markAllPriceAlertsRead } = await import('./watches')

    await markAllPriceAlertsRead()

    expect(api.put).toHaveBeenCalledWith('/api/watches/price-alerts/read-all')
  })
})
