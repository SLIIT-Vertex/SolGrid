import { useEffect, useRef } from 'react'
import { useFormContext } from 'react-hook-form'
import { TextField } from '@/components/common/TextField'
import { useGoogleMapsScript } from '@/lib/useGoogleMapsScript'
import type { MicrogridNodeFormValues } from '@/features/microgrid/types'
import { MAX_ADDRESS_LENGTH } from '@/features/microgrid/types'
import { validateAddress, validateLatitude, validateLongitude } from '@/features/microgrid/validation'

/**
 * Address input backed by Google Places Autocomplete. Selecting a suggestion fills the address
 * text plus latitude/longitude together, since a station's coordinates must match its address.
 */
export function AddressAutocompleteField() {
  const {
    register,
    setValue,
    formState: { errors },
  } = useFormContext<MicrogridNodeFormValues>()
  const { isLoaded, error: scriptError } = useGoogleMapsScript()
  const inputRef = useRef<HTMLInputElement | null>(null)
  const autocompleteRef = useRef<google.maps.places.Autocomplete | null>(null)

  const addressRegister = register('addressLine', { validate: (value) => validateAddress(value) ?? true })
  const latitudeRegister = register('latitude', { validate: (value) => validateLatitude(value) ?? true })
  const longitudeRegister = register('longitude', { validate: (value) => validateLongitude(value) ?? true })

  useEffect(() => {
    if (!isLoaded || !inputRef.current || autocompleteRef.current) return

    const autocomplete = new google.maps.places.Autocomplete(inputRef.current, {
      fields: ['formatted_address', 'geometry'],
    })
    autocompleteRef.current = autocomplete

    const listener = autocomplete.addListener('place_changed', () => {
      const place = autocomplete.getPlace()
      const location = place.geometry?.location
      if (place.formatted_address) {
        setValue('addressLine', place.formatted_address, { shouldValidate: true, shouldDirty: true })
      }
      if (location) {
        setValue('latitude', String(location.lat()), { shouldValidate: true, shouldDirty: true })
        setValue('longitude', String(location.lng()), { shouldValidate: true, shouldDirty: true })
      }
    })

    return () => {
      listener.remove()
    }
  }, [isLoaded, setValue])

  return (
    <div className="grid grid-cols-1 gap-4">
      <TextField
        label="Address"
        maxLength={MAX_ADDRESS_LENGTH}
        placeholder={isLoaded ? 'Start typing an address…' : 'Address'}
        error={errors.addressLine?.message}
        hint={scriptError ?? undefined}
        {...addressRegister}
        ref={(element) => {
          addressRegister.ref(element)
          inputRef.current = element
        }}
      />
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <TextField
          label="Latitude"
          inputMode="decimal"
          placeholder="-90 to 90"
          readOnly
          error={errors.latitude?.message}
          hint="Filled automatically from the selected address."
          {...latitudeRegister}
        />
        <TextField
          label="Longitude"
          inputMode="decimal"
          placeholder="-180 to 180"
          readOnly
          error={errors.longitude?.message}
          hint="Filled automatically from the selected address."
          {...longitudeRegister}
        />
      </div>
    </div>
  )
}
