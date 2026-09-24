import { useState } from 'react'
import type { KeyboardEvent } from 'react'
import { ErrorState, LoadingState } from '@/components/common/QueryStates'
import { formatDateTime } from '@/features/microgrid/format'
import { AccountsTab } from '@/features/analytics/components/AccountsTab'
import { NodesTab } from '@/features/analytics/components/NodesTab'
import { OverviewTab } from '@/features/analytics/components/OverviewTab'
import { ReservationsTab } from '@/features/analytics/components/ReservationsTab'
import { RoutineLedger } from '@/features/analytics/components/RoutineLedger'
import { useAnalytics } from '@/features/analytics/hooks/useAnalytics'
import { cn } from '@/lib/cn'
import type { AnalyticsSnapshot } from '@/features/analytics/types'

type TabId = 'overview' | 'nodes' | 'reservations' | 'routines' | 'accounts'

interface TabDef {
  id: TabId
  label: string
}

function tabsFor(snapshot: AnalyticsSnapshot): TabDef[] {
  const tabs: TabDef[] = [
    { id: 'overview', label: 'Overview' },
    { id: 'nodes', label: 'Nodes' },
    { id: 'reservations', label: 'Reservations' },
    { id: 'routines', label: 'Business rules' },
  ]
  if (snapshot.accounts) tabs.push({ id: 'accounts', label: 'Accounts' })
  return tabs
}

export function AnalyticsPage() {
  const { data, isLoading, isError, refetch } = useAnalytics()
  const [tab, setTab] = useState<TabId>('overview')

  if (isLoading) return <LoadingState label="Loading analytics…" />
  if (isError || !data) {
    return <ErrorState message="We couldn't load analytics." onRetry={() => refetch()} />
  }

  const tabs = tabsFor(data)
  const active = tabs.some((item) => item.id === tab) ? tab : 'overview'
  const flags = data.routines.filter((routine) => routine.outcome !== 'Clear').length

  const handleKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    const index = tabs.findIndex((item) => item.id === active)
    let next: number | null = null
    if (event.key === 'ArrowRight') next = (index + 1) % tabs.length
    else if (event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length
    else if (event.key === 'Home') next = 0
    else if (event.key === 'End') next = tabs.length - 1
    if (next === null) return
    event.preventDefault()
    setTab(tabs[next].id)
    document.getElementById(`analytics-tab-${tabs[next].id}`)?.focus()
  }

  return (
    <div className="mx-auto max-w-6xl">
      <p className="text-sm text-ink-500">
        Calculated by the service at {formatDateTime(data.generatedAtUtc)}.
      </p>

      <div className="mt-4 border-b border-ink-200">
        <div role="tablist" aria-label="Analytics views" className="flex gap-6 overflow-x-auto overflow-y-hidden">
          {tabs.map((item) => {
            const selected = item.id === active
            return (
              <button
                key={item.id}
                id={`analytics-tab-${item.id}`}
                role="tab"
                type="button"
                aria-selected={selected}
                aria-controls={`analytics-panel-${item.id}`}
                tabIndex={selected ? 0 : -1}
                onClick={() => setTab(item.id)}
                onKeyDown={handleKeyDown}
                className={cn(
                  'relative -mb-px whitespace-nowrap border-b-2 px-1 py-3 text-sm font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600',
                  selected
                    ? 'border-brand-600 text-ink-900'
                    : 'border-transparent text-ink-500 hover:text-ink-800',
                )}
              >
                {item.label}
                {item.id === 'routines' && flags > 0 ? (
                  <span className="ml-2 inline-flex min-w-5 items-center justify-center rounded-full bg-amber-100 px-1.5 text-xs font-semibold tabular-nums text-amber-700">
                    {flags}
                  </span>
                ) : null}
              </button>
            )
          })}
        </div>
      </div>

      <div
        id={`analytics-panel-${active}`}
        role="tabpanel"
        aria-labelledby={`analytics-tab-${active}`}
        className="mt-6"
      >
        {active === 'overview' ? <OverviewTab snapshot={data} /> : null}
        {active === 'nodes' ? <NodesTab snapshot={data} /> : null}
        {active === 'reservations' ? <ReservationsTab snapshot={data} /> : null}
        {active === 'routines' ? <RoutineLedger routines={data.routines} /> : null}
        {active === 'accounts' && data.accounts ? <AccountsTab accounts={data.accounts} /> : null}
      </div>
    </div>
  )
}
