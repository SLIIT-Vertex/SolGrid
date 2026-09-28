import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiClient } from '@/lib/apiClient'
import { activateProsumer, deactivateProsumer, reactivateProsumer, updateProsumer, getProsumerActivity } from './api'

vi.mock('@/lib/apiClient', () => ({ apiClient: { patch: vi.fn(), put: vi.fn(), get: vi.fn() } }))

beforeEach(() => vi.resetAllMocks())

describe('prosumer mutation contracts', () => {
  it.each([
    ['activate', activateProsumer], ['deactivate', deactivateProsumer], ['reactivate', reactivateProsumer],
  ] as const)('sends the reviewed version and reason when performing %s', async (action, mutate) => {
    await mutate({ nic: '199012345678', expectedVersion: 7, reason: 'Reviewed request' })
    expect(apiClient.patch).toHaveBeenCalledWith(`/api/v1/prosumers/199012345678/${action}`, {
      expectedVersion: 7, reason: 'Reviewed request',
    })
  })

  it('preserves a zero version for legacy profiles and does not silently retry a conflict', async () => {
    const conflict = new Error('Account changed')
    vi.mocked(apiClient.put).mockRejectedValue(conflict)
    await expect(updateProsumer('199012345678', {
      firstName: 'Test', lastName: 'Owner', email: 'test@example.com', phoneNumber: null, expectedVersion: 0,
    })).rejects.toBe(conflict)
    expect(apiClient.put).toHaveBeenCalledOnce()
    expect(apiClient.put).toHaveBeenCalledWith('/api/v1/prosumers/199012345678', expect.objectContaining({ expectedVersion: 0 }))
  })

  it('loads the selected history page through the authenticated API client', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: { items: [], totalCount: 0, pageNumber: 2, pageSize: 10 } })
    expect((await getProsumerActivity('199012345678', 2)).pageNumber).toBe(2)
    expect(apiClient.get).toHaveBeenCalledWith('/api/v1/prosumers/199012345678/activity', {
      params: { pageNumber: 2, pageSize: 10 },
    })
  })
})
