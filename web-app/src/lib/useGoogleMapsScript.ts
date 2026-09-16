import { useEffect, useState } from 'react'

declare global {
  interface Window {
    google?: typeof google
  }
}

let loadPromise: Promise<void> | null = null

function loadGoogleMapsScript(apiKey: string): Promise<void> {
  if (window.google?.maps?.places) {
    return Promise.resolve()
  }
  if (loadPromise) {
    return loadPromise
  }

  loadPromise = new Promise((resolve, reject) => {
    const script = document.createElement('script')
    script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&loading=async&callback=__solgridGoogleMapsLoaded`
    script.async = true
    script.onerror = () => {
      loadPromise = null
      reject(new Error('Failed to load Google Maps script.'))
    }
    ;(window as unknown as Record<string, () => void>).__solgridGoogleMapsLoaded = () => {
      // The base script is ready; explicitly import the Places library so google.maps.places
      // is guaranteed populated before callers touch it (the libraries= query param alone
      // doesn't reliably finish before script.onload fires with loading=async).
      google.maps
        .importLibrary('places')
        .then(() => resolve())
        .catch(() => {
          loadPromise = null
          reject(new Error('Failed to load Google Maps Places library.'))
        })
    }
    document.head.appendChild(script)
  })

  return loadPromise
}

/** Loads the Google Maps JS SDK (Places library) once and reports readiness/error state. */
export function useGoogleMapsScript() {
  const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY as string | undefined
  const [isLoaded, setIsLoaded] = useState(Boolean(window.google?.maps?.places))
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    if (!apiKey || isLoaded) return

    let cancelled = false
    loadGoogleMapsScript(apiKey)
      .then(() => {
        if (!cancelled) setIsLoaded(true)
      })
      .catch(() => {
        if (!cancelled) setLoadError('Could not load Google Maps.')
      })

    return () => {
      cancelled = true
    }
  }, [apiKey, isLoaded])

  const error = !apiKey ? 'Google Maps API key is not configured.' : loadError

  return { isLoaded, error, apiKey }
}
