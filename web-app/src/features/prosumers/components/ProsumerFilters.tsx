import { useEffect, useState } from 'react'
import { TextField } from '@/components/common/TextField'
import { SelectField } from '@/components/common/SelectField'
import type { ProsumerFilters as ProsumerFiltersState } from '@/features/prosumers/types'

interface ProsumerFiltersProps {
  filters: ProsumerFiltersState
  onChange: (updater: (current: ProsumerFiltersState) => ProsumerFiltersState) => void
  showStatusFilter?: boolean
}

export function ProsumerFilters({ filters, onChange, showStatusFilter = true }: ProsumerFiltersProps) {
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
    <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
      <div className="flex-1">
        <TextField
          label="Search"
          placeholder="Search by name, email, or NIC"
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
        />
      </div>
      {showStatusFilter ? (
        <div className="w-full sm:w-52">
          <SelectField
            label="Status"
            value={filters.status}
            onChange={(event) =>
              onChange((current) => ({
                ...current,
                status: event.target.value as ProsumerFiltersState['status'],
                pageNumber: 1,
              }))
            }
          >
            <option value="">All statuses</option>
            <option value="Pending">Pending</option>
            <option value="Active">Active</option>
            <option value="DeactivationRequested">Deactivation requested</option>
            <option value="Deactivated">Deactivated</option>
          </SelectField>
        </div>
      ) : null}
    </div>
  )
}
