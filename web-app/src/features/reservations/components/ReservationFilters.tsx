import { useEffect, useState } from 'react'
import { Button } from '@/components/common/Button'
import { SelectField } from '@/components/common/SelectField'
import { TextField } from '@/components/common/TextField'
import type { ReservationFilters as ReservationFiltersState, ReservationStatus } from '@/features/reservations/types'

interface ReservationFiltersProps {
  filters: ReservationFiltersState
  onChange: (updater: (current: ReservationFiltersState) => ReservationFiltersState) => void
  showStatusFilter?: boolean
}

export function ReservationFilters({ filters, onChange, showStatusFilter = true }: ReservationFiltersProps) {
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

  const hasFilters = Boolean(
    filters.searchText.trim() || filters.status || filters.scheduledFrom || filters.scheduledTo,
  )

  return (
    <div className="flex flex-col gap-3 md:flex-row md:items-end md:flex-wrap">
      <div className="min-w-48 flex-1">
        <TextField
          label="Search"
          placeholder="Search by prosumer or station"
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
        />
      </div>
      {showStatusFilter ? (
        <div className="w-full md:w-44">
          <SelectField
            label="Status"
            value={filters.status}
            onChange={(event) =>
              onChange((current) => ({
                ...current,
                status: event.target.value as ReservationStatus | '',
                pageNumber: 1,
              }))
            }
          >
            <option value="">All statuses</option>
            <option value="Pending">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Rejected">Rejected</option>
            <option value="Cancelled">Cancelled</option>
            <option value="Completed">Completed</option>
          </SelectField>
        </div>
      ) : null}
      <div className="w-full md:w-44">
        <TextField
          label="From"
          type="date"
          value={filters.scheduledFrom}
          onChange={(event) =>
            onChange((current) => ({ ...current, scheduledFrom: event.target.value, pageNumber: 1 }))
          }
        />
      </div>
      <div className="w-full md:w-44">
        <TextField
          label="To"
          type="date"
          value={filters.scheduledTo}
          onChange={(event) =>
            onChange((current) => ({ ...current, scheduledTo: event.target.value, pageNumber: 1 }))
          }
        />
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
            scheduledFrom: '',
            scheduledTo: '',
            pageNumber: 1,
          }))
        }}
        disabled={!hasFilters && !searchDraft}
      >
        Clear filters
      </Button>
    </div>
  )
}
