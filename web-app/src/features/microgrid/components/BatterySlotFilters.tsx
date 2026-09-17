import { Button } from '@/components/common/Button'
import { SelectField } from '@/components/common/SelectField'
import { TextField } from '@/components/common/TextField'
import { hasMicrogridSlotFilters } from '@/features/microgrid/types'
import type { MicrogridSlotFilters, SlotStatus } from '@/features/microgrid/types'

interface BatterySlotFiltersProps {
  filters: MicrogridSlotFilters
  rangeError?: string
  onChange: (updater: (current: MicrogridSlotFilters) => MicrogridSlotFilters) => void
}

export function BatterySlotFilters({ filters, rangeError, onChange }: BatterySlotFiltersProps) {
  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-3 md:flex-row md:items-end">
        <div className="w-full md:w-48">
          <SelectField
            id="microgrid-slot-status"
            label="Status"
            value={filters.status}
            onChange={(event) =>
              onChange((current) => ({
                ...current,
                status: event.target.value as SlotStatus | '',
                pageNumber: 1,
              }))
            }
          >
            <option value="">All statuses</option>
            <option value="Available">Available</option>
            <option value="Reserved">Reserved</option>
            <option value="Occupied">Occupied</option>
            <option value="OutOfService">Out of service</option>
          </SelectField>
        </div>
        <div className="w-full md:w-44">
          <TextField
            id="microgrid-slot-from"
            label="From date"
            type="date"
            value={filters.fromDate}
            onChange={(event) =>
              onChange((current) => ({
                ...current,
                fromDate: event.target.value,
                pageNumber: 1,
              }))
            }
          />
        </div>
        <div className="w-full md:w-44">
          <TextField
            id="microgrid-slot-to"
            label="To date"
            type="date"
            value={filters.toDate}
            onChange={(event) =>
              onChange((current) => ({
                ...current,
                toDate: event.target.value,
                pageNumber: 1,
              }))
            }
          />
        </div>
        <Button
          type="button"
          variant="secondary"
          onClick={() =>
            onChange((current) => ({
              ...current,
              status: '',
              fromDate: '',
              toDate: '',
              pageNumber: 1,
            }))
          }
          disabled={!hasMicrogridSlotFilters(filters)}
        >
          Clear filters
        </Button>
      </div>
      {rangeError ? (
        <p role="alert" className="text-sm text-red-600">
          {rangeError}
        </p>
      ) : null}
    </div>
  )
}
