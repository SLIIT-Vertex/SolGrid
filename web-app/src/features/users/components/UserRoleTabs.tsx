import { useRef } from 'react'
import type { KeyboardEvent } from 'react'
import { userRoleTabs } from '@/features/users/roleTabs'
import type { UserRoleTabId } from '@/features/users/roleTabs'
import type { UserRoleCounts } from '@/features/users/hooks/useUserRoleCounts'
import { cn } from '@/lib/cn'

interface UserRoleTabsProps {
  value: UserRoleTabId
  counts: UserRoleCounts
  onChange: (tab: UserRoleTabId) => void
}

export function UserRoleTabs({ value, counts, onChange }: UserRoleTabsProps) {
  const listRef = useRef<HTMLDivElement>(null)

  const selectTab = (tab: UserRoleTabId) => {
    onChange(tab)
    queueMicrotask(() => {
      listRef.current?.querySelector<HTMLElement>(`#user-role-tab-${tab}`)?.focus()
    })
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const index = userRoleTabs.findIndex((tab) => tab.id === value)
    if (index < 0) return

    if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
      event.preventDefault()
      selectTab(userRoleTabs[(index + 1) % userRoleTabs.length].id)
    } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
      event.preventDefault()
      selectTab(userRoleTabs[(index - 1 + userRoleTabs.length) % userRoleTabs.length].id)
    } else if (event.key === 'Home') {
      event.preventDefault()
      selectTab(userRoleTabs[0].id)
    } else if (event.key === 'End') {
      event.preventDefault()
      selectTab(userRoleTabs[userRoleTabs.length - 1].id)
    }
  }

  return (
    <div className="-mx-1 overflow-x-auto px-1">
      <div
        ref={listRef}
        role="tablist"
        aria-label="User roles"
        className="flex min-w-max gap-1 border-b border-ink-100"
        onKeyDown={handleKeyDown}
      >
        {userRoleTabs.map((tab) => {
          const isActive = tab.id === value
          const count = counts[tab.id]
          return (
            <button
              key={tab.id}
              type="button"
              role="tab"
              id={`user-role-tab-${tab.id}`}
              aria-selected={isActive}
              aria-controls="user-role-panel"
              tabIndex={isActive ? 0 : -1}
              className={cn(
                'relative flex items-center gap-2 whitespace-nowrap px-3 py-2.5 text-sm font-medium transition-colors',
                'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600',
                isActive ? 'text-brand-700' : 'text-ink-500 hover:text-ink-800',
              )}
              onClick={() => onChange(tab.id)}
            >
              {tab.label}
              {count === undefined ? null : (
                <span
                  className={cn(
                    'rounded-full px-1.5 py-0.5 text-xs font-semibold tabular-nums',
                    isActive ? 'bg-brand-50 text-brand-700' : 'bg-ink-100 text-ink-500',
                  )}
                >
                  {count}
                </span>
              )}
              {isActive ? (
                <span className="absolute inset-x-2 -bottom-px h-0.5 rounded-full bg-brand-600" />
              ) : null}
            </button>
          )
        })}
      </div>
    </div>
  )
}
