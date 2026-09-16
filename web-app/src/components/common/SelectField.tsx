import { forwardRef } from 'react'
import type { SelectHTMLAttributes } from 'react'
import { cn } from '@/lib/cn'

interface SelectFieldProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
}

export const SelectField = forwardRef<HTMLSelectElement, SelectFieldProps>(
  ({ label, error, id, className, children, ...rest }, ref) => {
    const selectId = id ?? rest.name
    return (
      <div className="flex flex-col gap-1.5">
        {label ? (
          <label htmlFor={selectId} className="text-sm font-medium text-ink-700">
            {label}
          </label>
        ) : null}
        <div className="relative">
          <select
            ref={ref}
            id={selectId}
            className={cn(
              'h-10 w-full appearance-none rounded-lg border border-ink-200 bg-white pl-3 pr-9 text-sm text-ink-900',
              'transition-colors hover:border-ink-300',
              'focus:border-brand-500 focus:outline focus:outline-2 focus:outline-brand-100',
              error && 'border-red-300 focus:border-red-400 focus:outline-red-100',
              className,
            )}
            aria-invalid={Boolean(error)}
            aria-describedby={error ? `${selectId}-error` : undefined}
            {...rest}
          >
            {children}
          </select>
          <svg
            viewBox="0 0 20 20"
            fill="none"
            aria-hidden="true"
            className="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-ink-400"
          >
            <path
              d="M5.5 7.5 10 12l4.5-4.5"
              stroke="currentColor"
              strokeWidth="1.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </div>
        {error ? (
          <p id={`${selectId}-error`} className="text-sm text-red-600">
            {error}
          </p>
        ) : null}
      </div>
    )
  },
)

SelectField.displayName = 'SelectField'
