import type { MicrogridNode } from '@/features/microgrid/types'
import { useStationPickerMap } from '../hooks/useStationPickerMap'
import { ReservationIcon } from './ReservationIcon'

export function StationPickerMap({
  nodes,
  selected,
  onSelect,
}: {
  nodes: MicrogridNode[]
  selected?: MicrogridNode
  onSelect: (id: string) => void
}) {
  const { container, isLoaded, mapUnavailable } = useStationPickerMap(
    nodes,
    selected,
    onSelect,
  )

  return (
    <div className="flex min-h-80 flex-col overflow-hidden rounded-xl border border-ink-200 bg-ink-50">
      {isLoaded && !mapUnavailable && nodes.length ? (
        <div
          ref={container}
          className="min-h-80 flex-1"
          aria-label="Grid node locations. Select a marker or use the station list."
        />
      ) : (
        <div className="flex min-h-72 flex-1 flex-col items-center justify-center px-8 py-10 text-center">
          <ReservationIcon name="pin" className="mb-4 size-8 text-ink-600" />
          <p className="text-sm font-semibold text-ink-900">
            {mapUnavailable
              ? 'Choose from the station list'
              : !nodes.length
                ? 'No matching stations'
                : 'Loading station map…'}
          </p>
          <p className="mt-2 max-w-64 text-sm leading-6 text-ink-600">
            {mapUnavailable
              ? 'You can select a grid node using its address and availability. Location links open in Google Maps.'
              : !nodes.length
                ? 'Adjust your search to see station locations.'
                : 'Station locations will appear here.'}
          </p>
        </div>
      )}
      <div className="border-t border-ink-200 bg-white p-4">
        {selected ? (
          <>
            <p className="text-sm font-semibold text-ink-900">
              {selected.name}
            </p>
            <p className="mt-1 text-xs leading-5 text-ink-600">
              {selected.addressLine}
            </p>
            <a
              href={`https://www.google.com/maps/search/?api=1&query=${selected.latitude},${selected.longitude}`}
              target="_blank"
              rel="noreferrer"
              className="mt-3 inline-flex items-center gap-2 text-xs font-medium text-brand-800 underline-offset-4 hover:underline"
            >
              Open location in Maps
              <ReservationIcon name="external" className="size-3.5" />
            </a>
          </>
        ) : (
          <p className="text-xs text-ink-600">
            Select a station to inspect its location.
          </p>
        )}
      </div>
    </div>
  )
}
