import type { Prosumer } from '@/features/prosumers/types'

export function ProsumerIdentity({ prosumer }: { prosumer: Prosumer }) {
  return (
    <div className="flex min-w-0 items-center gap-3">
      <span
        className="flex size-11 shrink-0 items-center justify-center rounded-full bg-ink-100 text-sm font-semibold text-ink-700"
        aria-hidden="true"
      >
        {prosumer.firstName[0]}
        {prosumer.lastName[0]}
      </span>
      <div className="min-w-0">
        <p className="font-semibold text-ink-900">
          {prosumer.firstName} {prosumer.lastName}
        </p>
        <p className="mt-1 break-all text-sm text-ink-600">
          {prosumer.nic} <span aria-hidden="true">·</span> {prosumer.email}
        </p>
      </div>
    </div>
  )
}
