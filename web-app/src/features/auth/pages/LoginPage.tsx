import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '@/auth/useAuth'
import type { LoginRequest } from '@/auth/types'
import loginIllustration from '@/assets/login.png'
import { Button } from '@/components/common/Button'
import { TextField } from '@/components/common/TextField'
import { Logo } from '@/components/layout/Logo'
import { getErrorMessage } from '@/lib/problemDetails'

export function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginRequest>({ defaultValues: { email: '', password: '' } })

  if (isAuthenticated) {
    const redirectTo = (location.state as { from?: Location })?.from?.pathname ?? '/'
    return <Navigate to={redirectTo} replace />
  }

  const onSubmit = async (values: LoginRequest) => {
    setFormError(null)
    try {
      await login(values)
      navigate('/', { replace: true })
    } catch (error) {
      setFormError(getErrorMessage(error, 'Invalid email or password.'))
    }
  }

  return (
    <div className="flex min-h-svh items-center justify-center bg-ink-50 px-4 py-8">
      <div className="grid w-full max-w-4xl overflow-hidden rounded-3xl border border-ink-100 bg-white shadow-sm lg:grid-cols-2">
        <div className="hidden flex-col justify-between bg-brand-50 p-10 lg:flex">
          <div className="flex items-center gap-2.5">
            <Logo className="size-20 object-contain" />
            <span className="text-lg font-semibold text-ink-900">SolGrid</span>
          </div>
          <img
            src={loginIllustration}
            alt=""
            className="mx-auto w-full max-w-sm object-contain"
          />
          <div />
        </div>

        <div className="flex flex-col justify-center p-8 sm:p-10">
          <div className="mb-8 flex flex-col items-center gap-3 lg:hidden">
            <Logo className="size-20 object-contain" />
          </div>

          <h1 className="text-2xl font-semibold text-ink-900">Login</h1>
          <p className="mt-1 text-sm text-ink-500">
            Welcome back! Please login to your account.
          </p>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-8 flex flex-col gap-4" noValidate>
            {formError ? (
              <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
                {formError}
              </div>
            ) : null}

            <TextField
              label="Your email"
              type="email"
              autoComplete="email"
              placeholder="you@solgrid.com"
              error={errors.email?.message}
              {...register('email', { required: 'Email is required' })}
            />

            <TextField
              label="Your password"
              type="password"
              autoComplete="current-password"
              placeholder="••••••••"
              error={errors.password?.message}
              {...register('password', { required: 'Password is required' })}
            />

            <Button type="submit" className="mt-2 w-full" isLoading={isSubmitting}>
              Login
            </Button>
          </form>

          <p className="mt-6 text-center text-xs text-ink-400">
            Backoffice and Grid Operator accounts only.
          </p>
        </div>
      </div>
    </div>
  )
}
