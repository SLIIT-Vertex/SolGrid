import { useEffect, useRef, useState } from 'react'
import type { MicrogridNode } from '@/features/microgrid/types'
import { useGoogleMapsScript } from '@/lib/useGoogleMapsScript'

export function useStationPickerMap(
  nodes: MicrogridNode[],
  selected: MicrogridNode | undefined,
  onSelect: (id: string) => void,
) {
  const { isLoaded, error } = useGoogleMapsScript()
  const mapId = (
    import.meta.env.VITE_GOOGLE_MAPS_MAP_ID as string | undefined
  )?.trim()
  const [timedOut, setTimedOut] = useState(false)
  const mapUnavailable = error || (!isLoaded && timedOut)
  useEffect(() => {
    if (isLoaded || error) return
    const timer = setTimeout(() => setTimedOut(true), 12_000)
    return () => clearTimeout(timer)
  }, [isLoaded, error])
  const container = useRef<HTMLDivElement>(null)
  const mapRef = useRef<google.maps.Map | null>(null)
  const markersRef = useRef<
    {
      id: string
      marker: google.maps.marker.AdvancedMarkerElement | google.maps.Marker
    }[]
  >([])
  useEffect(() => {
    if (!isLoaded || !container.current || !nodes.length) return
    const map = new google.maps.Map(container.current, {
      center: { lat: nodes[0].latitude, lng: nodes[0].longitude },
      zoom: 12,
      ...(mapId ? { mapId } : {}),
      streetViewControl: false,
      mapTypeControl: false,
      fullscreenControl: false,
    })
    mapRef.current = map
    const bounds = new google.maps.LatLngBounds()
    const markers = nodes.map((node) => {
      const position = { lat: node.latitude, lng: node.longitude }
      bounds.extend(position)
      const AdvancedMarker = mapId
        ? google.maps.marker?.AdvancedMarkerElement
        : undefined
      const marker = AdvancedMarker
        ? new AdvancedMarker({ map, position, title: node.name })
        : new google.maps.Marker({ map, position, title: node.name })
      marker.addListener('click', () => onSelect(node.id))
      return { id: node.id, marker }
    })
    markersRef.current = markers
    if (nodes.length > 1) map.fitBounds(bounds, 50)
    const zoomListener = google.maps.event.addListenerOnce(map, 'idle', () => {
      if ((map.getZoom() ?? 0) > 15) map.setZoom(15)
    })
    return () => {
      google.maps.event.removeListener(zoomListener)
      markers.forEach(({ marker }) => {
        google.maps.event.clearInstanceListeners(marker)
        if ('setMap' in marker) marker.setMap(null)
        else marker.map = null
      })
      mapRef.current = null
      markersRef.current = []
    }
  }, [isLoaded, nodes, onSelect, mapId])
  useEffect(() => {
    if (!selected || !mapRef.current) return
    mapRef.current.panTo({ lat: selected.latitude, lng: selected.longitude })
    markersRef.current.forEach(({ id, marker }) => {
      const zIndex = id === selected.id ? 100 : 1
      if ('setZIndex' in marker) marker.setZIndex(zIndex)
      else marker.zIndex = zIndex
    })
  }, [selected, isLoaded, nodes])

  return { container, isLoaded, mapUnavailable }
}
