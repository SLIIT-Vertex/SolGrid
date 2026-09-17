import { createContext, useCallback, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { cn } from '@/lib/cn'

type ToastTone = 'success' | 'error'

interface Toast {
  id: number
  message: string
  tone: ToastTone
}

interface ToastContextValue {
  showToast: (message: string, tone?: ToastTone) => void
}

// eslint-disable-next-line react-refresh/only-export-components
export const ToastContext = createContext<ToastContextValue | undefined>(undefined)

let toastId = 0

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])

  const showToast = useCallback((message: string, tone: ToastTone = 'success') => {
    const id = ++toastId
    setToasts((current) => [...current, { id, message, tone }])
    setTimeout(() => {
      setToasts((current) => current.filter((toast) => toast.id !== id))
    }, 4000)
  }, [])

  const value = useMemo(() => ({ showToast }), [showToast])

  return (
    <ToastContext.Provider value={value}>
      {children}
      {createPortal(
        <div className="pointer-events-none fixed bottom-4 right-4 z-[100] flex max-w-[calc(100%-2rem)] flex-col gap-2">
          {toasts.map((toast) => (
            <div
              key={toast.id}
              role={toast.tone === 'error' ? 'alert' : 'status'}
              aria-live={toast.tone === 'error' ? 'assertive' : 'polite'}
              className={cn(
                'pointer-events-auto min-w-[260px] rounded-lg px-4 py-3 text-sm font-medium shadow-lg ring-1',
                toast.tone === 'success'
                  ? 'bg-brand-600 text-white ring-brand-700'
                  : 'bg-red-600 text-white ring-red-700',
              )}
            >
              {toast.message}
            </div>
          ))}
        </div>,
        document.body,
      )}
    </ToastContext.Provider>
  )
}
