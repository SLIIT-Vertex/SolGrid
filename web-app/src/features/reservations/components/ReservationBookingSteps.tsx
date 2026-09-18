import { useMemo, useState } from "react";
import type { ReactNode } from "react";
import { Button } from "@/components/common/Button";
import { TextField } from "@/components/common/TextField";
import { EmptyState, ErrorState } from "@/components/common/QueryStates";
import type {
  MicrogridNode,
  MicrogridBatterySlot,
} from "@/features/microgrid/types";
import type { Prosumer } from "@/features/prosumers/types";
import { cn } from "@/lib/cn";
import {
  dateLabel,
  dateTimeLabel,
  localDateTime,
  timeLabel,
  timezoneLabel,
} from "../presentation";
import type { BookingStep } from "../bookingSteps";
import { MAX_BOOKING_ADVANCE_MS, slotSelectable } from "../bookingRules";
import { StationPickerMap } from "./StationPickerMap";
import { ReservationIcon } from "./ReservationIcon";
import { ProsumerIdentity } from "./ProsumerIdentity";

interface OptionsState {
  loading: boolean;
  error: boolean;
  onRetry: () => void;
}
interface GridNodeStepProps extends OptionsState {
  nodes: MicrogridNode[];
  selectedStation?: MicrogridNode;
  onSelect: (id: string) => void;
}
export function GridNodeStep({
  nodes,
  selectedStation,
  loading,
  error,
  onRetry,
  onSelect,
}: GridNodeStepProps) {
  const [stationSearch, setStationSearch] = useState("");
  const visibleStations = useMemo(
    () =>
      nodes.filter((node) =>
        `${node.name} ${node.code} ${node.addressLine}`
          .toLowerCase()
          .includes(stationSearch.toLowerCase().trim()),
      ),
    [nodes, stationSearch],
  );
  return loading ? (
    <SelectionSkeleton />
  ) : error ? (
    <ErrorState
      message="Grid nodes couldn’t load. Try again to select a station."
      onRetry={() => onRetry()}
    />
  ) : (
    <>
      <TextField
        id="reservation-station-search"
        label="Search grid nodes"
        placeholder="Station name, code or address"
        value={stationSearch}
        onChange={(event) => setStationSearch(event.target.value)}
        className="placeholder:text-ink-600"
      />
      <div className="mt-5 grid gap-5 md:grid-cols-2">
        <div>
          <p className="mb-3 text-xs text-ink-600">
            {visibleStations.length} active{" "}
            {visibleStations.length === 1 ? "station" : "stations"}
          </p>
          <div className="max-h-107.5 space-y-3 overflow-y-auto pr-1">
            {visibleStations.map((node) => (
              <button
                key={node.id}
                type="button"
                aria-pressed={node.id === selectedStation?.id}
                onClick={() => onSelect(node.id)}
                className={cn(
                  "w-full rounded-xl border p-4 text-left transition-colors",
                  node.id === selectedStation?.id
                    ? "border-brand-600 bg-brand-50/60"
                    : "border-ink-200 hover:border-ink-300 hover:bg-ink-50",
                )}
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-sm font-semibold text-ink-900">
                      {node.name}
                    </p>
                    <p className="mt-1 text-xs text-ink-600">{node.code}</p>
                  </div>
                  <span
                    className={cn(
                      "flex size-5 shrink-0 items-center justify-center rounded-full border",
                      node.id === selectedStation?.id
                        ? "border-brand-700 bg-brand-700 text-white"
                        : "border-ink-300",
                    )}
                  >
                    {node.id === selectedStation?.id && (
                      <ReservationIcon name="check" className="size-3" />
                    )}
                  </span>
                </div>
                <p className="mt-3 flex items-start gap-2 text-xs leading-5 text-ink-600">
                  <ReservationIcon
                    name="pin"
                    className="mt-0.5 size-3.5 shrink-0"
                  />
                  {node.addressLine}
                </p>
                <div className="mt-4 flex flex-wrap gap-x-4 gap-y-1 text-xs">
                  <span
                    className={
                      node.availableSlotCount
                        ? "font-medium text-brand-800"
                        : "text-ink-600"
                    }
                  >
                    {node.availableSlotCount} of {node.totalSlotCount} slots
                    available
                  </span>
                  <span className="text-ink-600">{node.capacityKw} kW</span>
                </div>
              </button>
            ))}
            {!visibleStations.length && (
              <EmptyState
                title="No grid nodes found"
                description="Try a different station name or address."
              />
            )}
          </div>
        </div>
        <StationPickerMap
          nodes={visibleStations}
          selected={selectedStation}
          onSelect={onSelect}
        />
      </div>
    </>
  );
}

