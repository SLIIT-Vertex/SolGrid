import { useEffect, useState } from 'react'
import { TextField } from '@/components/common/TextField'
import { SelectField } from '@/components/common/SelectField'
import { Button } from '@/components/common/Button'
import type { UserFilters as UserFiltersState } from '@/features/users/types'
import { defaultUserFilters } from '@/features/users/types'
import { USER_SEARCH_MAX_LENGTH } from '@/features/users/validation'

interface UserFiltersProps {
  filters: UserFiltersState
  onChange: (updater: (current: UserFiltersState) => UserFiltersState) => void
}

export function UserFilters({ filters, onChange }: UserFiltersProps) {
  const [searchDraft, setSearchDraft] = useState(filters.searchText)

  useEffect(() => {
    const handle = setTimeout(() => {
      onChange((current) =>
        current.searchText === searchDraft ? current : { ...current, searchText: searchDraft, pageNumber: 1 },
      )
    }, 350)
    return () => clearTimeout(handle)
  }, [searchDraft, onChange])

  const hasActiveFilters = Boolean(filters.searchText || filters.role || filters.status)

  const clearFilters = () => {
    setSearchDraft('')
    onChange(() => ({ ...defaultUserFilters }))
  }

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
      <div className="flex-1">
        <TextField
          label="Search"
          placeholder="Search by name or email"
          maxLength={USER_SEARCH_MAX_LENGTH}
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
        />
      </div>
      <div className="w-full sm:w-44">
        <SelectField
          label="Role"
          value={filters.role}
          onChange={(event) =>
            onChange((current) => ({
              ...current,
              role: event.target.value as UserFiltersState['role'],
              pageNumber: 1,
            }))
          }
        >
          <option value="">All roles</option>
          <option value="Backoffice">Backoffice</option>
          <option value="GridOperator">Grid Operator</option>
        </SelectField>
      </div>
      <div className="w-full sm:w-44">
        <SelectField
          label="Status"
          value={filters.status}
          onChange={(event) =>
            onChange((current) => ({
              ...current,
              status: event.target.value as UserFiltersState['status'],
              pageNumber: 1,
            }))
          }
        >
          <option value="">All statuses</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
        </SelectField>
      </div>
      {hasActiveFilters ? (
        <Button
          type="button"
          variant="ghost"
          onClick={clearFilters}
          className="sm:self-end"
        >
          Clear filters
        </Button>
      ) : null}
    </div>
  )
}
