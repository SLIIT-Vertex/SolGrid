import { useRef } from 'react'
import type { KeyboardEvent } from 'react'
import { cn } from '@/lib/cn'
import type { MicrogridDetailTab } from '@/features/microgrid/types'

const TABS: { id: MicrogridDetailTab; label: string }[] = [
  { id: 'overview', label: 'Overview' },
  { id: 'schedule', label: 'Schedule' },
  { id: 'slots', label: 'Battery Slots' },
]

interface MicrogridDetailTabsProps {
  value: MicrogridDetailTab
  onChange: (tab: MicrogridDetailTab) => void
}

export function MicrogridDetailTabs({ value, onChange }: MicrogridDetailTabsProps) {
  const listRef = useRef<HTMLDivElement>(null)

  const selectTab = (tab: MicrogridDetailTab) => {
    onChange(tab)
    queueMicrotask(() => {
      listRef.current?.querySelector<HTMLElement>(`#microgrid-tab-${tab}`)?.focus()
    })
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const index = TABS.findIndex((tab) => tab.id === value)
    if (index < 0) return

    if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
      event.preventDefault()
      selectTab(TABS[(index + 1) % TABS.length].id)
    } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
      event.preventDefault()
      selectTab(TABS[(index - 1 + TABS.length) % TABS.length].id)
    } else if (event.key === 'Home') {
      event.preventDefault()
      selectTab(TABS[0].id)
    } else if (event.key === 'End') {
      event.preventDefault()
      selectTab(TABS[TABS.length - 1].id)
    }
  }

  return (
    <div className="-mx-1 overflow-x-auto px-1">
      <div
        ref={listRef}
        role="tablist"
        aria-label="Node sections"
        className="flex min-w-max gap-1 border-b border-ink-100"
        onKeyDown={handleKeyDown}
      >
        {TABS.map((tab) => {
          const isActive = tab.id === value
          return (
            <button
              key={tab.id}
              type="button"
              role="tab"
              id={`microgrid-tab-${tab.id}`}
              aria-selected={isActive}
              aria-controls={`microgrid-panel-${tab.id}`}
              tabIndex={isActive ? 0 : -1}
              className={cn(
                'relative whitespace-nowrap px-3 py-2.5 text-sm font-medium transition-colors',
                'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600',
                isActive ? 'text-brand-700' : 'text-ink-500 hover:text-ink-800',
              )}
              onClick={() => onChange(tab.id)}
            >
              {tab.label}
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
