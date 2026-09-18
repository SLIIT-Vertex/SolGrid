import type { ReactNode } from 'react'

interface DashboardSectionProps {
  title: string
  description?: string
  children: ReactNode
}

export function DashboardSection({ title, description, children }: DashboardSectionProps) {
  return (
    <section className="space-y-3">
      <div>
        <h3 className="text-sm font-semibold text-ink-900">{title}</h3>
        {description ? <p className="text-sm text-ink-500">{description}</p> : null}
      </div>
      {children}
    </section>
  )
}