interface SlotTimeStepProps extends OptionsState {
  sortedSlots: MicrogridBatterySlot[];
  selectedStation?: MicrogridNode;
  selectedSlot?: MicrogridBatterySlot;
  bookingSlotId: string;
  currentSlotId?: string;
  scheduledAt: string;
  openedAt: number;
  onChangeStation: () => void;
  onSelectSlot: (id: string) => void;
  onTimeChange: (value: string) => void;
}
export function SlotTimeStep({
  sortedSlots,
  selectedStation,
  selectedSlot,
  bookingSlotId,
  currentSlotId,
  scheduledAt,
  openedAt,
  loading,
  error,
  onRetry,
  onChangeStation,
  onSelectSlot,
  onTimeChange,
}: SlotTimeStepProps) {
  const [slotDate, setSlotDate] = useState("");
  const dates = [
    ...new Set(
      sortedSlots.map((slot) => localDateTime(slot.startTime).slice(0, 10)),
    ),
  ];
  const visibleSlots = sortedSlots.filter(
    (slot) => !slotDate || localDateTime(slot.startTime).startsWith(slotDate),
  );
  return (
    <>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3 border-b border-ink-100 pb-5">
        <div>
          <p className="text-sm font-semibold text-ink-900">
            {selectedStation?.name}
          </p>
          <p className="mt-1 text-xs text-ink-600">
            {selectedStation?.addressLine}
          </p>
        </div>
        <button
          type="button"
          onClick={() => onChangeStation()}
          className="text-sm font-medium text-brand-800 hover:underline"
        >
          Change station
        </button>
      </div>
      {loading ? (
        <SelectionSkeleton />
      ) : error ? (
        <ErrorState
          message="Battery slots couldn’t load. Try again to check availability."
          onRetry={() => onRetry()}
        />
      ) : !sortedSlots.length ? (
        <EmptyState
          title="No battery slots at this station"
          description="Choose another grid node, or add battery slots in Microgrid Nodes."
          action={
            <Button variant="secondary" onClick={() => onChangeStation()}>
              Choose another station
            </Button>
          }
        />
      ) : (
        <>
          <div
            className="mb-5 flex gap-2 overflow-x-auto pb-1"
            role="group"
            aria-label="Filter slots by date"
          >
            <button
              type="button"
              aria-pressed={!slotDate}
              className={cn(
                "shrink-0 rounded-lg border px-3 py-2 text-xs font-medium",
                !slotDate
                  ? "border-brand-600 bg-brand-50 text-brand-900"
                  : "border-ink-200 text-ink-600",
              )}
              onClick={() => setSlotDate("")}
            >
              All dates
            </button>
            {dates.map((date) => (
              <button
                type="button"
                key={date}
                aria-pressed={slotDate === date}
                onClick={() => setSlotDate(date)}
                className={cn(
                  "shrink-0 rounded-lg border px-3 py-2 text-xs font-medium",
                  slotDate === date
                    ? "border-brand-600 bg-brand-50 text-brand-900"
                    : "border-ink-200 text-ink-600",
                )}
              >
                {dateLabel(`${date}T12:00:00`)}
              </button>
            ))}
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            {visibleSlots.map((slot) => {
              const available = slotSelectable(slot, currentSlotId);
              const selected = slot.id === bookingSlotId;
              return (
                <button
                  type="button"
                  key={slot.id}
                  disabled={!available}
                  aria-pressed={selected}
                  onClick={() => {
                    onSelectSlot(slot.id);
                  }}
                  className={cn(
                    "rounded-xl border p-4 text-left transition-colors disabled:cursor-not-allowed",
                    selected
                      ? "border-brand-600 bg-brand-50/60"
                      : available
                        ? "border-ink-200 hover:border-ink-300 hover:bg-ink-50"
                        : "border-ink-100 bg-ink-50",
                  )}
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="flex items-center gap-2 text-sm font-semibold text-ink-900">
                      <ReservationIcon
                        name="battery"
                        className="size-4 text-ink-600"
                      />
                      Battery slot {slot.slotNumber}
                    </span>
                    <span
                      className={cn(
                        "text-xs font-medium",
                        available ? "text-brand-800" : "text-ink-600",
                      )}
                    >
                      {selected
                        ? "Selected"
                        : available
                          ? "Available"
                          : !slot.isActive
                            ? "Out of service"
                            : slot.status === "Available"
                              ? "Unavailable"
                              : slot.status}
                    </span>
                  </div>
                  <p className="mt-4 text-lg font-semibold tabular-nums text-ink-900">
                    {slot.batteryCapacityKwh}{" "}
                    <span className="text-xs font-normal text-ink-600">
                      kWh capacity
                    </span>
                  </p>
                  <div className="mt-3 border-t border-ink-200/70 pt-3 text-xs leading-5 text-ink-600">
                    <p>{dateLabel(slot.startTime)}</p>
                    <p className="mt-0.5 font-medium text-ink-800">
                      {timeLabel(slot.startTime)} – {timeLabel(slot.endTime)}
                      {dateLabel(slot.startTime) !== dateLabel(slot.endTime) &&
                        ` · ${dateLabel(slot.endTime)}`}
                    </p>
                  </div>
                </button>
              );
            })}
          </div>
          {selectedSlot && (
            <div className="mt-7 border-t border-ink-100 pt-6">
              <TextField
                id="reservation-scheduled-time"
                label="Scheduled arrival"
                type="datetime-local"
                required
                min={localDateTime(new Date(openedAt).toISOString())}
                max={localDateTime(
                  new Date(openedAt + MAX_BOOKING_ADVANCE_MS).toISOString(),
                )}
                value={scheduledAt}
                onChange={(event) => {
                  onTimeChange(event.target.value);
                }}
                hint={`Slot window: ${dateTimeLabel(selectedSlot.startTime)} – ${dateTimeLabel(selectedSlot.endTime)}. Times use ${timezoneLabel}.`}
              />
              <p className="mt-3 text-xs leading-5 text-ink-600">
                Choose a future time within seven days. Check that arrival works
                with the slot’s time window.
              </p>
            </div>
          )}
        </>
      )}
    </>
  );
}

