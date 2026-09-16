import { useFormContext } from 'react-hook-form'
import { TextField } from '@/components/common/TextField'
import type { MicrogridNodeFormValues } from '@/features/microgrid/types'
import { validateLatitude, validateLongitude } from '@/features/microgrid/validation'

export function CoordinateFields() {
  const {
    register,
    formState: { errors },
  } = useFormContext<MicrogridNodeFormValues>()

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
      <TextField
        label="Latitude"
        inputMode="decimal"
        placeholder="-90 to 90"
        error={errors.latitude?.message}
        {...register('latitude', { validate: (value) => validateLatitude(value) ?? true })}
      />
      <TextField
        label="Longitude"
        inputMode="decimal"
        placeholder="-180 to 180"
        error={errors.longitude?.message}
        {...register('longitude', { validate: (value) => validateLongitude(value) ?? true })}
      />
    </div>
  )
}
