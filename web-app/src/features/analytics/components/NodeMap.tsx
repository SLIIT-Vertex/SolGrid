import { useEffect, useMemo, useRef, useState } from 'react'
import { formatGenerationKw, formatStorageKwh } from '@/features/microgrid/format'
import { useGoogleMapsScript } from '@/lib/useGoogleMapsScript'
import type { NodeAnalytics } from '@/features/analytics/types'

/** Builds the small floating summary shown above a pin when it is clicked. */
function summaryHtml(node: NodeAnalytics): string {
  const active = node.status === 'Active'
  const statusColor = active ? '#139a68' : '#b91c1c'
  const statusBg = active ? '#f0fdf6' : '#fef2f2'
  const rows: [string, string][] = [
    ['Capacity', formatGenerationKw(node.capacityKw)],
    ['Slots free', `${node.availableSlots} / ${node.totalSlots}`],
    ['Battery stored', formatStorageKwh(node.totalBatteryKwh)],
    ['Live bookings', String(node.activeReservations)],
    ['Utilization', `${node.utilizationPercent}%`],
  ]
  const rowsHtml = rows
    .map(
      ([label, value]) =>
        `<div style="display:flex;justify-content:space-between;gap:16px;font-size:12px;line-height:20px"><span style="color:#5f6874">${label}</span><span style="color:#1f2227;font-variant-numeric:tabular-nums">${value}</span></div>`,
    )
    .join('')

  return `
    <div style="min-width:184px;font-family:inherit">
      <p style="margin:0;font-size:13px;font-weight:600;color:#1f2227">${escapeHtml(node.name)}</p>
      <p style="margin:2px 0 8px;font-family:ui-monospace,monospace;font-size:11px;color:#78828f">${escapeHtml(node.code)}</p>
      <span style="display:inline-block;margin-bottom:8px;padding:1px 8px;border-radius:9999px;font-size:11px;font-weight:600;color:${statusColor};background:${statusBg}">${node.status}</span>
      ${rowsHtml}
      <p style="margin:8px 0 0;font-size:11px;line-height:16px;color:${node.deactivationBlocked ? '#b45309' : '#78828f'}">
        ${node.deactivationBlocked ? 'Deactivation blocked — live reservations.' : escapeHtml(node.addressLine)}
      </p>
    </div>`
}

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (char) => {
    switch (char) {
      case '&':
        return '&amp;'
      case '<':
        return '&lt;'
      case '>':
        return '&gt;'
      case '"':
        return '&quot;'
      default:
        return '&#39;'
    }
  })
}

/** A solar-themed teardrop pin: a sun glyph on a brand-colored map marker. */
function pinSvg(color: string): string {
  const rays = [0, 45, 90, 135, 180, 225, 270, 315]
    .map((deg) => {
      const rad = (deg * Math.PI) / 180
      const x1 = (18 + 7.6 * Math.cos(rad)).toFixed(2)
      const y1 = (18 + 7.6 * Math.sin(rad)).toFixed(2)
      const x2 = (18 + 10.4 * Math.cos(rad)).toFixed(2)
      const y2 = (18 + 10.4 * Math.sin(rad)).toFixed(2)
      return `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" />`
    })
    .join('')
  return `<svg width="36" height="44" viewBox="0 0 36 44" xmlns="http://www.w3.org/2000/svg">
    <path d="M18 1C8.61 1 1 8.61 1 18c0 11.4 17 25 17 25s17-13.6 17-25C35 8.61 27.39 1 18 1Z" fill="${color}" stroke="#ffffff" stroke-width="2"/>
    <circle cx="18" cy="18" r="11.5" fill="#ffffff"/>
    <circle cx="18" cy="18" r="4.6" fill="${color}"/>
    <g stroke="${color}" stroke-width="1.7" stroke-linecap="round">${rays}</g>
  </svg>`
}

