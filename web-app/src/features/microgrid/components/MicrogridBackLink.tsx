import { Link } from 'react-router-dom'

interface MicrogridBackLinkProps {
  to?: string
  label?: string
}

export function MicrogridBackLink({
  to = '/microgrid',
  label = 'Microgrid Nodes',
}: MicrogridBackLinkProps) {
  return (
    <Link
      to={to}
      className="mb-4 inline-flex items-center gap-1.5 text-sm font-medium text-ink-500 transition-colors hover:text-brand-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
    >
      <svg viewBox="0 0 20 20" fill="currentColor" className="size-4" aria-hidden="true">
        <path
          fillRule="evenodd"
          d="M7.707 14.707a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 0-1.414l4-4a1 1 0 0 1 1.414 1.414L5.414 9H17a1 1 0 1 1 0 2H5.414l2.293 2.293a1 1 0 0 1 0 1.414Z"
          clipRule="evenodd"
        />
      </svg>
      {label}
    </Link>
  )
}
