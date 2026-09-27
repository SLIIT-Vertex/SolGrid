import { DashboardSection } from '@/features/dashboard/components/DashboardSection'
import { StatCard } from '@/features/dashboard/components/StatCard'
import { useProsumers } from '@/features/prosumers/hooks/useProsumers'
import { defaultPendingProsumerFilters, defaultProsumerFilters } from '@/features/prosumers/types'
import { useUsers } from '@/features/users/hooks/useUsers'
import { defaultUserFilters } from '@/features/users/types'

export function BackofficeMetrics() {
  const users = useUsers({ ...defaultUserFilters, pageSize: 1 })
  const backofficeUsers = useUsers({ ...defaultUserFilters, role: 'Backoffice', pageSize: 1 })
  const operatorUsers = useUsers({ ...defaultUserFilters, role: 'GridOperator', pageSize: 1 })
  const prosumers = useProsumers({ ...defaultProsumerFilters, pageSize: 1 })
  const pendingProsumers = useProsumers({ ...defaultPendingProsumerFilters, pageSize: 1 })

  return (
    <>
      <DashboardSection title="Web users" description="Staff accounts that can sign in to this console.">
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-3">
          <StatCard
            label="All users"
            value={users.data?.totalCount}
            hint="Backoffice and Grid Operator"
            to="/users"
            isLoading={users.isLoading}
            isError={users.isError}
          />
          <StatCard
            label="Backoffice"
            value={backofficeUsers.data?.totalCount}
            hint="Full console access"
            to="/users"
            isLoading={backofficeUsers.isLoading}
            isError={backofficeUsers.isError}
          />
          <StatCard
            label="Grid Operators"
            value={operatorUsers.data?.totalCount}
            hint="Reservations and slots"
            to="/users"
            isLoading={operatorUsers.isLoading}
            isError={operatorUsers.isError}
          />
        </div>
      </DashboardSection>

      <DashboardSection title="Prosumers" description="EV owner accounts registered against the grid.">
        <div className="grid grid-cols-2 gap-4">
          <StatCard
            label="Awaiting approval"
            value={pendingProsumers.data?.totalCount}
            hint="New sign-ups to review"
            to="/prosumers"
            isLoading={pendingProsumers.isLoading}
            isError={pendingProsumers.isError}
            needsAttention
          />
          <StatCard
            label="All prosumers"
            value={prosumers.data?.totalCount}
            hint="Every registration"
            to="/prosumers"
            isLoading={prosumers.isLoading}
            isError={prosumers.isError}
          />
        </div>
      </DashboardSection>
    </>
  )
}
