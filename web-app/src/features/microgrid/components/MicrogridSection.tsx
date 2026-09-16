import type { ReactNode } from 'react'

interface MicrogridSectionProps {
  title: string
  description?: string
  action?: ReactNode
  children?: ReactNode
}

export function MicrogridSection({ title, description, action, children }: MicrogridSectionProps) {
  return (
    <section className="rounded-2xl border border-ink-100 bg-white p-5 sm:p-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-sm font-semibold text-ink-900">{title}</h3>
          {description ? <p className="mt-1 text-sm text-ink-500">{description}</p> : null}
        </div>
        {action}
      </div>
      {children ? <div className="mt-4">{children}</div> : null}
    </section>
  )
}
