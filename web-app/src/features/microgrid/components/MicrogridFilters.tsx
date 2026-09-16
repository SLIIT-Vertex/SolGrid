import { useEffect, useState } from 'react'
import { Button } from '@/components/common/Button'
import { SelectField } from '@/components/common/SelectField'
import { TextField } from '@/components/common/TextField'
import { hasMicrogridNodeFilters } from '@/features/microgrid/types'
import type { MicrogridNodeFilters, SlotAvailabilityFilter, StationStatus } from '@/features/microgrid/types'

interface MicrogridFiltersProps {
  filters: MicrogridNodeFilters
  onChange: (updater: (current: MicrogridNodeFilters) => MicrogridNodeFilters) => void
}

export function MicrogridFilters({ filters, onChange }: MicrogridFiltersProps) {
  const [searchDraft, setSearchDraft] = useState(filters.searchText)

  useEffect(() => {
    const handle = setTimeout(() => {
      onChange((current) =>
        current.searchText === searchDraft ? current : { ...current, searchText: searchDraft, pageNumber: 1 },
      )
    }, 350)
    return () => clearTimeout(handle)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchDraft])

  return (
    <div className="flex flex-col gap-3 md:flex-row md:items-end">
      <div className="flex-1">
        <TextField
          id="microgrid-search"
          label="Search"
          placeholder="Search nodes…"
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
        />
      </div>
      <div className="w-full md:w-44">
        <SelectField
          id="microgrid-status"
          label="Status"
          value={filters.status}
          onChange={(event) =>
            onChange((current) => ({
              ...current,
              status: event.target.value as StationStatus | '',
              pageNumber: 1,
            }))
          }
        >
          <option value="">All statuses</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
        </SelectField>
      </div>
      <div className="w-full md:w-52">
        <SelectField
          id="microgrid-availability"
          label="Availability"
          value={filters.hasAvailableSlots}
          onChange={(event) =>
            onChange((current) => ({
              ...current,
              hasAvailableSlots: event.target.value as SlotAvailabilityFilter,
              pageNumber: 1,
            }))
          }
        >
          <option value="">All availability</option>
          <option value="available">Has available slots</option>
          <option value="none">No available slots</option>
        </SelectField>
      </div>
      <Button
        type="button"
        variant="secondary"
        onClick={() => {
          setSearchDraft('')
          onChange((current) => ({
            ...current,
            searchText: '',
            status: '',
            hasAvailableSlots: '',
            pageNumber: 1,
          }))
        }}
        disabled={!hasMicrogridNodeFilters(filters) && !searchDraft}
      >
        Clear filters
      </Button>
    </div>
  )
}
