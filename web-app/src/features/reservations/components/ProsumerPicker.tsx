import { useEffect, useId, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getProsumers } from '@/features/prosumers/api'
import { defaultProsumerFilters } from '@/features/prosumers/types'
import type { Prosumer } from '@/features/prosumers/types'
import { prosumersKeys } from '@/features/prosumers/queryKeys'
import { Button } from '@/components/common/Button'
import { ReservationIcon } from './ReservationIcon'
import { ProsumerIdentity } from './ProsumerIdentity'

export function ProsumerPicker({
  selected,
  onSelect,
}: {
  selected: Prosumer | null
  onSelect: (value: Prosumer | null) => void
}) {
  const [search, setSearch] = useState('')
  const [debounced, setDebounced] = useState('')
  const [expanded, setExpanded] = useState(true)
  const [active, setActive] = useState(-1)
  const id = useId()
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(search.trim()), 300)
    return () => clearTimeout(timer)
  }, [search])
  const filters = {
    ...defaultProsumerFilters,
    searchText: debounced,
    status: 'Active' as const,
    pageSize: 8,
  }
  const results = useQuery({
    queryKey: prosumersKeys.list(filters),
    queryFn: () => getProsumers(filters),
    enabled: debounced.length >= 2 && !selected,
    retry: false,
  })
  const items = debounced === search.trim() ? (results.data?.items ?? []) : []
  const visible = expanded && !selected && search.trim().length >= 2
  const choose = (person: Prosumer) => {
    onSelect(person)
    setExpanded(false)
    setActive(-1)
  }

  if (selected)
    return (
      <div className="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-brand-200 bg-brand-50/40 p-5">
        <ProsumerIdentity prosumer={selected} />
        <Button
          variant="secondary"
          size="sm"
          onClick={() => {
            onSelect(null)
            setSearch('')
            setDebounced('')
            setExpanded(true)
          }}
        >
          Change prosumer
        </Button>
        <div className="flex w-full flex-wrap gap-x-6 gap-y-2 border-t border-brand-100 pt-4 text-sm text-ink-600">
          <span className="flex items-center gap-2">
            <span className="size-1.5 rounded-full bg-brand-700" />
            Active account
          </span>
          <span>{selected.phoneNumber ?? 'No phone number on file'}</span>
        </div>
      </div>
    )

  return (
    <div>
      <label
        htmlFor={id}
        className="mb-2 block text-sm font-medium text-ink-800"
      >
        Find a prosumer
      </label>
      <div className="relative">
        <ReservationIcon
          name="search"
          className="pointer-events-none absolute left-3.5 top-3.5 size-5 text-ink-600"
        />
        <input
          id={id}
          role="combobox"
          aria-autocomplete="list"
          aria-expanded={visible}
          aria-controls={
            visible &&
            !results.isFetching &&
            debounced === search.trim() &&
            !results.isError
              ? `${id}-results`
              : undefined
          }
          aria-activedescendant={
            visible && active >= 0 && items[active]
              ? `${id}-option-${active}`
              : undefined
          }
          autoComplete="off"
          value={search}
          placeholder="Search by name, NIC or email"
          className="h-12 w-full rounded-lg border border-ink-200 bg-white pl-11 pr-4 text-sm text-ink-900 placeholder:text-ink-600 focus:border-brand-600 focus:outline-2 focus:outline-brand-100"
          onFocus={() => setExpanded(true)}
          onChange={(event) => {
            setSearch(event.target.value)
            setActive(-1)
            setExpanded(true)
          }}
          onKeyDown={(event) => {
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
              event.preventDefault()
              setExpanded(true)
              setActive((current) =>
                event.key === 'ArrowDown'
                  ? Math.min(current + 1, items.length - 1)
                  : Math.max(current - 1, 0),
              )
            }
            if (event.key === 'Enter') {
              event.preventDefault()
              if (visible && items[active]) choose(items[active])
            }
            if (event.key === 'Escape') {
              event.preventDefault()
              setExpanded(false)
            }
          }}
        />
      </div>
      <p className="mt-2 text-xs text-ink-600">
        Type at least two characters. Only active prosumers can make a
        reservation.
      </p>
      {visible && (
        <div className="mt-4 rounded-xl border border-ink-200 bg-white">
          {results.isError ? (
            <div role="alert" className="p-5 text-sm text-red-700">
              Prosumer search couldn’t load.{' '}
              <button
                type="button"
                className="ml-1 underline underline-offset-4"
                onClick={() => void results.refetch()}
              >
                Try again
              </button>
            </div>
          ) : results.isFetching || debounced !== search.trim() ? (
            <p role="status" className="p-5 text-sm text-ink-600">
              Searching active prosumers…
            </p>
          ) : (
            <>
              <p className="border-b border-ink-100 px-4 py-3 text-xs text-ink-600">
                {results.data?.totalCount ?? 0} matching prosumers
                {(results.data?.totalCount ?? 0) > items.length
                  ? ' · Refine your search for more results'
                  : ''}
              </p>
              <div
                role="listbox"
                id={`${id}-results`}
                aria-label="Matching prosumers"
              >
                {items.map((person, index) => (
                  <button
                    key={person.nic}
                    id={`${id}-option-${index}`}
                    type="button"
                    role="option"
                    aria-selected={active === index}
                    onClick={() => choose(person)}
                    onMouseEnter={() => setActive(index)}
                    className={`flex w-full items-center justify-between gap-3 border-b border-ink-100 p-4 text-left last:border-0 hover:bg-ink-50 ${active === index ? 'bg-ink-50' : ''}`}
                  >
                    <ProsumerIdentity prosumer={person} />
                    <ReservationIcon
                      name="chevron"
                      className="size-4 shrink-0 text-ink-600"
                    />
                  </button>
                ))}
              </div>
              {!items.length && (
                <div className="p-6">
                  <p className="text-sm font-medium text-ink-900">
                    No active prosumers found
                  </p>
                  <p className="mt-1 text-sm text-ink-600">
                    Try their NIC or email, or check their account status in
                    Prosumers.
                  </p>
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  )
}