interface ReviewStepProps {
  selectedPerson: Prosumer | null;
  prosumerId?: string;
  selectedStation?: MicrogridNode;
  selectedSlot?: MicrogridBatterySlot;
  scheduledAt: string;
  isEditing: boolean;
  goTo: (step: BookingStep) => void;
}
export function ReviewStep({
  selectedPerson,
  prosumerId,
  selectedStation,
  selectedSlot,
  scheduledAt,
  isEditing,
  goTo,
}: ReviewStepProps) {
  return (
    <div>
      <ReviewSection
        title="Prosumer"
        onChange={!isEditing ? () => goTo("prosumer") : undefined}
      >
        {selectedPerson ? (
          <ProsumerIdentity prosumer={selectedPerson} />
        ) : (
          <p className="text-sm text-ink-800">{prosumerId}</p>
        )}
      </ReviewSection>
      <ReviewSection title="Grid node" onChange={() => goTo("grid-node")}>
        <p className="text-sm font-semibold text-ink-900">
          {selectedStation?.name}
        </p>
        <p className="mt-1 text-sm text-ink-600">
          {selectedStation?.addressLine}
        </p>
        <p className="mt-1 text-xs text-ink-600">
          {selectedStation?.code} · {selectedStation?.capacityKw} kW station
          capacity
        </p>
      </ReviewSection>
      <ReviewSection
        title="Battery slot & time"
        onChange={() => goTo("slot-time")}
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <p className="text-sm font-semibold text-ink-900">
              Battery slot {selectedSlot?.slotNumber}
            </p>
            <p className="mt-1 text-sm text-ink-600">
              {selectedSlot?.batteryCapacityKwh} kWh capacity
            </p>
          </div>
          <div>
            <p className="text-sm font-semibold text-ink-900">
              {scheduledAt && dateTimeLabel(scheduledAt)}
            </p>
            <p className="mt-1 text-xs text-ink-600">{timezoneLabel}</p>
          </div>
        </div>
        {selectedSlot && (
          <p className="mt-4 text-xs leading-5 text-ink-600">
            Slot window: {dateTimeLabel(selectedSlot.startTime)} –{" "}
            {dateTimeLabel(selectedSlot.endTime)}
          </p>
        )}
      </ReviewSection>
      <div className="mt-5 flex gap-3 rounded-lg bg-amber-50 p-4 text-sm leading-6 text-amber-900">
        <ReservationIcon name="info" className="mt-1 size-4 shrink-0" />
        <p>
          {isEditing
            ? "Your changes will be saved to this reservation."
            : "This reservation will be pending until an operator approves it."}{" "}
          Updates and cancellations need at least twelve hours’ notice.
        </p>
      </div>
    </div>
  );
}

function ReviewSection({
  title,
  children,
  onChange,
}: {
  title: string;
  children: ReactNode;
  onChange?: () => void;
}) {
  return (
    <section className="border-b border-ink-100 py-5 first:pt-0">
      <div className="mb-3 flex items-center justify-between gap-3">
        <h3 className="text-xs font-medium text-ink-600">{title}</h3>
        {onChange && (
          <button
            type="button"
            onClick={onChange}
            className="text-xs font-medium text-brand-800 hover:underline"
          >
            Change
          </button>
        )}
      </div>
      {children}
    </section>
  );
}
function SelectionSkeleton() {
  return (
    <div role="status" aria-label="Loading options" className="space-y-4">
      <span className="sr-only">Loading options…</span>
      {[1, 2, 3].map((item) => (
        <div
          key={item}
          className="h-24 rounded-lg bg-ink-100 motion-safe:animate-pulse"
        />
      ))}
    </div>
  );
}
