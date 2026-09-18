import { describe, expect, it, vi } from 'vitest'
import { allPages } from './pagination'

describe('allPages', () => {
  it('collects pages in order and stops once the reported total is reached', async () => {
    const load = vi.fn(async (page: number) => ({
      items: page === 1 ? ['first', 'second'] : ['third'],
      totalCount: 3,
    }))

    await expect(allPages(load)).resolves.toEqual(['first', 'second', 'third'])
    expect(load.mock.calls).toEqual([[1], [2]])
  })

  it('stops on an empty page even when the reported total is outdated', async () => {
    const load = vi.fn(async (page: number) => ({
      items: page === 1 ? ['remaining'] : [],
      totalCount: 100,
    }))

    await expect(allPages(load)).resolves.toEqual(['remaining'])
    expect(load.mock.calls).toEqual([[1], [2]])
  })

  it('propagates a failed page rather than returning incomplete options', async () => {
    const error = new Error('Station options unavailable')
    const load = vi.fn(async (page: number) => {
      if (page === 2) throw error
      return { items: ['first'], totalCount: 2 }
    })

    await expect(allPages(load)).rejects.toBe(error)
  })
})
