import { useEffect, useRef, useState } from 'react'
import { useFormContext } from 'react-hook-form'
import { TextField } from '@/components/common/TextField'
import { useGoogleMapsScript } from '@/lib/useGoogleMapsScript'
import type { MicrogridNodeFormValues } from '@/features/microgrid/types'
import { MAX_ADDRESS_LENGTH } from '@/features/microgrid/types'
import { validateAddress, validateLatitude, validateLongitude } from '@/features/microgrid/validation'

const DEFAULT_CENTER = { lat: 6.9271, lng: 79.8612 } // Colombo — used only until a location is picked

/**
 * Address input backed by Google Places Autocomplete, plus an embedded map so the station's
 * coordinates can also be set by tapping/dragging a marker. Either path fills the same
 * addressLine/latitude/longitude form fields — a station's coordinates must match its address,
 * so both interactions stay in sync with each other. Latitude/longitude are stored but not shown
 * as raw numbers; the map and address text are the user-facing representation of the location.
 */
export function AddressAutocompleteField() {
  const {
    register,
    setValue,
    watch,
    formState: { errors },
  } = useFormContext<MicrogridNodeFormValues>()
  const { isLoaded, error: scriptError } = useGoogleMapsScript()
  const inputRef = useRef<HTMLInputElement | null>(null)
  const mapDivRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<google.maps.Map | null>(null)
  const markerRef = useRef<google.maps.marker.AdvancedMarkerElement | google.maps.Marker | null>(null)
  const geocoderRef = useRef<google.maps.Geocoder | null>(null)
  const autocompleteRef = useRef<google.maps.places.Autocomplete | null>(null)
  const [geocodeError, setGeocodeError] = useState<string | null>(null)

  const addressRegister = register('addressLine', { validate: (value) => validateAddress(value) ?? true })
  const latitudeRegister = register('latitude', { validate: (value) => validateLatitude(value) ?? true })
  const longitudeRegister = register('longitude', { validate: (value) => validateLongitude(value) ?? true })

  const currentLatitude = watch('latitude')
  const currentLongitude = watch('longitude')

  function applyLocation(position: google.maps.LatLng, addressOverride?: string) {
    setValue('latitude', String(position.lat()), { shouldValidate: true, shouldDirty: true })
    setValue('longitude', String(position.lng()), { shouldValidate: true, shouldDirty: true })
    if (addressOverride) {
      setValue('addressLine', addressOverride, { shouldValidate: true, shouldDirty: true })
    }
    mapRef.current?.panTo(position)
    const marker = markerRef.current
    if (marker instanceof google.maps.Marker) {
      marker.setPosition(position)
    } else if (marker) {
      marker.position = position
    }
  }

  function reverseGeocode(position: google.maps.LatLng) {
    setGeocodeError(null)
    geocoderRef.current?.geocode({ location: position }, (results, status) => {
      if (status === 'OK' && results?.[0]) {
        applyLocation(position, results[0].formatted_address)
      } else {
        // Coordinates are still usable even when reverse geocoding fails — just keep the
        // previous address text rather than blocking the pick.
        applyLocation(position)
        setGeocodeError('Could not look up an address for that point — coordinates were still set.')
      }
    })
  }

  // Places Autocomplete on the address text field.
  useEffect(() => {
    if (!isLoaded || !inputRef.current || autocompleteRef.current) return

    const autocomplete = new google.maps.places.Autocomplete(inputRef.current, {
      fields: ['formatted_address', 'geometry'],
    })
    autocompleteRef.current = autocomplete

    const listener = autocomplete.addListener('place_changed', () => {
      const place = autocomplete.getPlace()
      const location = place.geometry?.location
      if (location) {
        applyLocation(location, place.formatted_address)
      }
    })

    return () => {
      listener.remove()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoaded])

  // Embedded map with a marker the user can tap/drag to set coordinates directly.
  useEffect(() => {
    if (!isLoaded || !mapDivRef.current || mapRef.current) return

    const parsedLat = Number(currentLatitude)
    const parsedLng = Number(currentLongitude)
    const initialCenter =
      Number.isFinite(parsedLat) && Number.isFinite(parsedLng) && (parsedLat !== 0 || parsedLng !== 0)
        ? { lat: parsedLat, lng: parsedLng }
        : DEFAULT_CENTER

    const map = new google.maps.Map(mapDivRef.current, {
      center: initialCenter,
      zoom: 14,
      mapId: 'SOLGRID_NODE_LOCATION_MAP',
      streetViewControl: false,
      fullscreenControl: false,
    })
    mapRef.current = map
    geocoderRef.current = new google.maps.Geocoder()

    const AdvancedMarker = google.maps.marker?.AdvancedMarkerElement
    const marker = AdvancedMarker
      ? new AdvancedMarker({ map, position: initialCenter, gmpDraggable: true })
      : new google.maps.Marker({ map, position: initialCenter, draggable: true })
    markerRef.current = marker

    map.addListener('click', (event: google.maps.MapMouseEvent) => {
      if (event.latLng) reverseGeocode(event.latLng)
    })

    if (marker instanceof google.maps.Marker) {
      marker.addListener('dragend', () => {
        const position = marker.getPosition()
        if (position) reverseGeocode(position)
      })
    } else {
      marker.addListener('dragend', () => {
        const position = marker.position
        if (!position) return
        const latLng =
          position instanceof google.maps.LatLng ? position : new google.maps.LatLng(position.lat, position.lng)
        reverseGeocode(latLng)
      })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoaded])

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

      <div className="flex flex-col gap-1.5">
        <p className="text-sm font-medium text-ink-700">Pin the exact location</p>
        <div
          ref={mapDivRef}
          className="h-64 w-full overflow-hidden rounded-lg border border-ink-200 bg-ink-50"
          aria-label="Map for picking the station's exact GPS location"
        />
        <p className="text-sm text-ink-500">
          Tap the map or drag the pin to fine-tune the exact GPS position after searching an address.
        </p>
        {geocodeError ? <p className="text-sm text-amber-600">{geocodeError}</p> : null}
        {(errors.latitude?.message || errors.longitude?.message) ? (
          <p className="text-sm text-red-600">{errors.latitude?.message ?? errors.longitude?.message}</p>
        ) : null}
      </div>

      {/* Coordinates are captured by search/tap/drag above — kept as hidden fields so the exact
          values are still submitted and validated without showing raw numbers to the user. */}
      <input type="hidden" {...latitudeRegister} />
      <input type="hidden" {...longitudeRegister} />
    </div>
  )
}
