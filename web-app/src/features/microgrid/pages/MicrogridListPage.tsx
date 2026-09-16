import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { Pagination } from '@/components/common/Pagination'
import { EmptyState, ErrorState, LoadingState } from '@/components/common/QueryStates'
import { useAuth } from '@/auth/useAuth'
import { MicrogridCardList } from '@/features/microgrid/components/MicrogridCard'
import { MicrogridFilters } from '@/features/microgrid/components/MicrogridFilters'
import { MicrogridTable } from '@/features/microgrid/components/MicrogridTable'
import { useMicrogridNodes } from '@/features/microgrid/hooks/useMicrogridNodes'
import { canCreateNode } from '@/features/microgrid/permissions'
import { defaultMicrogridNodeFilters, hasMicrogridNodeFilters } from '@/features/microgrid/types'

function PlusIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" className={className} aria-hidden="true">
      <path d="M10.75 4.75a.75.75 0 0 0-1.5 0v4.5h-4.5a.75.75 0 0 0 0 1.5h4.5v4.5a.75.75 0 0 0 1.5 0v-4.5h4.5a.75.75 0 0 0 0-1.5h-4.5v-4.5Z" />
    </svg>
  )
}

export function MicrogridListPage() {
  const { session } = useAuth()
  const navigate = useNavigate()
  const [filters, setFilters] = useState(defaultMicrogridNodeFilters)
  const [filterResetKey, setFilterResetKey] = useState(0)
  const { data, isLoading, isError, refetch } = useMicrogridNodes(filters)
  const filtersActive = hasMicrogridNodeFilters(filters)
  const showCreate = canCreateNode(session)

  const clearFilters = () => {
    setFilters(defaultMicrogridNodeFilters)
    setFilterResetKey((current) => current + 1)
  }

  return (
    <div className="mx-auto max-w-5xl">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <p className="max-w-xl text-sm text-ink-500">
          Manage generation nodes and battery storage availability.
        </p>
        {showCreate ? (
          <Button
            type="button"
            className="w-full sm:w-auto"
            leftIcon={<PlusIcon className="size-4" />}
            onClick={() => navigate('/microgrid/new')}
          >
            New Node
          </Button>
        ) : null}
      </div>

      <div className="rounded-2xl border border-ink-100 bg-white">
        <div className="border-b border-ink-100 p-4">
          <MicrogridFilters key={filterResetKey} filters={filters} onChange={setFilters} />
        </div>

        {isLoading ? (
          <LoadingState label="Loading Microgrid Nodes…" />
        ) : isError ? (
          <ErrorState message="We couldn't load Microgrid Nodes." onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState
            title={filtersActive ? 'No nodes match your filters.' : 'No Microgrid Nodes have been created yet.'}
            description={
              filtersActive
                ? 'Clear filters and try again.'
                : showCreate
                  ? 'Register a generation node to start managing operating hours and battery storage.'
                  : 'When Backoffice registers a node, it will appear here for operational review.'
            }
            action={
              filtersActive ? (
                <Button type="button" variant="secondary" onClick={clearFilters}>
                  Clear filters
                </Button>
              ) : undefined
            }
          />
        ) : (
          <>
            <div className="hidden md:block">
              <MicrogridTable nodes={data.items} />
            </div>
            <div className="md:hidden">
              <MicrogridCardList nodes={data.items} />
            </div>
            <div className="min-w-0 overflow-x-auto">
              <Pagination
                pageNumber={data.pageNumber}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                onPageChange={(page) => setFilters((current) => ({ ...current, pageNumber: page }))}
              />
            </div>
          </>
        )}
      </div>
    </div>
  )
}
