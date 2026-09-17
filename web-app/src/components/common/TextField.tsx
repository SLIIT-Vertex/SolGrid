import { forwardRef } from 'react'
import type { ChangeEventHandler, InputHTMLAttributes } from 'react'
import { cn } from '@/lib/cn'

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  hint?: string
  sanitizeValue?: (value: string) => string
}

export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(
  ({ label, error, hint, id, className, sanitizeValue, onChange, ...rest }, ref) => {
    const inputId = id ?? rest.name
    const handleChange: ChangeEventHandler<HTMLInputElement> = (event) => {
      if (sanitizeValue) {
        event.currentTarget.value = sanitizeValue(event.currentTarget.value)
      }
      onChange?.(event)
    }

    return (
      <div className="flex flex-col gap-1.5">
        {label ? (
          <label htmlFor={inputId} className="text-sm font-medium text-ink-700">
            {label}
          </label>
        ) : null}
        <input
          ref={ref}
          id={inputId}
          className={cn(
            'h-10 rounded-lg border border-ink-200 bg-white px-3 text-sm text-ink-900',
            'placeholder:text-ink-400',
            'focus:border-brand-500 focus:outline focus:outline-2 focus:outline-brand-100',
            'read-only:bg-ink-50 read-only:text-ink-700',
            error && 'border-red-300 focus:border-red-400 focus:outline-red-100',
            className,
          )}
          {...rest}
          onChange={handleChange}
          aria-invalid={Boolean(error)}
          aria-describedby={
            error ? `${inputId}-error` : rest['aria-describedby'] ?? (hint ? `${inputId}-hint` : undefined)
          }
        />
        {error ? (
          <p id={`${inputId}-error`} role="alert" className="text-sm text-red-600">
            {error}
          </p>
        ) : hint ? (
          <p id={`${inputId}-hint`} className="text-sm text-ink-500">{hint}</p>
        ) : null}
      </div>
    )
  },
)

TextField.displayName = 'TextField'