function pinDataUri(color: string): string {
  return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(pinSvg(color))}`
}

function markerColor(node: NodeAnalytics): string {
  // Green for a live hub, grey once it is deactivated.
  return node.status === 'Active' ? '#139a68' : '#98a1ad'
}

export function NodeMap({ nodes }: { nodes: NodeAnalytics[] }) {
  const { isLoaded, error } = useGoogleMapsScript()
  const mapId = (import.meta.env.VITE_GOOGLE_MAPS_MAP_ID as string | undefined)?.trim()
  const [timedOut, setTimedOut] = useState(false)
  const container = useRef<HTMLDivElement>(null)
  const located = useMemo(
    () => nodes.filter((node) => Number.isFinite(node.latitude) && Number.isFinite(node.longitude)),
    [nodes],
  )
  const unavailable = Boolean(error) || (!isLoaded && timedOut)

  useEffect(() => {
    if (isLoaded || error) return
    const timer = setTimeout(() => setTimedOut(true), 12_000)
    return () => clearTimeout(timer)
  }, [isLoaded, error])

  useEffect(() => {
    if (!isLoaded || !container.current || located.length === 0) return

    const map = new google.maps.Map(container.current, {
      center: { lat: located[0].latitude, lng: located[0].longitude },
      zoom: 11,
      ...(mapId ? { mapId } : {}),
      streetViewControl: false,
      mapTypeControl: false,
      fullscreenControl: false,
    })
    const infoWindow = new google.maps.InfoWindow()
    const bounds = new google.maps.LatLngBounds()
    const AdvancedMarker = mapId ? google.maps.marker?.AdvancedMarkerElement : undefined

    const markers = located.map((node) => {
      const position = { lat: node.latitude, lng: node.longitude }
      bounds.extend(position)
      const color = markerColor(node)
      let marker: google.maps.marker.AdvancedMarkerElement | google.maps.Marker
      if (AdvancedMarker) {
        const glyph = document.createElement('div')
        glyph.innerHTML = pinSvg(color)
        glyph.style.lineHeight = '0'
        marker = new AdvancedMarker({ map, position, title: node.name, content: glyph })
      } else {
        marker = new google.maps.Marker({
          map,
          position,
          title: node.name,
          icon: {
            url: pinDataUri(color),
            scaledSize: new google.maps.Size(36, 44),
            anchor: new google.maps.Point(18, 43),
          },
        })
      }
      marker.addListener('click', () => {
        infoWindow.setContent(summaryHtml(node))
        infoWindow.open({ anchor: marker, map })
      })
      return marker
    })

    if (located.length > 1) map.fitBounds(bounds, 60)
    const idleListener = google.maps.event.addListenerOnce(map, 'idle', () => {
      if ((map.getZoom() ?? 0) > 15) map.setZoom(15)
    })

    return () => {
      google.maps.event.removeListener(idleListener)
      infoWindow.close()
      markers.forEach((marker) => {
        google.maps.event.clearInstanceListeners(marker)
        if ('setMap' in marker) marker.setMap(null)
        else marker.map = null
      })
    }
  }, [isLoaded, located, mapId])

  return (
    <section className="rounded-2xl border border-ink-100 bg-white p-5">
      <div className="mb-4">
        <h3 className="text-sm font-semibold text-ink-900">Node map</h3>
        <p className="mt-0.5 text-xs text-ink-500">Every hub by GPS location. Click a pin for its summary.</p>
      </div>
      <div className="overflow-hidden rounded-xl border border-ink-200 bg-ink-50">
        {isLoaded && !unavailable && located.length > 0 ? (
          <div
            ref={container}
            className="h-[32rem] w-full"
            aria-label="Microgrid node locations. Click a marker to read its summary."
          />
        ) : (
          <div className="flex h-[32rem] flex-col items-center justify-center px-8 text-center">
            <p className="text-sm font-semibold text-ink-900">
              {unavailable
                ? 'Map unavailable'
                : located.length === 0
                  ? 'No located nodes'
                  : 'Loading node map…'}
            </p>
            <p className="mt-2 max-w-72 text-sm leading-6 text-ink-600">
              {unavailable
                ? 'The Google Maps key is not configured, so node locations cannot be plotted here. Node coordinates are still listed in the table below.'
                : located.length === 0
                  ? 'Nodes will appear here once they have GPS coordinates.'
                  : 'Node pins will appear shortly.'}
            </p>
          </div>
        )}
      </div>
    </section>
  )
}
